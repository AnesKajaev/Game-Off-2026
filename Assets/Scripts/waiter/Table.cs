using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Seat
{
    public Transform approachPoint;
    public Transform sitPoint;

    [HideInInspector] public Guest guest;
    public bool Occupied => guest != null;

    public void Reserve(Guest g) => guest = g;
    public void Release(Guest g) { if (guest == g) guest = null; }
}

public class Table : MonoBehaviour
{
    public static readonly List<Table> All = new List<Table>();

    public string label = "";
    public Seat[] seats;

    public string Label => string.IsNullOrEmpty(label) ? name : label;

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public string SeatLabel(Seat s)
    {
        if (seats == null || seats.Length <= 1) return Label;
        return Label + " - Seat " + (System.Array.IndexOf(seats, s) + 1);
    }

    public bool IsEmpty
    {
        get
        {
            foreach (var s in seats) if (s.Occupied) return false;
            return true;
        }
    }

    public static int EmptyTableCount(int minSeats = 1)
    {
        int n = 0;
        foreach (var t in All) if (t.IsEmpty && t.seats.Length >= minSeats) n++;
        return n;
    }

    public static Table PickEmpty(int minSeats)
    {
        var list = new List<Table>();
        foreach (var t in All) if (t.IsEmpty && t.seats.Length >= minSeats) list.Add(t);
        return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
    }

    public bool TryPickFreeSeat(out Seat seat)
    {
        var free = new List<Seat>();
        foreach (var s in seats) if (!s.Occupied) free.Add(s);
        seat = free.Count == 0 ? null : free[Random.Range(0, free.Count)];
        return seat != null;
    }

    public static int FreeSeatCount()
    {
        int n = 0;
        foreach (var t in All)
            foreach (var s in t.seats)
                if (!s.Occupied) n++;
        return n;
    }

    public static bool TryPickFree(out Table table, out Seat seat)
    {
        var tables = new List<Table>();
        var seatList = new List<Seat>();
        foreach (var t in All)
            foreach (var s in t.seats)
                if (!s.Occupied) { tables.Add(t); seatList.Add(s); }

        table = null; seat = null;
        if (seatList.Count == 0) return false;

        int i = Random.Range(0, seatList.Count);
        table = tables[i];
        seat = seatList[i];
        return true;
    }
}