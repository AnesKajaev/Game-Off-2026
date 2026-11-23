public class Dish
{
    public MeatType meat;
    public float quality;
    public Doneness doneness;
    public int nuggets;
    public OrderNote note;

    public string Name
    {
        get
        {
            string extra = meat == MeatType.Beef ? ", " + DonenessInfo.Name(doneness) : meat == MeatType.Fish ? " x" + nuggets : "";
            return MeatInfo.Dish(meat) + extra;
        }
    }

    public string Label => note != null ? Name + "  (" + note.title + ")" : Name;
}