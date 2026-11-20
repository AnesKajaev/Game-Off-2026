using System.Collections.Generic;
using UnityEngine;

public class FishTrimStep : ChefStep
{
    class Part
    {
        public Rect area;
        public float shade;
        public string art;
        public Vector2 offset;
        public Vector2 vel;
        public float rot;
        public float rotVel;
        public bool falling;
    }

    const float HeadGuide = 0.31f;
    const float TailGuide = 0.67f;

    readonly Part head = new Part { area = new Rect(0.16f, 0.4f, 0.15f, 0.2f), shade = 0.3f, art = "fish-head" };
    readonly Part body = new Part { area = new Rect(0.31f, 0.4f, 0.36f, 0.2f), shade = 0.5f, art = "fish-body" };
    readonly Part tail = new Part { area = new Rect(0.67f, 0.43f, 0.13f, 0.14f), shade = 0.3f, art = "fish-tail" };
    float endTimer;

    protected override void Begin()
    {
        title = "Cut off the head and the tail";
        hint = "Hold the mouse and drag horizontally across each line to cut.";
    }

    void TryCut(Part p, float guideX, float prevX, float curX, float dir)
    {
        float gx = guideX * Screen.width;
        if ((prevX - gx) * (curX - gx) > 0f || prevX == curX) return;
        p.falling = true;
        p.vel = new Vector2(0.25f * dir, -0.35f);
        p.rotVel = 320f * dir;
    }

    public override void Tick(ChefInput i)
    {
        float top = 0.34f * Screen.height;
        float bottom = 0.66f * Screen.height;
        if (i.down && i.m.y > top && i.m.y < bottom && Mathf.Abs(i.delta.x) >= Mathf.Abs(i.delta.y) * 0.6f)
        {
            float prevX = i.m.x - i.delta.x;
            if (!head.falling) TryCut(head, HeadGuide, prevX, i.m.x, -1f);
            if (!tail.falling) TryCut(tail, TailGuide, prevX, i.m.x, 1f);
        }

        foreach (var p in new[] { head, tail })
        {
            if (!p.falling) continue;
            p.vel.y += 2.4f * i.dt;
            p.offset += p.vel * i.dt;
            p.rot += p.rotVel * i.dt;
        }

        if (head.falling && tail.falling)
        {
            endTimer += i.dt;
            if (endTimer > 1.1f) done = true;
        }
    }

    void DrawPart(Part p)
    {
        Rect r = N(p.area.x + p.offset.x, p.area.y + p.offset.y, p.area.width, p.area.height);
        if (p.falling) GuiDraw.FrameRot(r, p.rot, p.shade);
        else Box(r, p.art, "", -1, p.shade);
    }

    void DrawGuide(float x)
    {
        float gx = x * Screen.width;
        for (int k = 0; k < 8; k++)
        {
            float y0 = Screen.height * (0.34f + k * 0.04f);
            GuiDraw.Line(new Vector2(gx, y0), new Vector2(gx, y0 + Screen.height * 0.022f), 3f, Color.white);
        }
    }

    public override void Draw(ChefInput i)
    {
        DrawPart(body);
        DrawPart(head);
        DrawPart(tail);
        if (!head.falling) DrawGuide(HeadGuide);
        if (!tail.falling) DrawGuide(TailGuide);
    }

    public override void Scores(List<ScoreLine> list) { }
}

public class FishScaleStep : ChefStep
{
    struct Fall
    {
        public Vector2 p;
        public Vector2 v;
        public float rot;
        public float rv;
        public float life;
    }

    const int Cols = 30;
    const int Rows = 8;

    readonly bool[] present = new bool[Cols * Rows];
    readonly List<Fall> falls = new List<Fall>();
    int remaining;

    Rect BodyRect => N(0.2f, 0.3f, 0.6f, 0.3f);

    protected override void Begin()
    {
        title = "Remove the scales";
        hint = "Hold the mouse and move the cleaver across the body. Go slowly, or you will miss some scales.";
        actionLabel = "Done";
        for (int k = 0; k < present.Length; k++) present[k] = true;
        remaining = present.Length;
    }

    public override void Tick(ChefInput i)
    {
        Rect b = BodyRect;
        float cw = b.width / Cols;
        float ch = b.height / Rows;

        if (i.down)
        {
            float speed = i.dt > 0f ? Mathf.Abs(i.delta.x) / Screen.width / i.dt : 0f;
            float chance = Mathf.Clamp01(1.5f - speed);
            float bladeW = H(0.03f);
            float bladeHalf = H(0.075f);
            float prevX = i.m.x - i.delta.x;
            float x0 = Mathf.Min(i.m.x, prevX) - bladeW / 2f;
            float x1 = Mathf.Max(i.m.x, prevX) + bladeW / 2f;
            float y0 = i.m.y - bladeHalf;
            float y1 = i.m.y + bladeHalf;

            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                {
                    int idx = y * Cols + x;
                    if (!present[idx]) continue;
                    Vector2 c = new Vector2(b.x + (x + 0.5f) * cw, b.y + (y + 0.5f) * ch);
                    if (c.x < x0 || c.x > x1 || c.y < y0 || c.y > y1) continue;
                    if (Random.value >= chance) continue;

                    present[idx] = false;
                    remaining--;
                    if (falls.Count < 400)
                        falls.Add(new Fall
                        {
                            p = c,
                            v = new Vector2(Random.Range(-60f, 60f), Random.Range(-220f, -60f)),
                            rot = Random.Range(0f, 360f),
                            rv = Random.Range(-400f, 400f),
                            life = 1.6f
                        });
                }
        }

        for (int k = falls.Count - 1; k >= 0; k--)
        {
            Fall f = falls[k];
            f.v.y += 2.5f * Screen.height * i.dt;
            f.p += f.v * i.dt;
            f.rot += f.rv * i.dt;
            f.life -= i.dt;
            if (f.life <= 0f) falls.RemoveAt(k);
            else falls[k] = f;
        }
    }

    public override void Draw(ChefInput i)
    {
        Rect b = BodyRect;
        GuiDraw.Frame(b, 0.15f);
        float cw = b.width / Cols;
        float ch = b.height / Rows;
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
                if (present[y * Cols + x])
                    GuiDraw.Fill(new Rect(b.x + x * cw + 1f, b.y + y * ch + 1f, cw - 2f, ch - 2f), Gray(0.8f));

        foreach (var f in falls)
            GuiDraw.FillRot(new Rect(f.p.x - cw / 2f, f.p.y - ch / 2f, cw - 2f, ch - 2f), f.rot, new Color(0.8f, 0.8f, 0.8f, Mathf.Clamp01(f.life)));

        float bw = H(0.03f);
        float bh = H(0.15f);
        var r = new Rect(i.m.x - bw / 2f, i.m.y - bh / 2f, bw, bh);
        Texture2D t = ctx.Tex("cleaver");
        if (t != null)
        {
            GUI.DrawTexture(new Rect(i.m.x - bh / 2f, r.y, bh, bh), t, ScaleMode.ScaleToFit);
        }
        else
        {
            GuiDraw.Fill(r, Color.white);
            GuiDraw.Fill(new Rect(r.x, r.yMax, r.width, H(0.06f)), Gray(0.5f));
        }
    }

    public override void Scores(List<ScoreLine> list)
    {
        list.Add(new ScoreLine("Scales removed", 1f - remaining / (float)present.Length));
    }
}

public class FishFilletStep : ChefStep
{
    const int Pts = 72;

    readonly Vector2[] outline = new Vector2[Pts];
    readonly Vector2[] pixels = new Vector2[Pts];
    readonly bool[] cut = new bool[Pts];
    float devSum;
    int devSamples;

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    float Tol => H(0.03f);

    float Coverage()
    {
        int n = 0;
        foreach (var c in cut) if (c) n++;
        return n / (float)Pts;
    }

    public override bool ActionEnabled => Coverage() >= 0.85f;

    protected override void Begin()
    {
        title = "Cut out the fillet";
        hint = "Hold the mouse and follow the dashed line to cut the skin. You can let go and continue.";
        actionLabel = "Done";

        float p1 = Random.value * 6.28f;
        float p2 = Random.value * 6.28f;
        for (int k = 0; k < Pts; k++)
        {
            float t = k * Mathf.PI * 2f / Pts;
            float rx = 0.27f * (1f + 0.07f * Mathf.Sin(3f * t + p1));
            float ry = 0.17f * (1f + 0.09f * Mathf.Sin(2f * t + p2));
            outline[k] = Center + new Vector2(Mathf.Cos(t) * rx, Mathf.Sin(t) * ry);
        }
    }

    public override void Tick(ChefInput i)
    {
        if (!i.down) return;

        float tol = Tol;
        float best = float.MaxValue;
        for (int k = 0; k < Pts; k++)
        {
            float d = (Px(outline[k]) - i.m).magnitude;
            if (d < best) best = d;
            if (d < tol * 1.3f) cut[k] = true;
        }

        if (best < tol * 6f)
        {
            devSum += Mathf.Min(best, tol * 4f);
            devSamples++;
        }

        if (Coverage() >= 0.97f) done = true;
    }

    public override void Draw(ChefInput i)
    {
        Vector2 c = Px(Center);
        for (int k = 0; k < Pts; k++) pixels[k] = Px(outline[k]);
        GuiDraw.FillFan(c, pixels, Gray(0.25f));

        for (int k = 0; k < Pts; k++)
        {
            Vector2 a = pixels[k];
            Vector2 b = pixels[(k + 1) % Pts];
            if (cut[k]) GuiDraw.Line(a, b, 6f, Color.white);
            else if (k % 2 == 0) GuiDraw.Line(a, b, 2f, Gray(0.7f));
        }

        GuiDraw.Circle(i.m, 6f, Color.white);
    }

    public override void Scores(List<ScoreLine> list)
    {
        float precision = devSamples > 0 ? 1f - Mathf.Clamp01(devSum / devSamples / (Tol * 3f)) : 1f;
        list.Add(new ScoreLine("Cut the skin along the line", Coverage() * precision));
    }
}

public class FishCutStep : ChefStep
{
    readonly List<float> vCuts = new List<float>();
    readonly List<float> hCuts = new List<float>();
    bool drawing;
    Vector2 start;

    Rect FilletRect => N(0.25f, 0.32f, 0.5f, 0.3f);
    int Pieces => (vCuts.Count + 1) * (hCuts.Count + 1);

    protected override void Begin()
    {
        title = "Cut the nuggets";
        hint = "Draw straight lines from one side of the fillet to the other. The order wants " + ctx.targetNuggets + " nuggets.";
        actionLabel = "Done";
    }

    static bool Near(List<float> list, float v)
    {
        foreach (var x in list) if (Mathf.Abs(x - v) < 0.04f) return true;
        return false;
    }

    void Commit(Vector2 a, Vector2 b)
    {
        Rect r = FilletRect;
        float dx = Mathf.Abs(b.x - a.x);
        float dy = Mathf.Abs(b.y - a.y);

        if (dx >= dy)
        {
            float y = (a.y + b.y) / 2f;
            bool spans = Mathf.Min(a.x, b.x) <= r.x + r.width * 0.1f && Mathf.Max(a.x, b.x) >= r.xMax - r.width * 0.1f;
            float t = (y - r.y) / r.height;
            if (spans && t > 0.03f && t < 0.97f && !Near(hCuts, t) && hCuts.Count < 7) hCuts.Add(t);
        }
        else
        {
            float x = (a.x + b.x) / 2f;
            bool spans = Mathf.Min(a.y, b.y) <= r.y + r.height * 0.1f && Mathf.Max(a.y, b.y) >= r.yMax - r.height * 0.1f;
            float t = (x - r.x) / r.width;
            if (spans && t > 0.03f && t < 0.97f && !Near(vCuts, t) && vCuts.Count < 7) vCuts.Add(t);
        }
    }

    public override void Tick(ChefInput i)
    {
        Rect r = FilletRect;
        var area = new Rect(r.x - H(0.1f), r.y - H(0.1f), r.width + H(0.2f), r.height + H(0.2f));
        if (i.pressed && area.Contains(i.m))
        {
            drawing = true;
            start = i.m;
        }
        if (drawing && !i.down)
        {
            drawing = false;
            Commit(start, i.m);
        }
    }

    public override void Draw(ChefInput i)
    {
        Rect r = FilletRect;
        var xs = new List<float> { 0f };
        var ys = new List<float> { 0f };
        var sx = new List<float>(vCuts);
        var sy = new List<float>(hCuts);
        sx.Sort();
        sy.Sort();
        xs.AddRange(sx);
        ys.AddRange(sy);
        xs.Add(1f);
        ys.Add(1f);

        for (int a = 0; a < xs.Count - 1; a++)
            for (int b = 0; b < ys.Count - 1; b++)
            {
                var piece = new Rect(r.x + xs[a] * r.width + 2f, r.y + ys[b] * r.height + 2f, (xs[a + 1] - xs[a]) * r.width - 4f, (ys[b + 1] - ys[b]) * r.height - 4f);
                GuiDraw.Frame(piece, 0.25f);
            }

        if (drawing) GuiDraw.Line(start, i.m, 3f, Color.white);

        GuiDraw.Label(N(0.25f, 0.68f, 0.5f, 0.07f), "Pieces: " + Pieces + "   (order: " + ctx.targetNuggets + ")", FS(0.034f), Color.white);
    }

    public override void OnAction()
    {
        ctx.nuggets = Mathf.Min(Pieces, 64);
        done = true;
    }

    public override void Scores(List<ScoreLine> list)
    {
        int target = Mathf.Max(1, ctx.targetNuggets);
        list.Add(new ScoreLine("Amount of nuggets (wanted " + target + ", cut " + Pieces + ")", 1f - Mathf.Abs(Pieces - target) / (float)target, 2f));
    }
}

public class FishBreadStep : ChefStep
{
    readonly DragSet set = new DragSet();
    readonly List<float> coat = new List<float>();
    int plated;

    Vector2 BowlCenter => Px(new Vector2(0.5f, 0.54f));
    float BowlRadius => H(0.18f);
    Rect PlateRect => N(0.72f, 0.38f, 0.22f, 0.3f);

    protected override void Begin()
    {
        title = "Bread the nuggets";
        hint = "Drag each piece into the bowl and shake it (a swirl works best), then drag it to the plate.";
        int n = Mathf.Clamp(ctx.nuggets > 0 ? ctx.nuggets : ctx.targetNuggets, 1, 16);
        for (int k = 0; k < n; k++)
        {
            Vector2 home = new Vector2(0.08f + (k % 4) * 0.065f, 0.3f + (k / 4) * 0.08f);
            set.Add("", "nugget", home, 0.06f, 1f, 0.95f);
            coat.Add(0f);
        }
    }

    public override void Tick(ChefInput i)
    {
        set.Update(i);
        Drag h = set.held;
        Vector2 c = BowlCenter;

        if (h != null && i.down && (i.m - c).magnitude < BowlRadius)
        {
            int idx = set.items.IndexOf(h);
            Vector2 pv = i.m - i.delta;
            float a0 = Mathf.Atan2(pv.y - c.y, pv.x - c.x) * Mathf.Rad2Deg;
            float a1 = Mathf.Atan2(i.m.y - c.y, i.m.x - c.x) * Mathf.Rad2Deg;
            float da = Mathf.Abs(Mathf.DeltaAngle(a0, a1));
            coat[idx] = Mathf.Min(1f, coat[idx] + da / 360f * 0.55f + i.delta.magnitude / Screen.height * 0.25f);
        }

        Drag rel = set.released;
        if (rel != null && PlateRect.Contains(rel.Center))
        {
            Rect pr = PlateRect;
            rel.stay = true;
            rel.locked = true;
            rel.pos = Nrm(new Vector2(pr.x + pr.width * (0.18f + 0.21f * (plated % 4)), pr.y + pr.height * (0.3f + 0.25f * (plated / 4))));
            plated++;
        }

        for (int k = 0; k < set.items.Count; k++)
            set.items[k].shade = Mathf.Lerp(0.95f, 0.55f, coat[k]);

        if (plated >= set.items.Count) done = true;
    }

    public override void Draw(ChefInput i)
    {
        Vector2 c = BowlCenter;
        float r = BowlRadius;
        Disc(c, r, "bowl", 0.3f);

        Vector2 prev = c;
        for (int s = 1; s <= 40; s++)
        {
            float t = s / 40f;
            float ang = t * 4f * Mathf.PI;
            Vector2 p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r * 0.75f * t;
            GuiDraw.Line(prev, p, 2f, Gray(0.6f));
            prev = p;
        }

        Disc(PlateRect.center, PlateRect.height * 0.5f, "plate", 0f);
        DrawDrags(set);
    }

    public override void Scores(List<ScoreLine> list)
    {
        float s = 0f;
        foreach (var v in coat) s += v;
        list.Add(new ScoreLine("Breaded the nuggets well", s / coat.Count));
    }
}