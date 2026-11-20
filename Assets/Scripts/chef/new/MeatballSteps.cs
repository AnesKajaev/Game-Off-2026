using System.Collections.Generic;
using UnityEngine;

public class MeatballMixStep : ChefStep
{
    static readonly string[] Names = { "Garlic powder", "Olive oil", "Salt and pepper", "Egg" };
    static readonly string[] Arts = { "garlic", "oil", "spices", "egg" };

    static readonly Vector2[] Homes =
    {
        new Vector2(0.1f, 0.3f), new Vector2(0.1f, 0.45f), new Vector2(0.1f, 0.6f), new Vector2(0.1f, 0.75f)
    };

    readonly DragSet set = new DragSet();
    readonly Drag[] items = new Drag[4];
    readonly float[] hold = new float[3];
    readonly bool[] added = new bool[4];
    readonly List<Vector2> specks = new List<Vector2>();
    int eggState;
    float mixDeg;
    bool swirling;

    Vector2 BowlCenter => Px(new Vector2(0.56f, 0.54f));
    float BowlRadius => H(0.22f);

    float Mix => Mathf.Clamp01(mixDeg / 1080f);

    int AddedCount
    {
        get
        {
            int n = 0;
            foreach (var a in added) if (a) n++;
            return n;
        }
    }

    protected override void Begin()
    {
        title = "Mix the meat";
        hint = "Put in what you want (or not): hold garlic, oil and seasoning over the bowl. Strike the egg on the rim, right click to open it. Then hold and swirl to mix.";
        actionLabel = "Done";
        for (int k = 0; k < items.Length; k++)
            items[k] = set.Add(Names[k], Arts[k], Homes[k], 0.14f);
    }

    void Add(int k)
    {
        added[k] = true;
        mixDeg *= 0.4f;
        int n = k == 2 ? 24 : k == 3 ? 8 : 14;
        for (int s = 0; s < n; s++) specks.Add(Random.insideUnitCircle * 0.8f);
    }

    public override void Tick(ChefInput i)
    {
        set.Update(i);
        Drag h = set.held;
        Vector2 c = BowlCenter;
        float R = BowlRadius;
        int idx = h != null ? System.Array.IndexOf(items, h) : -1;

        if (h != null && i.down && idx >= 0)
        {
            float d = (i.m - c).magnitude;
            if (idx < 3)
            {
                if (!added[idx] && d < R)
                {
                    hold[idx] += i.dt;
                    if (hold[idx] >= 0.7f) Add(idx);
                }
            }
            else if (eggState < 2)
            {
                float pd = (i.m - i.delta - c).magnitude;
                float speed = i.dt > 0f ? i.delta.magnitude / Screen.height / i.dt : 0f;
                if (eggState == 0 && (pd - R) * (d - R) < 0f && speed > 0.5f)
                {
                    eggState = 1;
                    items[3].name = "Cracked egg";
                    items[3].shade = 0.3f;
                }
                if (eggState == 1 && i.rPressed && d < R * 0.95f)
                {
                    eggState = 2;
                    items[3].visible = false;
                    Add(3);
                }
            }
        }

        if (!swirling && i.pressed && h == null && (i.m - c).magnitude < R) swirling = true;
        if (swirling)
        {
            if (!i.down)
            {
                swirling = false;
            }
            else
            {
                float d = (i.m - c).magnitude;
                if (d > R * 0.15f && d < R * 1.05f)
                {
                    Vector2 pv = i.m - i.delta;
                    float a0 = Mathf.Atan2(pv.y - c.y, pv.x - c.x) * Mathf.Rad2Deg;
                    float a1 = Mathf.Atan2(i.m.y - c.y, i.m.x - c.x) * Mathf.Rad2Deg;
                    mixDeg += Mathf.Abs(Mathf.DeltaAngle(a0, a1));
                }
            }
        }
    }

    public override void Draw(ChefInput i)
    {
        Vector2 c = BowlCenter;
        float R = BowlRadius;
        Disc(c, R, "bowl", 0f);

        Texture2D meat = ctx.Tex("ground-meat");
        if (meat != null) GUI.DrawTexture(new Rect(c.x - R * 0.8f, c.y - R * 0.8f, R * 1.6f, R * 1.6f), meat, ScaleMode.ScaleToFit);
        else GuiDraw.Circle(c, R * 0.82f, Gray(Mathf.Lerp(0.3f, 0.55f, Mix * AddedCount / 4f)));

        float alpha = 1f - Mix * 0.85f;
        foreach (var s in specks)
            GuiDraw.Circle(c + s * R, Mathf.Max(2f, R * 0.04f), new Color(1f, 1f, 1f, alpha));

        if (swirling) GuiDraw.Circle(i.m, 6f, Color.white);
        DrawDrags(set);
    }

    public override void Scores(List<ScoreLine> list)
    {
        list.Add(new ScoreLine("Put all the ingredients in", AddedCount / 4f));
        list.Add(new ScoreLine("Mixed everything well", Mix));
    }
}

public class MeatballSculptStep : ChefStep
{
    enum Mode { Idle, Taking, Moving, Sculpting }

    const int Pts = 48;

    readonly float[] radius = new float[Pts];
    readonly float[] radiusPx = new float[Pts];
    readonly List<float> rounds = new List<float>();
    Mode mode;
    float remaining = 1f;
    float hold;
    float size;
    float moveT;
    Vector2 piecePos;
    Vector2 startPos;
    int balls;

    Vector2 BowlCenter => Px(new Vector2(0.2f, 0.52f));
    float BowlRadius => H(0.17f);
    Vector2 WorkCenter => Px(new Vector2(0.62f, 0.5f));

    public override bool ActionEnabled => mode == Mode.Sculpting;

    protected override void Begin()
    {
        title = "Make the balls";
        hint = "Hold on the meat to take a piece (the longer, the bigger). Click inside it to expand, click outside to squeeze. Aim for a perfect circle.";
        actionLabel = "Done ball";
    }

    float RadiusAt(float angleDeg)
    {
        float a = Mathf.Repeat(angleDeg, 360f) / 360f * Pts;
        int i0 = Mathf.FloorToInt(a) % Pts;
        int i1 = (i0 + 1) % Pts;
        return Mathf.Lerp(radius[i0], radius[i1], a - Mathf.Floor(a));
    }

    float MeanRadius()
    {
        float mean = 0f;
        foreach (var r in radius) mean += r;
        return mean / Pts;
    }

    float Roundness()
    {
        float mean = MeanRadius();
        float sq = 0f;
        foreach (var r in radius) sq += (r - mean) * (r - mean);
        float cv = Mathf.Sqrt(sq / Pts) / mean;
        return 1f - Mathf.Clamp01(cv / 0.2f);
    }

    void TakePiece(Vector2 mouse)
    {
        remaining = Mathf.Max(0f, remaining - (0.08f + 0.12f * size));
        float baseR = Mathf.Lerp(0.06f, 0.16f, size);
        float p1 = Random.value * 6.28f;
        float p2 = Random.value * 6.28f;
        float p3 = Random.value * 6.28f;
        for (int k = 0; k < Pts; k++)
        {
            float a = k * Mathf.PI * 2f / Pts;
            radius[k] = baseR * (1f + 0.22f * Mathf.Sin(2f * a + p1) + 0.16f * Mathf.Sin(3f * a + p2) + 0.1f * Mathf.Sin(5f * a + p3));
        }
        startPos = mouse;
        moveT = 0f;
        mode = Mode.Moving;
    }

    void Sculpt(ChefInput i)
    {
        if (!i.down) return;

        Vector2 rel = (i.m - piecePos) / Screen.height;
        float dist = rel.magnitude;
        float ang = Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg;
        float contour = RadiusAt(ang);
        bool inside = dist < contour;
        float falloff = inside ? 1f : Mathf.Clamp01(1f - (dist - contour) / 0.15f);
        if (falloff <= 0f) return;

        for (int k = 0; k < Pts; k++)
        {
            float a = k * 360f / Pts;
            float w = Mathf.Exp(-Mathf.Pow(Mathf.DeltaAngle(a, ang) / 22f, 2f)) * falloff;
            radius[k] = Mathf.Clamp(radius[k] + (inside ? 1f : -1f) * 0.22f * i.dt * w, 0.03f, 0.3f);
        }
    }

    public override void Tick(ChefInput i)
    {
        switch (mode)
        {
            case Mode.Idle:
                if (i.pressed && remaining > 0.02f && (i.m - BowlCenter).magnitude < BowlRadius)
                {
                    mode = Mode.Taking;
                    hold = 0f;
                    size = 0f;
                }
                break;

            case Mode.Taking:
                hold += i.dt;
                size = Mathf.Clamp01(hold / 2f);
                if (!i.down) TakePiece(i.m);
                break;

            case Mode.Moving:
                moveT += i.dt / 0.35f;
                piecePos = Vector2.Lerp(startPos, WorkCenter, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(moveT)));
                if (moveT >= 1f) mode = Mode.Sculpting;
                break;

            case Mode.Sculpting:
                Sculpt(i);
                break;
        }
    }

    public override void Draw(ChefInput i)
    {
        Vector2 bc = BowlCenter;
        float br = BowlRadius;
        Disc(bc, br, "bowl", 0f);
        if (remaining > 0.02f) GuiDraw.Circle(bc, br * 0.8f * Mathf.Sqrt(remaining), Gray(0.55f));

        for (int k = 0; k < rounds.Count; k++)
            GuiDraw.Circle(Px(new Vector2(0.5f + k * 0.05f, 0.24f)), H(0.022f), Color.white);
        GuiDraw.Label(N(0.5f, 0.27f, 0.3f, 0.05f), "Balls: " + balls + " / " + ctx.ballsToMake, FS(0.024f), Color.white, TextAnchor.MiddleLeft);

        if (mode == Mode.Taking) GuiDraw.Circle(i.m, H(Mathf.Lerp(0.03f, 0.09f, size)), Gray(0.55f));

        if (mode != Mode.Moving && mode != Mode.Sculpting) return;

        for (int k = 0; k < Pts; k++) radiusPx[k] = radius[k] * Screen.height;
        GuiDraw.FillPolar(piecePos, radiusPx, Gray(0.55f));

        if (mode != Mode.Sculpting) return;

        float mr = MeanRadius() * Screen.height;
        for (int k = 0; k < 40; k++)
        {
            float a0 = k * Mathf.PI * 2f / 40f;
            float a1 = (k + 1) * Mathf.PI * 2f / 40f;
            GuiDraw.Line(piecePos + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * mr, piecePos + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * mr, 2f, Color.white);
        }
    }

    public override void OnAction()
    {
        rounds.Add(Roundness());
        balls++;
        mode = Mode.Idle;
        if (balls >= ctx.ballsToMake || remaining <= 0.02f)
        {
            ctx.balls = balls;
            done = true;
        }
    }

    public override void Scores(List<ScoreLine> list)
    {
        float s = 0f;
        foreach (var r in rounds) s += r;
        list.Add(new ScoreLine("Formed perfect circles", s / rounds.Count));
    }
}