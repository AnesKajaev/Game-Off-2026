using System.Collections.Generic;
using UnityEngine;

public class BarCounter : MonoBehaviour
{
    public static BarCounter Instance { get; private set; }

    [Tooltip("Empty objects on the NavMesh in front of the bar, one per guest that can wait")]
    public Transform[] spots;
    [Tooltip("What guests face while they wait, usually the bar counter")]
    public Transform lookTarget;

    Guest[] occupants;

    void Awake()
    {
        Instance = this;
        occupants = new Guest[spots.Length];
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool TryReserve(Guest g, out Transform spot)
    {
        spot = null;
        var free = new List<int>();
        for (int i = 0; i < occupants.Length; i++)
            if (occupants[i] == null) free.Add(i);
        if (free.Count == 0) return false;

        int pick = free[Random.Range(0, free.Count)];
        occupants[pick] = g;
        spot = spots[pick];
        return true;
    }

    public void Release(Guest g)
    {
        for (int i = 0; i < occupants.Length; i++)
            if (occupants[i] == g) occupants[i] = null;
    }

    public Guest NextCustomer()
    {
        if (occupants == null) return null;
        foreach (var g in occupants)
            if (g != null && g.NeedsDrinks) return g;
        return null;
    }
}