using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStealthVisibilityPun : MonoBehaviourPun
{
    [Header("Local visual")]
    [Range(0f, 1f)] public float localAlphaInBush = 0.5f;

    [Tooltip("If empty, auto-finds all Renderers in children.")]
    public Renderer[] renderersToFade;

    [Header("Network")]
    public bool hideForOthers = true; // other clients cannot see you when in bush

    int _overlapCount;
    bool _hiddenOnThisClient; // remote-side hidden state

    MaterialPropertyBlock _mpb;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        if (renderersToFade == null || renderersToFade.Length == 0)
            renderersToFade = GetComponentsInChildren<Renderer>(true);

        _mpb = new MaterialPropertyBlock();
    }

    // Called by bush trigger
    public void EnterBush()
    {
        if (!photonView.IsMine) return;

        _overlapCount++;
        if (_overlapCount != 1) return;

        SetLocalAlpha(localAlphaInBush);

        if (hideForOthers)
            photonView.RPC(nameof(RPC_SetHiddenRemote), RpcTarget.OthersBuffered, true);
    }

    // Called by bush trigger
    public void ExitBush()
    {
        if (!photonView.IsMine) return;

        _overlapCount = Mathf.Max(0, _overlapCount - 1);
        if (_overlapCount != 0) return;

        SetLocalAlpha(1f);

        if (hideForOthers)
            photonView.RPC(nameof(RPC_SetHiddenRemote), RpcTarget.OthersBuffered, false);
    }

    void SetLocalAlpha(float a)
    {
        // This affects ONLY this client, because only the local player calls Enter/Exit.
        for (int i = 0; i < renderersToFade.Length; i++)
        {
            var r = renderersToFade[i];
            if (r == null) continue;

            r.GetPropertyBlock(_mpb);

            // URP/Lit uses _BaseColor
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId))
            {
                var c = r.sharedMaterial.GetColor(BaseColorId);
                c.a = a;
                _mpb.SetColor(BaseColorId, c);
            }
            // Fallback for other shaders
            else if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(ColorId))
            {
                var c = r.sharedMaterial.GetColor(ColorId);
                c.a = a;
                _mpb.SetColor(ColorId, c);
            }

            r.SetPropertyBlock(_mpb);
        }
    }

    [PunRPC]
    void RPC_SetHiddenRemote(bool hidden)
    {
        // Remote clients: you are invisible (enabled=false).
        if (_hiddenOnThisClient == hidden) return;
        _hiddenOnThisClient = hidden;

        for (int i = 0; i < renderersToFade.Length; i++)
        {
            if (renderersToFade[i] != null)
                renderersToFade[i].enabled = !hidden;
        }
    }

    void OnDisable()
    {
        // Safety: reset local alpha and renderer visibility
        for (int i = 0; i < renderersToFade.Length; i++)
        {
            if (renderersToFade[i] == null) continue;
            renderersToFade[i].enabled = true;
            renderersToFade[i].SetPropertyBlock(null);
        }
    }
}
