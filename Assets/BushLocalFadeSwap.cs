using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class BushLocalFadePunSwap : MonoBehaviour
{
    [Header("Local fade")]
    [Range(0f, 1f)] public float localAlphaInBush = 0.5f;

    [Header("Transparent clone tuning (URP/Lit)")]
    public bool tweakLitForTransparency = true;
    [Range(0f, 1f)] public float forcedMetallic = 0f;
    [Range(0f, 1f)] public float forcedSmoothness = 0.2f;

    int _localOverlapCount;

    Renderer _renderer;
    Material[] _normal;
    Material[] _stealth;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId     = Shader.PropertyToID("_Color");
    static readonly int SurfaceId   = Shader.PropertyToID("_Surface");
    static readonly int MetallicId  = Shader.PropertyToID("_Metallic");
    static readonly int SmoothnessId= Shader.PropertyToID("_Smoothness");

    void Awake()
    {
        // Strict: only the Renderer on THIS GameObject.
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError($"[BushLocalFadePunSwap] No Renderer found on '{name}'. Put this script on the same GameObject that has the bush Renderer.");
            enabled = false;
            return;
        }

        BuildStealthClones();
    }

    void OnDestroy()
    {
        if (_stealth == null) return;

        for (int i = 0; i < _stealth.Length; i++)
        {
            if (_stealth[i] != null)
                Destroy(_stealth[i]);
        }
    }

    void BuildStealthClones()
    {
        _normal = _renderer.sharedMaterials;
        _stealth = new Material[_normal.Length];

        for (int j = 0; j < _normal.Length; j++)
        {
            var src = _normal[j];
            if (src == null) continue;

            var m = new Material(src); // clone
            ConfigureAsTransparentURP(m);

            if (tweakLitForTransparency)
            {
                if (m.HasProperty(MetallicId))   m.SetFloat(MetallicId, forcedMetallic);
                if (m.HasProperty(SmoothnessId)) m.SetFloat(SmoothnessId, forcedSmoothness);
            }

            // start as fully opaque even though transparent-capable
            SetMatAlpha(m, 1f);

            _stealth[j] = m;
        }
    }

    static void ConfigureAsTransparentURP(Material m)
    {
        if (m.HasProperty(SurfaceId)) m.SetFloat(SurfaceId, 1f); // 1 = Transparent

        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;

        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);

        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    static void SetMatAlpha(Material mat, float a)
    {
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

    void SwapToStealth(float alpha)
    {
        if (_renderer == null || _stealth == null) return;

        for (int j = 0; j < _stealth.Length; j++)
        {
            var mat = _stealth[j];
            if (mat != null) SetMatAlpha(mat, alpha);
        }

        _renderer.sharedMaterials = _stealth;
    }

    void SwapToNormal()
    {
        if (_renderer == null || _normal == null) return;
        _renderer.sharedMaterials = _normal;
    }

    void OnTriggerEnter(Collider other)
    {
        var pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        _localOverlapCount++;
        if (_localOverlapCount == 1)
            SwapToStealth(localAlphaInBush);
    }

    void OnTriggerExit(Collider other)
    {
        var pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        _localOverlapCount = Mathf.Max(0, _localOverlapCount - 1);
        if (_localOverlapCount == 0)
            SwapToNormal();
    }
}
