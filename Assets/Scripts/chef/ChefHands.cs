using UnityEngine;

public class ChefHands : MonoBehaviour
{
    public static ChefHands Instance { get; private set; }

    public enum Kind { Empty, Raw, Cooked }

    public Kind kind { get; private set; }
    public MeatType meat { get; private set; }
    public float quality { get; private set; }
    public bool Empty => kind == Kind.Empty;

    string toast = "";
    float toastTimer;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void HoldRaw(MeatType m)
    {
        kind = Kind.Raw;
        meat = m;
        quality = 0f;
    }

    public void HoldCooked(MeatType m, float q)
    {
        kind = Kind.Cooked;
        meat = m;
        quality = q;
    }

    public void Clear()
    {
        kind = Kind.Empty;
        quality = 0f;
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
            case Kind.Raw: return "Holding: raw " + MeatInfo.Raw(meat).ToLower();
            case Kind.Cooked: return "Holding: " + MeatInfo.Dish(meat) + " (" + Mathf.RoundToInt(quality * 100f) + "%)";
            default: return "Hands: empty";
        }
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !PlayerMovement.CurrentIs(PlayerMovement.Role.Chef)) return;

        var box = new Rect(16f, Screen.height - 56f, 360f, 40f);
        GuiDraw.Fill(box, new Color(0f, 0f, 0f, 0.55f));
        GuiDraw.Label(box, HeldText(), 18, Color.white);

        if (toastTimer > 0f)
        {
            var r = new Rect(Screen.width / 2f - 260f, 80f, 520f, 40f);
            float a = Mathf.Clamp01(toastTimer);
            GuiDraw.Fill(r, new Color(0f, 0f, 0f, 0.65f * a));
            GuiDraw.Label(r, toast, 18, new Color(1f, 1f, 1f, a));
        }
    }
}