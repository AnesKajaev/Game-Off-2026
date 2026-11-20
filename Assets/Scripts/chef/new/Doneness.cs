public enum Doneness { Raw, Rare, MediumRare, Medium, MediumWell, WellDone, Overcooked }

public static class DonenessInfo
{
    public static string Name(Doneness d)
    {
        switch (d)
        {
            case Doneness.Raw: return "raw";
            case Doneness.Rare: return "rare";
            case Doneness.MediumRare: return "medium rare";
            case Doneness.Medium: return "medium";
            case Doneness.MediumWell: return "medium well";
            case Doneness.WellDone: return "well done";
            default: return "overcooked";
        }
    }
}