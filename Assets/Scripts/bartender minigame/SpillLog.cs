using UnityEngine;

public static class SpillLog
{
    public static void Add(string drink, int amount)
    {
        if (amount > 0) PlayerPrefs.SetInt(Key(drink), Get(drink) + amount);
    }

    public static int Get(string drink) => PlayerPrefs.GetInt(Key(drink), 0);
    public static void Clear(string drink) => PlayerPrefs.DeleteKey(Key(drink));

    static string Key(string drink) => "spill_" + drink;
}