using UnityEngine;

public class WallFader : MonoBehaviour
{
    [Tooltip("Size of the see-through hole, in half screen heights")]
    public float radius = 0.35f;
    [Range(0.01f, 0.5f)] public float softness = 0.15f;
    [Tooltip("How much of the wall stays visible inside the hole. 0 = fully hidden")]
    [Range(0f, 1f)] public float leftover = 0f;
    [Tooltip("Height of the ground. Anything lower than this plus Wall Min Height is never faded")]
    public float floorY = 0f;
    public float wallMinHeight = 0.05f;
    [Tooltip("Aim at the middle of the character instead of its feet")]
    public float targetHeight = 0.2f;
    public float fadeSpeed = 6f;
    public float switchSpeed = 10f;
    public Transform fallbackTarget;

    static readonly int TargetId = Shader.PropertyToID("_FadeTarget");
    static readonly int ParamsId = Shader.PropertyToID("_FadeParams");

    Transform last;
    Vector3 offset;
    Vector3 shown;
    float strength;

    void LateUpdate()
    {
        Transform t = PlayerMovement.Current != null ? PlayerMovement.Current.transform : fallbackTarget;
        strength = Mathf.MoveTowards(strength, t != null ? 1f : 0f, fadeSpeed * Time.deltaTime);

        if (t != null)
        {
            Vector3 p = t.position + Vector3.up * targetHeight;
            if (t != last)
            {
                offset = last != null ? shown - p : Vector3.zero;
                last = t;
            }
            offset = Vector3.Lerp(offset, Vector3.zero, 1f - Mathf.Exp(-switchSpeed * Time.deltaTime));
            shown = p + offset;
        }

        Shader.SetGlobalVector(TargetId, new Vector4(shown.x, shown.y, shown.z, strength * (1f - leftover)));
        Shader.SetGlobalVector(ParamsId, new Vector4(radius, softness, floorY, wallMinHeight));
    }

    void OnDisable()
    {
        Shader.SetGlobalVector(TargetId, Vector4.zero);
    }
}