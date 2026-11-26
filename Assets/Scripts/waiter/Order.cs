using System.Collections.Generic;
using UnityEngine;

public static class Menu
{
    public static readonly string[] Meats = { "Beef", "Grilled chicken", "Fish nuggets", "Meatballs" };
    public static readonly string[] Soups = { "Tomato soup", "Mushroom soup", "Vegetable soup" };
    public static readonly int[] NuggetCounts = { 4, 6, 8, 9, 10, 12 };
    public const string Sandwich = "Sandwich";

    public static string Pick(string[] options) => options[Random.Range(0, options.Length)];
}

[System.Serializable]
public class Order
{
    public static bool AlwaysIncludeMeat = true;

    public string meat;
    public string extra;
    public Doneness doneness;
    public int nuggets;

    public static Order Generate()
    {
        int mask = Random.Range(1, 4);
        if (AlwaysIncludeMeat) mask |= 1;

        var o = new Order();
        if ((mask & 1) != 0)
        {
            o.meat = Menu.Pick(Menu.Meats);
            if (o.meat == "Beef")
                o.doneness = (Doneness)Random.Range((int)Doneness.Rare, (int)Doneness.WellDone + 1);
            else if (o.meat == "Fish nuggets")
                o.nuggets = Menu.NuggetCounts[Random.Range(0, Menu.NuggetCounts.Length)];
        }
        if ((mask & 2) != 0) o.extra = Random.value < 0.5f ? Menu.Pick(Menu.Soups) : Menu.Sandwich;
        return o;
    }

    public bool HasMeat(MeatType m) => !string.IsNullOrEmpty(meat) && meat == MeatInfo.Dish(m);

    public string MeatText()
    {
        if (string.IsNullOrEmpty(meat)) return "";
        if (meat == "Beef") return "Beef (" + DonenessInfo.Name(doneness) + ")";
        if (meat == "Fish nuggets") return "Fish nuggets (x" + nuggets + ")";
        return meat;
    }

    public List<string> Lines()
    {
        var l = new List<string>();
        if (!string.IsNullOrEmpty(meat)) l.Add("Meat: " + MeatText());
        if (!string.IsNullOrEmpty(extra)) l.Add("Extra: " + extra);
        return l;
    }

    public string Speech()
    {
        var items = new List<string>();
        if (!string.IsNullOrEmpty(meat))
        {
            if (meat == "Beef") items.Add(DonenessInfo.Name(doneness) + " beef");
            else if (meat == "Fish nuggets") items.Add(nuggets + " fish nuggets");
            else items.Add(meat.ToLower());
        }
        if (!string.IsNullOrEmpty(extra))
            items.Add(extra == Menu.Sandwich ? "a sandwich" : extra.ToLower());

        return "I'd like " + (items.Count == 1 ? items[0] : items[0] + " and " + items[1]) + ".";
    }
}