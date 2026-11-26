using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SurfaceZone : MonoBehaviour
{
    public enum Surface { Carpet, Concrete, Tiles }
    public enum Area { None, Dining, Kitchen, Bar, Storage }

    public static Surface defaultSurface = Surface.Tiles;

    static readonly List<SurfaceZone> All = new List<SurfaceZone>();

    public Surface surface = Surface.Tiles;
    public Area area = Area.None;

    BoxCollider box;

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void Awake() => box = GetComponent<BoxCollider>();
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public static Area AreaAt(Vector3 p)
    {
        foreach (var z in All)
        {
            if (z.area == Area.None) continue;
            Bounds b = z.box.bounds;
            if (p.x < b.min.x || p.x > b.max.x || p.z < b.min.z || p.z > b.max.z) continue;
            return z.area;
        }
        return Area.Dining;
    }

    public static string FootstepEventAt(Vector3 p)
    {
        Surface s = defaultSurface;
        foreach (var z in All)
        {
            Bounds b = z.box.bounds;
            if (p.x < b.min.x || p.x > b.max.x || p.z < b.min.z || p.z > b.max.z) continue;
            s = z.surface;
            break;
        }

        switch (s)
        {
            case Surface.Carpet: return Sfx.FootCarpet;
            case Surface.Concrete: return Sfx.FootConcrete;
            default: return Sfx.FootTiles;
        }
    }
}