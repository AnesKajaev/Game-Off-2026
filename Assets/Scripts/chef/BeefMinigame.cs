using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeefMinigame : MonoBehaviour
{
    public static bool Active { get; private set; }

    [Header("Optional art (plain boxes are drawn while these are empty)")]
    [Tooltip("Order: Fish, Chicken, Beef, Ground meat")]
    public Texture2D[] meatTextures = new Texture2D[4];
    public Texture2D saltTexture;
    [Tooltip("The second seasoning, same order: Parsley, Paprika, Oregano, Garlic")]
    public Texture2D[] secondTextures = new Texture2D[4];
    public Texture2D stoveTexture;

    [Header("Seasoning")]
    public float minShakeSpeed = 1f;
    public float amountPerTravel = 0.4f;
    public float targetAmount = 1f;
    public float maxAmount = 1.3f;
    public float returnSpeed = 10f;
    public float autoAdvanceDelay = 0.8f;

    [Header("Thermostat")]
    public float minTemp = 80f;
    public float maxTemp = 260f;
    public bool showIdealZone = true;

    class Item
    {
        public string label;
        public Texture2D tex;
        public Color color;
        public Vector2 homeN;
        public Vector2 posN;
        public float sizeH;
        public float amount;
        public bool held;
    }

    struct Speck
    {
        public Vector2 p;
        public Color c;
    }

    enum Phase { Closed, Seasoning, Stove }

    Phase phase;
    MeatType meatType;
    MeatRecipe recipe;
    Stove stove;
    int slot = -1;
    Item salt, second, meat, held;
    readonly List<Speck> specks = new List<Speck>();
    Vector2 grabOffset, lastMouse;
    float shakeSpeed, speckAcc, advanceTimer, thermo = 0.5f;
    bool draggingThermo, meatInPan;

    static Rect N(float x, float y, float w, float h) => new Rect(x * Screen.width, y * Screen.height, w * Screen.width, h * Screen.height);
    static Vector2 Px(Vector2 n) => new Vector2(n.x * Screen.width, n.y * Screen.height);
    static int FS(float f) => Mathf.Max(10, Mathf.RoundToInt(Screen.height * f));
    static Texture2D At(Texture2D[] a, MeatType m) { int i = (int)m; return a != null && i >= 0 && i < a.Length ? a[i] : null; }

    Rect PanelRect => N(0.04f, 0.06f, 0.92f, 0.88f);
    Rect MeatRect => N(0.08f, 0.22f, 0.36f, 0.32f);
    Rect ActionRect => N(0.76f, 0.82f, 0.18f, 0.08f);
    Rect StoveRect => N(0.5f, 0.2f, 0.42f, 0.5f);
    Rect TrackRect => N(0.1f, 0.8f, 0.5f, 0.035f);

    Rect Cell(int i)
    {
        Rect s = StoveRect;
        float cw = s.width * 0.44f;
        float ch = s.height * 0.44f;
        float gx = (s.width - 2f * cw) / 3f;
        float gy = (s.height - 2f * ch) / 3f;
        int col = i % 2;
        int row = i / 2;
        return new Rect(s.x + gx + col * (cw + gx), s.y + gy + row * (ch + gy), cw, ch);
    }

    int CellAt(Vector2 p)
    {
        for (int i = 0; i < 4; i++)
            if (Cell(i).Contains(p)) return i;
        return -1;
    }

    static Rect ItemRect(Item it)
    {
        float s = it.sizeH * Screen.height;
        Vector2 c = Px(it.posN);
        return new Rect(c.x - s / 2f, c.y - s / 2f, s, s);
    }

    public void Open(Stove s, MeatType type)
    {
        if (phase != Phase.Closed || s == null) return;
        stove = s;
        meatType = type;
        recipe = MeatRecipe.For(type);
        slot = -1;

        salt = new Item { label = "Salt", tex = saltTexture, color = new Color(0.92f, 0.92f, 0.95f), homeN = new Vector2(0.72f, 0.3f), sizeH = 0.17f };
        second = new Item { label = recipe.secondName, tex = At(secondTextures, type), color = recipe.secondColor, homeN = new Vector2(0.72f, 0.58f), sizeH = 0.17f };
        salt.posN = salt.homeN;
        second.posN = second.homeN;
        meat = null;
        held = null;

        specks.Clear();
        shakeSpeed = speckAcc = advanceTimer = 0f;
        thermo = 0.5f;
        draggingThermo = false;
        meatInPan = false;
        lastMouse = GuiDraw.MousePos();

        phase = Phase.Seasoning;
        Active = true;
        PlayerMovement.inputLocked = true;
    }

    public void Close()
    {
        phase = Phase.Closed;
        stove = null;
        held = null;
        Active = false;
        PlayerMovement.inputLocked = false;
    }

    void Update()
    {
        if (phase == Phase.Closed) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null && kb[Key.Escape].wasPressedThisFrame) { Close(); return; }
        if (mouse == null) return;

        Vector2 m = GuiDraw.MousePos();
        bool pressed = mouse.leftButton.wasPressedThisFrame;
        bool down = mouse.leftButton.isPressed;
        float dt = Time.deltaTime;

        if (phase == Phase.Seasoning) UpdateSeasoning(m, pressed, down, dt);
        else UpdateStove(m, pressed, down, dt);

        lastMouse = m;
    }

    void UpdateSeasoning(Vector2 m, bool pressed, bool down, float dt)
    {
        if (pressed)
        {
            if (ActionRect.Contains(m)) { GoToStove(); return; }
            foreach (var it in new[] { second, salt })
            {
                if (!ItemRect(it).Contains(m)) continue;
                held = it;
                held.held = true;
                grabOffset = Px(it.posN) - m;
                break;
            }
        }

        if (held != null)
        {
            if (down)
            {
                Vector2 target = m + grabOffset;
                held.posN = new Vector2(target.x / Screen.width, target.y / Screen.height);

                float travel = (m - lastMouse).magnitude / Screen.height;
                float speed = dt > 0f ? travel / dt : 0f;
                shakeSpeed = Mathf.Lerp(shakeSpeed, speed, 0.35f);

                if (MeatRect.Contains(Px(held.posN)) && shakeSpeed > minShakeSpeed)
                {
                    float add = travel * amountPerTravel;
                    held.amount = Mathf.Min(maxAmount, held.amount + add);
                    AddSpecks(held, add);
                }
            }
            else
            {
                held.held = false;
                held = null;
                shakeSpeed = 0f;
            }
        }

        foreach (var it in new[] { salt, second })
            if (!it.held) it.posN = Vector2.Lerp(it.posN, it.homeN, 1f - Mathf.Exp(-returnSpeed * dt));

        if (salt.amount >= targetAmount && second.amount >= targetAmount)
        {
            advanceTimer += dt;
            if (advanceTimer >= autoAdvanceDelay) GoToStove();
        }
        else
        {
            advanceTimer = 0f;
        }
    }

    void AddSpecks(Item it, float amount)
    {
        speckAcc += amount * 50f;
        Rect mr = MeatRect;
        float cx = (Px(it.posN).x - mr.x) / mr.width;
        Color c = it == salt ? Color.white : recipe.secondColor;

        while (speckAcc >= 1f)
        {
            speckAcc -= 1f;
            if (specks.Count >= 220) specks.RemoveAt(0);
            specks.Add(new Speck
            {
                p = new Vector2(Mathf.Clamp01(cx + Random.Range(-0.12f, 0.12f)), Random.Range(0.08f, 0.92f)),
                c = c
            });
        }
    }

    void GoToStove()
    {
        if (held != null) { held.held = false; held = null; }
        meat = new Item { label = MeatInfo.Raw(meatType), tex = At(meatTextures, meatType), color = recipe.meatColor, homeN = new Vector2(0.24f, 0.42f), sizeH = 0.22f };
        meat.posN = meat.homeN;
        meatInPan = false;
        slot = -1;
        phase = Phase.Stove;
    }

    void UpdateStove(Vector2 m, bool pressed, bool down, float dt)
    {
        Rect track = TrackRect;
        Rect trackHit = new Rect(track.x, track.y - Screen.height * 0.03f, track.width, track.height + Screen.height * 0.06f);

        if (pressed)
        {
            if (meatInPan && ActionRect.Contains(m)) { Finish(); return; }

            if (trackHit.Contains(m))
            {
                draggingThermo = true;
            }
            else if (ItemRect(meat).Contains(m))
            {
                held = meat;
                held.held = true;
                meatInPan = false;
                slot = -1;
                grabOffset = Px(meat.posN) - m;
            }
        }

        if (draggingThermo)
        {
            if (down) thermo = Mathf.Clamp01((m.x - track.x) / track.width);
            else draggingThermo = false;
        }

        if (held != null)
        {
            if (down)
            {
                Vector2 target = m + grabOffset;
                meat.posN = new Vector2(target.x / Screen.width, target.y / Screen.height);
            }
            else
            {
                held.held = false;
                held = null;
                int cell = CellAt(Px(meat.posN));
                if (cell >= 0 && stove.IsFree(cell))
                {
                    Rect pan = Cell(cell);
                    slot = cell;
                    meatInPan = true;
                    meat.posN = new Vector2((pan.x + pan.width / 2f) / Screen.width, (pan.y + pan.height / 2f) / Screen.height);
                }
            }
        }
        else if (!meatInPan)
        {
            meat.posN = Vector2.Lerp(meat.posN, meat.homeN, 1f - Mathf.Exp(-returnSpeed * dt));
        }
    }

    float Closeness(float amount) => Mathf.Clamp01(1f - Mathf.Abs(amount - targetAmount) / targetAmount);

    float SeasoningScore() => (Closeness(salt.amount) + Closeness(second.amount)) / 2f;

    float HeatScore()
    {
        float d = thermo < recipe.idealMin ? recipe.idealMin - thermo : thermo > recipe.idealMax ? thermo - recipe.idealMax : 0f;
        return Mathf.Clamp01(1f - d / 0.35f);
    }

    void Finish()
    {
        Stove st = stove;
        int chosen = slot;
        float seasoning = SeasoningScore();
        float heatScore = HeatScore();
        float heat = thermo;
        Close();

        if (st == null || chosen < 0) return;
        st.StartCooking(chosen, meatType, seasoning, heat, heatScore);

        var hands = ChefHands.Instance;
        if (hands == null) return;
        hands.Clear();
        hands.Say(MeatInfo.Raw(meatType) + " is cooking on place " + (chosen + 1));
    }

    static void DrawBox(Rect r, Texture2D tex, Color fallback, string label, int size)
    {
        if (tex != null)
        {
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
            return;
        }
        GuiDraw.Fill(r, fallback);
        GuiDraw.Label(r, label, size, fallback.grayscale > 0.6f ? Color.black : Color.white);
    }

    static void DrawSpecks(List<Speck> list, Rect r)
    {
        float s = Mathf.Max(2f, r.height * 0.025f);
        foreach (var sp in list)
            GuiDraw.Fill(new Rect(r.x + sp.p.x * r.width - s / 2f, r.y + sp.p.y * r.height - s / 2f, s, s), sp.c);
    }

    void DrawItem(Item it)
    {
        Rect r = ItemRect(it);
        GuiDraw.Fill(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), new Color(0f, 0f, 0f, 0.3f));
        DrawBox(r, it.tex, it.color, it.label, FS(0.024f));
    }

    void DrawBar(Rect r, string label, float amount, Color c)
    {
        GuiDraw.Label(new Rect(r.x, r.y - Screen.height * 0.04f, r.width, Screen.height * 0.04f),
            label + "  " + Mathf.RoundToInt(amount / targetAmount * 100f) + "%", FS(0.022f), Color.white, TextAnchor.MiddleLeft);
        GuiDraw.Fill(r, new Color(0.1f, 0.08f, 0.07f));
        GuiDraw.Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(amount / maxAmount), r.height), c);
        GuiDraw.Fill(new Rect(r.x + r.width * (targetAmount / maxAmount) - 1.5f, r.y - 3f, 3f, r.height + 6f), Color.white);
    }

    void OnGUI()
    {
        if (phase == Phase.Closed || Event.current.type != EventType.Repaint) return;

        Vector2 m = GuiDraw.MousePos();
        GuiDraw.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));
        GuiDraw.Fill(PanelRect, new Color(0.2f, 0.15f, 0.12f, 0.98f));

        if (phase == Phase.Seasoning) DrawSeasoning(m);
        else DrawStove(m);
    }

    void DrawSeasoning(Vector2 m)
    {
        GuiDraw.Label(N(0.08f, 0.08f, 0.84f, 0.08f), "Season the " + MeatInfo.Raw(meatType).ToLower(), FS(0.05f), Color.white, TextAnchor.MiddleLeft);
        GuiDraw.Label(N(0.08f, 0.15f, 0.84f, 0.05f), "Drag the salt and the " + recipe.secondName.ToLower() + " over the meat and shake them. Let go and they go back.",
            FS(0.024f), new Color(0.8f, 0.75f, 0.7f), TextAnchor.MiddleLeft);

        Rect mr = MeatRect;
        GuiDraw.Fill(new Rect(mr.x + 5f, mr.y + 5f, mr.width, mr.height), new Color(0f, 0f, 0f, 0.3f));
        DrawBox(mr, At(meatTextures, meatType), recipe.meatColor, MeatInfo.Raw(meatType), FS(0.04f));
        DrawSpecks(specks, mr);

        DrawBar(N(0.08f, 0.66f, 0.36f, 0.03f), "Salt", salt.amount, new Color(0.9f, 0.9f, 0.95f));
        DrawBar(N(0.08f, 0.76f, 0.36f, 0.03f), recipe.secondName, second.amount, recipe.secondColor);

        if (held != salt) DrawItem(salt);
        if (held != second) DrawItem(second);
        if (held != null) DrawItem(held);

        GuiDraw.Button(ActionRect, "Skip", ActionRect.Contains(m), true, FS(0.034f));
    }

    void DrawStove(Vector2 m)
    {
        GuiDraw.Label(N(0.08f, 0.08f, 0.84f, 0.08f), "Cook the " + MeatInfo.Raw(meatType).ToLower(), FS(0.05f), Color.white, TextAnchor.MiddleLeft);
        GuiDraw.Label(N(0.08f, 0.15f, 0.84f, 0.05f), "Drag the meat onto a free burner, then set the thermostat.",
            FS(0.024f), new Color(0.8f, 0.75f, 0.7f), TextAnchor.MiddleLeft);

        Rect s = StoveRect;
        if (stoveTexture != null) GUI.DrawTexture(s, stoveTexture, ScaleMode.ScaleToFit);
        else GuiDraw.Fill(s, new Color(0.28f, 0.28f, 0.3f));

        for (int i = 0; i < 4; i++)
        {
            Rect c = Cell(i);
            Rect top = new Rect(c.x, c.y, c.width, c.height * 0.25f);
            if (i == slot)
            {
                GuiDraw.Fill(new Rect(c.x - 6f, c.y - 6f, c.width + 12f, c.height + 12f), new Color(1f, 0.55f, 0.15f, 0.25f + 0.55f * thermo));
                GuiDraw.Fill(c, new Color(0.18f + 0.5f * thermo, 0.12f, 0.1f));
                GuiDraw.Label(top, "Place " + (i + 1), FS(0.02f), Color.white);
            }
            else if (stove.IsFree(i))
            {
                bool hover = held != null && c.Contains(Px(meat.posN));
                GuiDraw.Fill(c, hover ? new Color(0.45f, 0.4f, 0.3f) : new Color(0.22f, 0.22f, 0.26f));
                GuiDraw.Label(top, "Place " + (i + 1), FS(0.02f), new Color(0.75f, 0.75f, 0.8f));
            }
            else
            {
                GuiDraw.Fill(c, new Color(0.3f, 0.14f, 0.12f));
                GuiDraw.Label(top, "Place " + (i + 1), FS(0.02f), new Color(0.8f, 0.6f, 0.55f));
                GuiDraw.Label(c, MeatInfo.Raw(stove.MeatOf(i)) + "\nBusy", FS(0.022f), new Color(0.9f, 0.75f, 0.7f));
            }
        }

        DrawItem(meat);
        Rect mr = ItemRect(meat);
        DrawSpecks(specks, mr);

        Rect track = TrackRect;
        int temp = Mathf.RoundToInt(Mathf.Lerp(minTemp, maxTemp, thermo));
        GuiDraw.Label(new Rect(track.x, track.y - Screen.height * 0.075f, track.width, Screen.height * 0.05f),
            "Thermostat   " + temp + " C", FS(0.028f), Color.white, TextAnchor.MiddleLeft);

        int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            GuiDraw.Fill(new Rect(track.x + t * track.width, track.y, track.width / segments + 1f, track.height),
                Color.Lerp(new Color(0.3f, 0.5f, 0.9f), new Color(0.95f, 0.3f, 0.2f), t));
        }

        if (showIdealZone)
            GuiDraw.Fill(new Rect(track.x + recipe.idealMin * track.width, track.y - track.height * 0.45f, (recipe.idealMax - recipe.idealMin) * track.width, track.height * 0.3f),
                new Color(0.4f, 0.9f, 0.4f, 0.9f));

        float kw = Screen.height * 0.035f;
        var knob = new Rect(track.x + thermo * track.width - kw / 2f, track.y - track.height * 0.4f, kw, track.height * 1.8f);
        GuiDraw.Fill(knob, Color.white);

        float ly = track.y + track.height * 1.5f;
        GuiDraw.Label(new Rect(track.x, ly, 80f, 24f), "Low", FS(0.02f), new Color(0.8f, 0.75f, 0.7f), TextAnchor.MiddleLeft);
        GuiDraw.Label(new Rect(track.x + track.width / 2f - 40f, ly, 80f, 24f), "Medium", FS(0.02f), new Color(0.8f, 0.75f, 0.7f));
        GuiDraw.Label(new Rect(track.xMax - 80f, ly, 80f, 24f), "High", FS(0.02f), new Color(0.8f, 0.75f, 0.7f), TextAnchor.MiddleRight);

        GuiDraw.Button(ActionRect, "Start cooking", meatInPan && ActionRect.Contains(m), meatInPan, FS(0.03f));
    }
}