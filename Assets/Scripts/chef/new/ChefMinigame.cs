using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChefMinigame : MonoBehaviour
{
    public static bool Active { get; private set; }

    [System.Serializable]
    public class ArtSlot
    {
        public string name;
        public Texture2D texture;
    }

    public static readonly string[] ArtNames =
    {
        "meat", "salt", "oregano", "pan", "garlic", "oil", "spices", "egg", "bowl", "plate",
        "chicken", "brush", "vinegar", "lemon", "grill",
        "fish-head", "fish-body", "fish-tail", "cleaver", "nugget",
        "ground-meat", "meatball"
    };

    public ArtSlot[] art;

    [Header("Tuning")]
    public float secondsPerMinute = 6f;
    public float sideSeconds = 6f;
    public int ballsToMake = 5;

    enum Phase { Closed, Steps, Results }

    readonly Dictionary<string, Texture2D> lookup = new Dictionary<string, Texture2D>();
    readonly List<ScoreLine> lines = new List<ScoreLine>();

    Phase phase;
    ChefCtx ctx;
    List<ChefStep> steps;
    int stepIndex;
    float total;
    OrderNote note;
    Vector2 lastMouse;

    Rect ActionRect => ChefStep.N(0.76f, 0.86f, 0.18f, 0.07f);

    [ContextMenu("Fill art slots")]
    void FillArtSlots()
    {
        var list = new List<ArtSlot>(art ?? new ArtSlot[0]);
        foreach (var n in ArtNames)
        {
            bool has = false;
            foreach (var s in list) if (s.name == n) has = true;
            if (!has) list.Add(new ArtSlot { name = n });
        }
        art = list.ToArray();
    }

    void Reset() => FillArtSlots();

    Texture2D Lookup(string n)
    {
        Texture2D t;
        return lookup.TryGetValue(n, out t) ? t : null;
    }

    public void Open(MeatType meat)
    {
        lookup.Clear();
        foreach (var s in art)
            if (s.texture != null) lookup[s.name] = s.texture;

        ctx = new ChefCtx
        {
            meat = meat,
            secondsPerMinute = secondsPerMinute,
            sideSeconds = sideSeconds,
            ballsToMake = ballsToMake,
            art = Lookup
        };

        note = OrderBoard.Instance.Claim(meat);
        if (note != null)
        {
            ctx.order = note.order;
            ctx.targetDoneness = note.order.doneness;
            if (note.order.nuggets > 0) ctx.targetNuggets = note.order.nuggets;
        }

        steps = Build(meat);
        stepIndex = 0;
        steps[0].Init(ctx);
        lastMouse = GuiDraw.MousePos();
        phase = Phase.Steps;
        Active = true;
        PlayerMovement.inputLocked = true;
    }

    List<ChefStep> Build(MeatType meat)
    {
        switch (meat)
        {
            case MeatType.Beef:
                return new List<ChefStep> { new BeefSeasonStep(), new BeefPanStep() };
            case MeatType.Chicken:
                return new List<ChefStep> { new ChickenSauceStep(), new ChickenPaintStep(), new CookStep(CookStep.Kind.Chicken) };
            case MeatType.Fish:
                return new List<ChefStep>
                {
                    new FishTrimStep(), new FishScaleStep(), new FishFilletStep(),
                    new FishCutStep(), new FishBreadStep(), new CookStep(CookStep.Kind.Nugget)
                };
            default:
                return new List<ChefStep> { new MeatballMixStep(), new MeatballSculptStep(), new CookStep(CookStep.Kind.Ball) };
        }
    }

    void Close()
    {
        if (phase == Phase.Steps) steps[stepIndex].End();
        phase = Phase.Closed;
        Active = false;
        PlayerMovement.inputLocked = false;
    }

    ChefInput ReadInput(Mouse mouse)
    {
        Vector2 m = GuiDraw.MousePos();
        var i = new ChefInput
        {
            m = m,
            delta = m - lastMouse,
            pressed = mouse.leftButton.wasPressedThisFrame,
            down = mouse.leftButton.isPressed,
            released = mouse.leftButton.wasReleasedThisFrame,
            rPressed = mouse.rightButton.wasPressedThisFrame,
            rDown = mouse.rightButton.isPressed,
            dt = Mathf.Min(Time.deltaTime, 0.05f)
        };
        lastMouse = m;
        return i;
    }

    void Update()
    {
        if (phase == Phase.Closed) return;

        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            Sfx.Play(Sfx.UiBack);
            OrderBoard.Instance.Release(note);
            Close();
            return;
        }

        ChefInput i = ReadInput(Mouse.current);

        if (phase == Phase.Results)
        {
            if (i.pressed && ActionRect.Contains(i.m))
            {
                Sfx.Play(Sfx.UiSelect);
                Finish();
            }
            return;
        }

        ChefStep step = steps[stepIndex];
        if (i.pressed && step.actionLabel != null && step.ActionEnabled && ActionRect.Contains(i.m))
        {
            Sfx.Play(Sfx.UiSelect);
            step.OnAction();
        }
        else step.Tick(i);

        if (step.done) NextStep();
    }

    void NextStep()
    {
        steps[stepIndex].End();
        stepIndex++;
        if (stepIndex < steps.Count)
        {
            steps[stepIndex].Init(ctx);
            return;
        }

        lines.Clear();
        foreach (var s in steps) s.Scores(lines);

        float sum = 0f;
        float weights = 0f;
        foreach (var l in lines)
        {
            sum += l.value * l.weight;
            weights += l.weight;
        }
        total = sum / weights;
        phase = Phase.Results;
    }

    void Finish()
    {
        ChefHands.Instance.HoldCooked(ctx.meat, total, ctx.doneness, ctx.nuggets, note);
        ChefHands.Instance.Say("Dish ready (" + Mathf.RoundToInt(total * 100f) + "%)");
        Close();
    }

    string OrderText()
    {
        return note != null ? "For " + note.title + ": " + note.order.MeatText() : "Practice run";
    }

    void OnGUI()
    {
        if (phase == Phase.Closed || Event.current.type != EventType.Repaint) return;

        var i = new ChefInput { m = GuiDraw.MousePos() };

        GuiDraw.Fill(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);
        GuiDraw.Frame(ChefStep.N(0.03f, 0.04f, 0.94f, 0.92f));

        if (phase == Phase.Steps) DrawStep(i);
        else DrawResults(i);

        GuiDraw.Label(ChefStep.N(0.07f, 0.88f, 0.3f, 0.05f), "Esc to leave", ChefStep.FS(0.02f), Color.white, TextAnchor.MiddleLeft);
    }

    void DrawStep(ChefInput i)
    {
        ChefStep step = steps[stepIndex];
        GuiDraw.Label(ChefStep.N(0.07f, 0.07f, 0.5f, 0.08f), step.title, ChefStep.FS(0.05f), Color.white, TextAnchor.MiddleLeft);
        GuiDraw.Label(ChefStep.N(0.07f, 0.15f, 0.86f, 0.05f), step.hint, ChefStep.FS(0.022f), Color.white, TextAnchor.MiddleLeft);
        GuiDraw.Label(ChefStep.N(0.5f, 0.07f, 0.43f, 0.06f), OrderText(), ChefStep.FS(0.022f), Color.white, TextAnchor.MiddleRight);

        step.Draw(i);

        if (step.actionLabel != null)
            GuiDraw.Button(ActionRect, step.actionLabel, ActionRect.Contains(i.m), step.ActionEnabled, ChefStep.FS(0.03f));
    }

    void DrawResults(ChefInput i)
    {
        GuiDraw.Label(ChefStep.N(0.07f, 0.07f, 0.6f, 0.08f), "Dish finished", ChefStep.FS(0.05f), Color.white, TextAnchor.MiddleLeft);

        float y = 0.24f;
        foreach (var l in lines)
        {
            GuiDraw.Label(ChefStep.N(0.07f, y, 0.46f, 0.06f), l.label, ChefStep.FS(0.022f), Color.white, TextAnchor.MiddleLeft);
            Rect bar = ChefStep.N(0.55f, y + 0.015f, 0.25f, 0.03f);
            GuiDraw.Frame(bar);
            GuiDraw.Fill(new Rect(bar.x + 2f, bar.y + 2f, (bar.width - 4f) * l.value, bar.height - 4f), Color.white);
            GuiDraw.Label(ChefStep.N(0.82f, y, 0.1f, 0.06f), Mathf.RoundToInt(l.value * 100f) + "%", ChefStep.FS(0.022f), Color.white);
            y += 0.075f;
        }

        GuiDraw.Label(ChefStep.N(0.07f, 0.8f, 0.6f, 0.07f), "Dish quality: " + Mathf.RoundToInt(total * 100f) + "%",
            ChefStep.FS(0.04f), Color.white, TextAnchor.MiddleLeft);
        GuiDraw.Button(ActionRect, "Take the dish", ActionRect.Contains(i.m), true, ChefStep.FS(0.03f));
    }
}