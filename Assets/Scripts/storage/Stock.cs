using System.Collections.Generic;
using UnityEngine;

public static class FridgeStock
{
    static int capacity = 6;
    static readonly int[] stock = { 6, 6, 6, 6 };

    public static int Capacity => capacity;

    public static void Reset(int cap)
    {
        capacity = cap;
        for (int i = 0; i < stock.Length; i++) stock[i] = cap;
    }

    public static int Count(MeatType m) => stock[(int)m];
    public static bool NeedsRefill(MeatType m) => stock[(int)m] < capacity;
    public static void Refill(MeatType m) => stock[(int)m] = capacity;
    public static void Return(MeatType m) => stock[(int)m] = Mathf.Min(capacity, stock[(int)m] + 1);

    public static bool Take(MeatType m)
    {
        if (stock[(int)m] <= 0) return false;
        stock[(int)m]--;
        return true;
    }
}

public static class BarStock
{
    static int capacity = 900;
    static readonly Dictionary<DrinkData, int> stock = new Dictionary<DrinkData, int>();

    public static int Capacity => capacity;

    public static void Reset(int cap)
    {
        capacity = cap;
        stock.Clear();
    }

    public static int Count(DrinkData d)
    {
        int n;
        return stock.TryGetValue(d, out n) ? n : capacity;
    }

    public static float Fraction(DrinkData d) => Count(d) / (float)capacity;
    public static bool NeedsRefill(DrinkData d) => Fraction(d) < 0.9f;
    public static void Refill(DrinkData d) => stock[d] = capacity;

    public static bool Use(DrinkData d)
    {
        int n = Count(d);
        if (n <= 0) return false;
        stock[d] = n - 1;
        return true;
    }
}