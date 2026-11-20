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