using UnityEngine;
using UnityEngine.InputSystem;

public class Stove : MonoBehaviour
{
    public enum State { Empty, Cooking, Ready, Burnt }

    public const int Count = 4;

    public BeefMinigame beefMinigame;
    public Transform labelPoint;

    [Header("Cooking")]
    public float cookTime = 20f;
    public float minHeatRate = 0.5f;
    public float maxHeatRate = 1.6f;
    public float perfectUntil = 1.25f;
    public float burnAt = 1.5f;

    class Slot
    {
        public State state;
        public MeatType meat;
        public float seasoningScore;
        public float heatScore;
        public float heat;
        public float progress;
        public float cookTimeScale = 1f;
    }

    readonly Slot[] slots = new Slot[Count];
    bool viewOpen;

    static Rect N(float x, float y, float w, float h) => new Rect(x * Screen.width, y * Screen.height, w * Screen.width, h * Screen.height);
    static int FS(float f) => Mathf.Max(10, Mathf.RoundToInt(Screen.height * f));

    Rect ViewCell(int i) => N(0.31f + (i % 2) * 0.19f, 0.3f + (i / 2) * 0.24f, 0.17f, 0.21f);

    void Awake()
    {
        for (int i = 0; i < Count; i++) slots[i] = new Slot();
    }

    public bool IsFree(int i) => slots[i].state == State.Empty;
    public MeatType MeatOf(int i) => slots[i].meat;

    public bool HasFreeSlot
    {
        get
        {
            foreach (var s in slots) if (s.state == State.Empty) return true;
            return false;
        }
    }

    bool AnyUsed
    {
        get
        {
            foreach (var s in slots) if (s.state != State.Empty) return true;
            return false;
        }
    }

    public void StartCooking(int i, MeatType m, float seasoning, float thermostat, float heatMatch)
    {
        var s = slots[i];
        s.meat = m;
        s.seasoningScore = seasoning;
        s.heat = thermostat;
        s.heatScore = heatMatch;
        s.progress = 0f;
        s.cookTimeScale = MeatRecipe.For(m).cookTimeScale;
        s.state = State.Cooking;
    }

    public void Interact()
    {
        var hands = ChefHands.Instance;
        if (hands == null || viewOpen) return;

        if (hands.kind == ChefHands.Kind.Raw)
        {
            if (beefMinigame == null)
            {
                hands.Say("The stove isn't set up yet");
                return;
            }
            if (!HasFreeSlot)
            {
                hands.Say("All the stove tops are busy");
                return;
            }
            beefMinigame.Open(this, hands.meat);
            return;
        }

        if (hands.kind == ChefHands.Kind.Cooked)
        {
            hands.Say("Your hands are full");
            return;
        }

        if (!AnyUsed)
        {
            hands.Say("Nothing is cooking. Get some meat from the fridge");
            return;
        }

        viewOpen = true;
        PlayerMovement.inputLocked = true;
    }

    void CloseView()
    {
        viewOpen = false;
        PlayerMovement.inputLocked = false;
    }

    float Quality(Slot s)
    {
        if (s.state == State.Burnt) return 0.05f;
        float doneness = s.progress <= perfectUntil ? 1f : Mathf.Clamp01(1f - (s.progress - perfectUntil) / (burnAt - perfectUntil));
        return s.seasoningScore * 0.4f + s.heatScore * 0.3f + doneness * 0.3f;
    }

    void Update()
    {
        for (int i = 0; i < Count; i++) UpdateSlot(i);
        if (viewOpen) HandleView();
    }

    void UpdateSlot(int i)
    {
        var s = slots[i];
        if (s.state == State.Empty || s.state == State.Burnt) return;

        s.progress += Time.deltaTime * Mathf.Lerp(minHeatRate, maxHeatRate, s.heat) / (cookTime * s.cookTimeScale);

        if (s.state == State.Cooking && s.progress >= 1f)
        {
            s.state = State.Ready;
            if (ChefHands.Instance != null) ChefHands.Instance.Say(MeatInfo.Raw(s.meat) + " on place " + (i + 1) + " is ready!");
        }

        if (s.progress >= burnAt)
        {
            s.state = State.Burnt;
            if (ChefHands.Instance != null) ChefHands.Instance.Say(MeatInfo.Raw(s.meat) + " on place " + (i + 1) + " burnt!");
        }
    }

    void HandleView()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null && kb[Key.Escape].wasPressedThisFrame) { CloseView(); return; }
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Vector2 m = GuiDraw.MousePos();
        for (int i = 0; i < Count; i++)
        {
            if (!ViewCell(i).Contains(m)) continue;

            var s = slots[i];
            var hands = ChefHands.Instance;
            if (s.state != State.Ready && s.state != State.Burnt) return;
            if (hands == null || !hands.Empty) return;

            hands.HoldCooked(s.meat, Quality(s));
            hands.Say("Took the " + MeatInfo.Dish(s.meat).ToLower() + " from place " + (i + 1));
            s.state = State.Empty;
            CloseView();
            return;
        }
    }

    Color BarColor(Slot s)
    {
        if (s.state == State.Burnt) return new Color(0.5f, 0.1f, 0.1f);
        if (s.state == State.Ready) return s.progress <= perfectUntil ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.9f, 0.75f, 0.2f);
        return new Color(0.95f, 0.55f, 0.15f);
    }

    void DrawProgress(Rect bar, Slot s)
    {
        GuiDraw.Fill(bar, new Color(0.12f, 0.12f, 0.12f));
        GuiDraw.Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(s.progress / burnAt), bar.height), BarColor(s));
        GuiDraw.Fill(new Rect(bar.x + bar.width / burnAt - 1f, bar.y - 2f, 2f, bar.height + 4f), Color.white);
    }

    string StatusText(Slot s)
    {
        if (s.state == State.Burnt) return "Burnt!";
        if (s.state == State.Ready) return "Ready!";
        return Mathf.RoundToInt(Mathf.Min(s.progress, 1f) * 100f) + "%";
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (!PlayerMovement.CurrentIs(PlayerMovement.Role.Chef)) return;

        if (viewOpen) DrawView();
        else if (!BeefMinigame.Active) DrawWorldBars();
    }

    void DrawWorldBars()
    {
        if (!AnyUsed) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(labelPoint != null ? labelPoint.position : transform.position + Vector3.up * 0.8f);
        if (sp.z <= 0f) return;

        float w = 50f;
        float gap = 6f;
        float total = Count * w + (Count - 1) * gap;
        float x0 = sp.x - total / 2f;
        float y = Screen.height - sp.y;

        GuiDraw.Fill(new Rect(x0 - 6f, y - 24f, total + 12f, 42f), new Color(0f, 0f, 0f, 0.55f));
        for (int i = 0; i < Count; i++)
        {
            var s = slots[i];
            float x = x0 + i * (w + gap);
            GuiDraw.Label(new Rect(x, y - 22f, w, 18f), (i + 1) + (s.state == State.Ready ? " !" : ""), 13, Color.white);
            if (s.state == State.Empty) GuiDraw.Fill(new Rect(x, y, w, 8f), new Color(0.2f, 0.2f, 0.2f));
            else DrawProgress(new Rect(x, y, w, 8f), s);
        }
    }

    void DrawView()
    {
        Vector2 m = GuiDraw.MousePos();

        GuiDraw.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.55f));
        GuiDraw.Fill(N(0.28f, 0.16f, 0.44f, 0.68f), new Color(0.2f, 0.2f, 0.22f, 0.98f));
        GuiDraw.Label(N(0.28f, 0.18f, 0.44f, 0.08f), "Stove", FS(0.05f), Color.white);

        for (int i = 0; i < Count; i++)
        {
            var s = slots[i];
            Rect c = ViewCell(i);
            bool done = s.state == State.Ready || s.state == State.Burnt;
            bool hover = done && c.Contains(m);

            Color fill;
            if (s.state == State.Empty) fill = new Color(0.14f, 0.14f, 0.16f);
            else if (s.state == State.Cooking) fill = new Color(0.35f, 0.2f, 0.1f);
            else if (s.state == State.Ready) fill = hover ? new Color(0.3f, 0.6f, 0.3f) : new Color(0.2f, 0.45f, 0.2f);
            else fill = hover ? new Color(0.5f, 0.2f, 0.2f) : new Color(0.35f, 0.12f, 0.12f);

            GuiDraw.Fill(c, fill);
            GuiDraw.Label(new Rect(c.x, c.y, c.width, c.height * 0.22f), "Place " + (i + 1), FS(0.02f), new Color(0.85f, 0.85f, 0.9f));

            if (s.state == State.Empty)
            {
                GuiDraw.Label(c, "Empty", FS(0.024f), new Color(0.55f, 0.55f, 0.6f));
                continue;
            }

            GuiDraw.Label(new Rect(c.x, c.y + c.height * 0.25f, c.width, c.height * 0.3f), MeatInfo.Raw(s.meat) + "  " + StatusText(s), FS(0.022f), Color.white);
            DrawProgress(new Rect(c.x + c.width * 0.1f, c.y + c.height * 0.62f, c.width * 0.8f, c.height * 0.08f), s);
            if (done) GuiDraw.Label(new Rect(c.x, c.y + c.height * 0.76f, c.width, c.height * 0.2f), "Click to take", FS(0.02f), Color.white);
        }

        GuiDraw.Label(N(0.28f, 0.78f, 0.44f, 0.05f), "Click a finished dish to take it   (Esc to close)", FS(0.02f), new Color(0.75f, 0.75f, 0.8f));
    }
}