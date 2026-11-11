using System.Collections.Generic;
using UnityEngine;

// Put this on the stage camera (same object as the Camera component).
// Drops are drawn as soft blobs on the "LiquidFX" layer into a render texture.
// A quad in front of the stage camera shows that texture through a threshold shader,
// which merges nearby blobs into one smooth liquid shape.
public class LiquidMetaball : MonoBehaviour
{
    [Header("References")]
    public Camera stageCamera;
    public Shader thresholdShader;   // Bartender/LiquidThreshold
    public Shader blobShader;        // Bartender/LiquidBlob

    [Header("Layers (must exist)")]
    public string stageLayer = "Bartender";
    public string fxLayer = "LiquidFX";

    [Header("Look")]
    [Range(0.05f, 1.5f)] public float threshold = 0.5f;   // higher = thinner, tighter liquid
    [Range(0.001f, 0.3f)] public float softness = 0.04f;  // edge smoothness
    [Range(0f, 1f)] public float rimWidth = 0.25f;        // width of the darker edge
    [Range(0f, 1f)] public float rimDarken = 0.7f;        // 1 = no rim
    [Range(0f, 1f)] public float opacity = 0.92f;
    public float blobScale = 2.5f;                        // blob size relative to the drop; higher = more merging
    [Range(0.25f, 1f)] public float resolution = 0.5f;    // render texture size relative to the screen
    public float quadDistance = 2f;
    public int sortingOrder = 0;                          // glass should be higher than this

    RenderTexture rt;
    Camera liquidCam;
    Transform quad;
    Material quadMat;
    readonly Dictionary<Color, Material> blobMats = new Dictionary<Color, Material>();
    Sprite blobSprite;

    void Reset()
    {
        stageCamera = GetComponent<Camera>();
        thresholdShader = Shader.Find("Bartender/LiquidThreshold");
        blobShader = Shader.Find("Bartender/LiquidBlob");
    }

    void OnEnable() { Init(); BuildView(); }
    void OnDisable() { Cleanup(); }

    void Init()
    {
        if (blobSprite != null) return;
        blobSprite = MakeBlobSprite();
    }

    // One material per liquid color, so the color doesn't depend on sprite vertex colors
    Material GetBlobMaterial(Color c)
    {
        if (!blobMats.TryGetValue(c, out var m) || m == null)
        {
            m = new Material(blobShader != null ? blobShader : Shader.Find("Bartender/LiquidBlob"));
            m.SetColor("_Color", c);
            blobMats[c] = m;
        }
        return m;
    }

    // Soft radial gradient generated in code, so no texture asset is needed
    static Sprite MakeBlobSprite()
    {
        int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f;
                float dy = (y + 0.5f) / s * 2f - 1f;
                float a = Mathf.Clamp01(1f - (dx * dx + dy * dy));
                px[y * s + x] = new Color(1f, 1f, 1f, a * a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);   // 1 unit wide
    }

    public SpriteRenderer CreateBlob(Transform parent, Color color, float dropDiameter)
    {
        Init();
        var go = new GameObject("Blob");
        go.layer = LayerMask.NameToLayer(fxLayer);
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * dropDiameter * blobScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = blobSprite;
        sr.sharedMaterial = GetBlobMaterial(color);
        return sr;
    }

    void BuildView()
    {
        if (stageCamera == null) stageCamera = GetComponent<Camera>();
        int fx = LayerMask.NameToLayer(fxLayer);
        int stage = LayerMask.NameToLayer(stageLayer);
        if (fx < 0 || stage < 0)
        {
            Debug.LogError("LiquidMetaball: create the layers '" + stageLayer + "' and '" + fxLayer + "'.");
            enabled = false;
            return;
        }

        CreateRT();

        var camGO = new GameObject("LiquidCamera");
        camGO.transform.SetParent(stageCamera.transform, false);
        liquidCam = camGO.AddComponent<Camera>();
        liquidCam.CopyFrom(stageCamera);
        liquidCam.cullingMask = 1 << fx;
        liquidCam.clearFlags = CameraClearFlags.SolidColor;
        liquidCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        liquidCam.depth = stageCamera.depth - 5f;
        liquidCam.allowHDR = false;
        liquidCam.allowMSAA = false;
        liquidCam.targetTexture = rt;

        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "LiquidQuad";
        q.layer = stage;
        Destroy(q.GetComponent<Collider>());
        quad = q.transform;

        quadMat = new Material(thresholdShader != null ? thresholdShader : Shader.Find("Bartender/LiquidThreshold"));
        quadMat.SetTexture("_MainTex", rt);
        var mr = q.GetComponent<MeshRenderer>();
        mr.sharedMaterial = quadMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = sortingOrder;
    }

    void CreateRT()
    {
        int w = Mathf.Max(64, Mathf.RoundToInt(Screen.width * resolution));
        int h = Mathf.Max(64, Mathf.RoundToInt(Screen.height * resolution));
        rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf)   // depth 24: URP's Render Graph requires a depth buffer
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1
        };
        rt.Create();
    }

    void LateUpdate()
    {
        if (liquidCam == null || quad == null) return;

        // Rebuild the texture if the window size changed
        int w = Mathf.Max(64, Mathf.RoundToInt(Screen.width * resolution));
        if (rt.width != w)
        {
            liquidCam.targetTexture = null;
            rt.Release();
            Destroy(rt);
            CreateRT();
            liquidCam.targetTexture = rt;
            quadMat.SetTexture("_MainTex", rt);
        }

        // Keep the liquid camera identical to the stage camera
        liquidCam.orthographic = stageCamera.orthographic;
        liquidCam.orthographicSize = stageCamera.orthographicSize;
        liquidCam.fieldOfView = stageCamera.fieldOfView;
        liquidCam.aspect = stageCamera.aspect;

        // Fit the quad exactly to the stage camera's view
        Transform ct = stageCamera.transform;
        quad.position = ct.position + ct.forward * quadDistance;
        quad.rotation = ct.rotation;
        float height = stageCamera.orthographic
            ? stageCamera.orthographicSize * 2f
            : 2f * quadDistance * Mathf.Tan(stageCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        quad.localScale = new Vector3(height * stageCamera.aspect, height, 1f);

        quadMat.SetFloat("_Threshold", threshold);
        quadMat.SetFloat("_Softness", softness);
        quadMat.SetFloat("_RimWidth", rimWidth);
        quadMat.SetFloat("_RimDarken", rimDarken);
        quadMat.SetFloat("_Opacity", opacity);
    }

    void Cleanup()
    {
        if (liquidCam != null) { liquidCam.targetTexture = null; Destroy(liquidCam.gameObject); }
        if (quad != null) Destroy(quad.gameObject);
        if (quadMat != null) Destroy(quadMat);
        if (rt != null) { rt.Release(); Destroy(rt); }
        foreach (var m in blobMats.Values) if (m != null) Destroy(m);
        blobMats.Clear();
        liquidCam = null; quad = null; quadMat = null; rt = null;
    }
}