using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PlayerBushStealthPunSwap : MonoBehaviourPun
{
    [Header("Local fade")]
    [Range(0f, 1f)] public float localAlphaInBush = 0.5f;

    [Tooltip("Renderers to fade locally (usually SkinnedMeshRenderers of the character). If empty, auto-finds SkinnedMeshRenderers.")]
    public Renderer[] fadeRenderers;

    [Header("Remote hide (others can't see you)")]
    [Tooltip("Renderers to hide on other clients. If empty, auto-finds ALL renderers under this player (includes nameplates/UI).")]
    public Renderer[] hideRenderers;

    [Tooltip("Send hide/show to others via buffered RPC so late joiners see correct state.")]
    public bool useBufferedRpc = true;

    [Header("Transparent clone tuning (URP/Lit)")]
    [Tooltip("If true, forces metallic/smoothness to safer values on the transparent clones to avoid dark/black look.")]
    public bool tweakLitForTransparency = true;
    [Range(0f, 1f)] public float forcedMetallic = 0f;
    [Range(0f, 1f)] public float forcedSmoothness = 0.2f;

    int _overlapCount;
    bool _hiddenOnThisClient;

    class SwapSet
    {
        public Renderer r;
        public Material[] normal;
        public Material[] stealth; // transparent clones
    }
    readonly List<SwapSet> _swapSets = new();

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    static readonly int MetallicId = Shader.PropertyToID("_Metallic");
    static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

    void Awake()
    {
        // Fade renderers: default to SkinnedMeshRenderers (character)
        if (fadeRenderers == null || fadeRenderers.Length == 0)
        {
            var skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var list = new List<Renderer>();
            for (int i = 0; i < skinned.Length; i++) list.Add(skinned[i]);
            fadeRenderers = list.ToArray();
        }

        // Hide renderers: default to ALL renderers under player (including nameplate)
        if (hideRenderers == null || hideRenderers.Length == 0)
            hideRenderers = GetComponentsInChildren<Renderer>(true);

        BuildStealthClones();
    }

    void OnDestroy()
    {
        // Cleanup instantiated materials
        for (int i = 0; i < _swapSets.Count; i++)
        {
            var set = _swapSets[i];
            if (set?.stealth == null) continue;
            for (int m = 0; m < set.stealth.Length; m++)
                if (set.stealth[m] != null) Destroy(set.stealth[m]);
        }
    }

    void BuildStealthClones()
    {
        _swapSets.Clear();

        for (int i = 0; i < fadeRenderers.Length; i++)
        {
            var r = fadeRenderers[i];
            if (r == null) continue;

            var normal = r.sharedMaterials;
            var stealth = new Material[normal.Length];

            for (int j = 0; j < normal.Length; j++)
            {
                var src = normal[j];
                if (src == null) continue;

                // Clone so we can modify safely per-player.
                var m = new Material(src);
                ConfigureAsTransparentURP(m);

                if (tweakLitForTransparency)
                {
                    if (m.HasProperty(MetallicId)) m.SetFloat(MetallicId, forcedMetallic);
                    if (m.HasProperty(SmoothnessId)) m.SetFloat(SmoothnessId, forcedSmoothness);
                }

                stealth[j] = m;
            }

            _swapSets.Add(new SwapSet { r = r, normal = normal, stealth = stealth });
        }
    }

    static void ConfigureAsTransparentURP(Material m)
    {
        // This assumes URP/Lit-style properties. If your shader doesn't have these,
        // it will simply skip what it can't set.
        if (m.HasProperty(SurfaceId)) m.SetFloat(SurfaceId, 1f); // 0=Opaque, 1=Transparent

        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;

        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);

        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    void SetLocalAlpha(float a)
    {
        for (int i = 0; i < _swapSets.Count; i++)
        {
            var set = _swapSets[i];
            for (int j = 0; j < set.stealth.Length; j++)
            {
                var mat = set.stealth[j];
                if (mat == null) continue;

                // Preserve RGB; change only alpha
                if (mat.HasProperty(BaseColorId))
                {
                    var c = mat.GetColor(BaseColorId);
                    c.a = a;
                    mat.SetColor(BaseColorId, c);
                }
                else if (mat.HasProperty(ColorId))
                {
                    var c = mat.GetColor(ColorId);
                    c.a = a;
                    mat.SetColor(ColorId, c);
                }
            }
        }
    }

    void SwapToStealth()
    {
        // swap materials (local only)
        for (int i = 0; i < _swapSets.Count; i++)
            _swapSets[i].r.sharedMaterials = _swapSets[i].stealth;

        SetLocalAlpha(localAlphaInBush);
    }

    void SwapToNormal()
    {
        for (int i = 0; i < _swapSets.Count; i++)
            _swapSets[i].r.sharedMaterials = _swapSets[i].normal;
    }

    // Called by bush trigger
    public void EnterBush()
    {
        if (!photonView.IsMine) return;

        _overlapCount++;
        if (_overlapCount != 1) return;

        SwapToStealth();

        if (useBufferedRpc)
            photonView.RPC(nameof(RPC_SetHiddenRemote), RpcTarget.OthersBuffered, true);
        else
            photonView.RPC(nameof(RPC_SetHiddenRemote), RpcTarget.Others, true);
    }

    // Called by bush trigger
    public void ExitBush()
    {
        if (!photonView.IsMine) return;

        _overlapCount = Mathf.Max(0, _overlapCount - 1);
        if (_overlapCount != 0) return;

        SwapToNormal();

        if (useBufferedRpc)
            photonView.RPC(nameof(RPC_SetHiddenRemote), RpcTarget.OthersBuffered, false);
        else
            photonView.RPC(nameof(RPC_SetHiddenRemote), RpcTarget.Others, false);
    }

    [PunRPC]
    void RPC_SetHiddenRemote(bool hidden)
    {
        // Only affects THIS client’s view of the remote player.
        if (_hiddenOnThisClient == hidden) return;
        _hiddenOnThisClient = hidden;

        for (int i = 0; i < hideRenderers.Length; i++)
        {
            var r = hideRenderers[i];
            if (r != null) r.enabled = !hidden;
        }
    }
}
