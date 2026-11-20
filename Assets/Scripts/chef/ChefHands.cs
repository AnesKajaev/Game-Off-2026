using UnityEngine;

public class ChefHands : MonoBehaviour
{
    public static ChefHands Instance { get; private set; }

    public enum Kind { Empty, Raw, Cooked }

    public Kind kind { get; private set; }
    public MeatType meat { get; private set; }
    public float quality { get; private set; }
    public Doneness doneness { get; private set; }
    public int nuggets { get; private set; }
    public OrderNote forNote { get; private set; }
    public bool Empty => kind == Kind.Empty;

    string toast = "";
    float toastTimer;

    void Awake() => Instance = this;

    public void HoldRaw(MeatType m)
    {
        kind = Kind.Raw;
        meat = m;
        quality = 0f;
        forNote = null;
    }

    public void HoldCooked(MeatType m, float q, Doneness d = Doneness.Medium, int count = 0, OrderNote note = null)
    {
        kind = Kind.Cooked;
        meat = m;
        quality = q;
        doneness = d;
        nuggets = count;
        forNote = note;
    }

    public void Clear()
    {
        OrderBoard.Instance.Release(forNote);
        kind = Kind.Empty;
        quality = 0f;
        forNote = null;
    }

    public void Say(string msg)
    {
        toast = msg;
        toastTimer = 2.5f;
    }

    void Update()
    {
        if (toastTimer > 0f) toastTimer -= Time.deltaTime;
    }

    string HeldText()
    {
        switch (kind)
        {
            case Kind.Raw:
                return "Holding: raw " + MeatInfo.Raw(meat).ToLower();
            case Kind.Cooked:
                string extra = meat == MeatType.Beef ? ", " + DonenessInfo.Name(doneness) : meat == MeatType.Fish ? " x" + nuggets : "";
                return "Holding: " + MeatInfo.Dish(meat) + extra + " (" + Mathf.RoundToInt(quality * 100f) + "%)";
            default:
                return "Hands: empty";
        }
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !PlayerMovement.CurrentIs(PlayerMovement.Role.Chef)) return;

        var box = new Rect(16f, Screen.height - 56f, 460f, 40f);
        GuiDraw.Frame(box);
        GuiDraw.Label(box, HeldText(), 18, Color.white);

        if (toastTimer > 0f)
        {
            var r = new Rect(Screen.width / 2f - 260f, 80f, 520f, 40f);
            GuiDraw.Frame(r);
            GuiDraw.Label(r, toast, 18, Color.white);
        }
    }
}