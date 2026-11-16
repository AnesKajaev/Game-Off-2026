using UnityEngine;

public enum MeatType { Fish, Chicken, Beef, Ground }

public static class MeatInfo
{
    public static string Raw(MeatType m)
    {
        switch (m)
        {
            case MeatType.Fish: return "Fish";
            case MeatType.Chicken: return "Chicken";
            case MeatType.Beef: return "Beef";
            default: return "Ground meat";
        }
    }

    public static string Dish(MeatType m)
    {
        switch (m)
        {
            case MeatType.Fish: return "Fish nuggets";
            case MeatType.Chicken: return "Grilled chicken";
            case MeatType.Beef: return "Beef";
            default: return "Meatballs";
        }
    }
}

public class MeatRecipe
{
    public Color meatColor;
    public string secondName;
    public Color secondColor;
    public float idealMin;
    public float idealMax;
    public float cookTimeScale = 1f;

    public static MeatRecipe For(MeatType m)
    {
        switch (m)
        {
            case MeatType.Fish:
                return new MeatRecipe
                {
                    meatColor = new Color(0.9f, 0.62f, 0.56f),
                    secondName = "Parsley",
                    secondColor = new Color(0.35f, 0.72f, 0.3f),
                    idealMin = 0.35f,
                    idealMax = 0.6f,
                    cookTimeScale = 0.7f
                };
            case MeatType.Chicken:
                return new MeatRecipe
                {
                    meatColor = new Color(0.93f, 0.78f, 0.62f),
                    secondName = "Paprika",
                    secondColor = new Color(0.9f, 0.42f, 0.14f),
                    idealMin = 0.5f,
                    idealMax = 0.75f,
                    cookTimeScale = 1.1f
                };
            case MeatType.Ground:
                return new MeatRecipe
                {
                    meatColor = new Color(0.55f, 0.25f, 0.22f),
                    secondName = "Garlic",
                    secondColor = new Color(0.96f, 0.9f, 0.6f),
                    idealMin = 0.5f,
                    idealMax = 0.7f,
                    cookTimeScale = 0.9f
                };
            default:
                return new MeatRecipe
                {
                    meatColor = new Color(0.62f, 0.2f, 0.18f),
                    secondName = "Oregano",
                    secondColor = new Color(0.28f, 0.55f, 0.25f),
                    idealMin = 0.55f,
                    idealMax = 0.8f,
                    cookTimeScale = 1f
                };
        }
    }
}