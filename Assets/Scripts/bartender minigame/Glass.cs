using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class Glass : MonoBehaviour
{
    [Header("Inner outline: local space, origin = center of base. Go left rim -> bottom -> right rim")]
    public Vector2[] profile =
    {
        new Vector2(-0.4f, 1f), new Vector2(-0.4f, 0f),
        new Vector2(0.4f, 0f),  new Vector2(0.4f, 1f)
    };
    public float wallThickness = 0.08f;
    public PhysicsMaterial2D physicsMaterial;

    [Header("Target")]
    [Range(0.2f, 1f)] public float targetFill = 0.8f;
    [Range(0.05f, 1f)] public float minFill = 0.6f;
    public float tolerance = 0.08f;

    [HideInInspector] public DrinkData drink;

    float minX, maxX, minY, maxY;

    public float TargetY => minY + (maxY - minY) * targetFill;
    public float MinY => minY + (maxY - minY) * Mathf.Min(minFill, targetFill);

    void OnValidate() => minFill = Mathf.Min(minFill, targetFill);

    void Awake()
    {
        CalcBounds();
        BuildCollider();
    }

    void CalcBounds()
    {
        minX = minY = float.MaxValue;
        maxX = maxY = float.MinValue;
        foreach (var p in profile)
        {
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
    }

    void BuildCollider()
    {
        var edge = GetComponent<EdgeCollider2D>();
        if (edge == null) edge = gameObject.AddComponent<EdgeCollider2D>();
        edge.points = profile;
        edge.edgeRadius = wallThickness / 2f;
        if (physicsMaterial) edge.sharedMaterial = physicsMaterial;
    }

    public bool Contains(Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = profile.Length - 1; i < profile.Length; j = i++)
        {
            Vector2 a = profile[i], b = profile[j];
            if ((a.y > p.y) != (b.y > p.y) &&
                p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    public bool InColumn(Vector2 p) => p.x > minX && p.x < maxX && p.y > minY;

    void OnDrawGizmosSelected()
    {
        if (profile == null || profile.Length < 2) return;
        CalcBounds();
        Gizmos.color = Color.cyan;
        for (int i = 0; i < profile.Length - 1; i++)
            Gizmos.DrawLine(transform.TransformPoint(profile[i]), transform.TransformPoint(profile[i + 1]));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(minX, TargetY, 0)),
                        transform.TransformPoint(new Vector3(maxX, TargetY, 0)));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(minX, MinY, 0)),
                        transform.TransformPoint(new Vector3(maxX, MinY, 0)));
    }
}