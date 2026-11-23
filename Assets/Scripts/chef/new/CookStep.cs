using System.Collections.Generic;
using UnityEngine;

public class CookStep : ChefStep
{
    public enum Kind { Nugget, Chicken, Ball }

    class Piece
    {
        public Vector2 pos;
        public Vector2 home;
        public float[] side;
        public int down;
        public float angle;
        public bool onSurface;
        public bool plated;
        public float score;
    }

    readonly Kind kind;
    readonly List<Piece> pieces = new List<Piece>();
    Piece held;
    Piece pending;
    Vector2 grab;
    Vector2 pressPos;
    float sideTime;
    int platedCount;

    public CookStep(Kind k)
    {
        kind = k;
    }

    bool Spin => kind == Kind.Ball;
    int Sides => Spin ? 4 : 2;
    float PieceH => kind == Kind.Chicken ? 0.14f : kind == Kind.Nugget ? 0.085f : 0.1f;
    float PieceAspect => kind == Kind.Chicken ? 1.7f : 1f;

    Rect SurfaceRect => kind == Kind.Chicken ? N(0.42f, 0.24f, 0.5f, 0.5f) : N(0.46f, 0.24f, 0.46f, 0.5f);
    Rect PlateRect => N(0.07f, 0.6f, 0.3f, 0.26f);

    public override bool ActionEnabled
    {
        get
        {
            foreach (var p in pieces) if (!p.onSurface) return false;
            return true;
        }
    }

    protected override void Begin()
    {
        int count;
        if (kind == Kind.Chicken) count = 2;
        else if (kind == Kind.Nugget) count = Mathf.Clamp(ctx.nuggets > 0 ? ctx.nuggets : ctx.targetNuggets, 1, 16);
        else count = Mathf.Max(1, ctx.balls);

        sideTime = ctx.sideSeconds * (Spin ? 0.6f : 1f);

        for (int k = 0; k < count; k++)
        {
            Vector2 home = kind == Kind.Chicken
                ? new Vector2(0.17f, 0.3f + k * 0.15f)
                : new Vector2(0.09f + (k % 4) * 0.065f, 0.3f + (k / 4) * 0.075f);
            pieces.Add(new Piece { pos = home, home = home, side = new float[Sides] });
        }

        if (kind == Kind.Nugget)
        {
            title = "Fry the nuggets";
            hint = "Drag the nuggets into the pan. The border shows the side that is cooking. Click to flip, then drag them to the plate.";
        }
        else if (kind == Kind.Chicken)
        {
            title = "Grill the chicken";
            hint = "Put the breasts on the grill and click them to turn them at the right time. Then drag them to the plate.";
        }
        else
        {
            title = "Cook the meatballs";
            hint = "Drop the balls in the sauce. Click and drag on a ball to spin it so it cooks all around. Serve when they're ready.";
            actionLabel = "Serve";
        }
    }

    Rect PieceRect(Piece p)
    {
        float hh = PieceH * Screen.height;
        float ww = hh * PieceAspect;
        Vector2 c = Px(p.pos);
        return new Rect(c.x - ww / 2f, c.y - hh / 2f, ww, hh);
    }

    int DownSide(Piece p)
    {
        if (!Spin) return p.down;
        return Mathf.FloorToInt(Mathf.Repeat(p.angle, 360f) / 90f) % 4;
    }

    bool OnSurface(Vector2 c)
    {
        Rect sr = SurfaceRect;
        if (kind == Kind.Chicken) return sr.Contains(c);
        return (c - sr.center).magnitude < Mathf.Min(sr.width, sr.height) * 0.5f;
    }

    static float SideScore(float p)
    {
        if (p < 0.9f) return p / 0.9f;
        if (p <= 1.3f) return 1f;
        return Mathf.Clamp01(1f - (p - 1.3f) / 0.4f);
    }

    float PieceScore(Piece p)
    {
        float s = 0f;
        foreach (var v in p.side) s += SideScore(v);
        return s / p.side.Length;
    }

    public override void Tick(ChefInput i)
    {
        foreach (var p in pieces)
            if (p.onSurface && !p.plated && p != held) p.side[DownSide(p)] += i.dt / sideTime;

        if (i.pressed && held == null && pending == null)
        {
            for (int k = pieces.Count - 1; k >= 0; k--)
            {
                Piece p = pieces[k];
                if (p.plated || !PieceRect(p).Contains(i.m)) continue;
                if (p.onSurface)
                {
                    pending = p;
                    pressPos = i.m;
                }
                else
                {
                    held = p;
                    grab = Px(p.pos) - i.m;
                }
                break;
            }
        }

        if (pending != null)
        {
            if (!i.down)
            {
                if (!Spin) pending.down = 1 - pending.down;
                pending = null;
            }
            else if (Spin)
            {
                pending.angle += i.delta.x * 0.9f;
            }
            else if ((i.m - pressPos).magnitude > 10f)
            {
                held = pending;
                grab = Px(held.pos) - i.m;
                pending = null;
            }
        }

        if (held != null)
        {
            if (i.down) held.pos = Nrm(i.m + grab);
            else Drop(held);
        }

        if (!Spin && platedCount == pieces.Count) done = true;
    }

    void Drop(Piece p)
    {
        Vector2 c = Px(p.pos);
        held = null;

        if (!Spin && p.onSurface && PlateRect.Contains(c))
        {
            Plate(p);
            return;
        }

        if (OnSurface(c))
        {
            p.onSurface = true;
            Rect sr = SurfaceRect;
            if (kind == Kind.Chicken)
            {
                c = new Vector2(Mathf.Clamp(c.x, sr.x + sr.width * 0.15f, sr.xMax - sr.width * 0.15f), Mathf.Clamp(c.y, sr.y + sr.height * 0.18f, sr.yMax - sr.height * 0.18f));
            }
            else
            {
                float maxR = Mathf.Min(sr.width, sr.height) * 0.38f;
                if ((c - sr.center).magnitude > maxR) c = sr.center + (c - sr.center).normalized * maxR;
            }
            p.pos = Nrm(c);
        }
        else
        {
            p.onSurface = false;
            p.pos = p.home;
        }
    }

    void Plate(Piece p)
    {
        p.score = PieceScore(p);
        p.plated = true;
        p.onSurface = false;
        Rect pr = PlateRect;
        int k = platedCount++;
        p.pos = Nrm(new Vector2(pr.x + pr.width * (0.2f + 0.2f * (k % 4)), pr.y + pr.height * (0.3f + 0.22f * (k / 4))));
    }

    public override void OnAction()
    {
        foreach (var p in pieces) p.score = PieceScore(p);
        done = true;
    }

    static float Shade(float p)
    {
        if (p <= 1f) return Mathf.Lerp(0.95f, 0.55f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 1f, p)));
        return Mathf.Lerp(0.55f, 0.1f, Mathf.InverseLerp(1.15f, 1.7f, p));
    }

    float Average(Piece p)
    {
        float s = 0f;
        foreach (var v in p.side) s += v;
        return s / p.side.Length;
    }

    void DrawPiece(Piece p)
    {
        Rect r = PieceRect(p);

        if (Spin)
        {
            Vector2 c = r.center;
            float rad = r.height * 0.5f;
            GuiDraw.Circle(c, rad, Color.white);
            GuiDraw.Circle(c, rad - 2f, Gray(Shade(Average(p))));
            if (p.plated) return;
            for (int k = 0; k < 4; k++)
            {
                float phi = (90f - (k * 90f + 45f - p.angle)) * Mathf.Deg2Rad;
                Vector2 dot = c + new Vector2(Mathf.Cos(phi), Mathf.Sin(phi)) * rad;
                float dr = rad * (k == DownSide(p) ? 0.2f : 0.13f);
                GuiDraw.Circle(dot, dr, Color.white);
                GuiDraw.Circle(dot, dr - 2f, Gray(Shade(p.side[k])));
            }
            return;
        }

        if (p.plated)
        {
            GuiDraw.Frame(r, Shade(Average(p)));
            return;
        }

        float b = Mathf.Max(3f, r.height * 0.1f);
        GuiDraw.Fill(r, Gray(Mathf.Max(0.25f, Shade(p.side[p.down]))));
        GuiDraw.Fill(new Rect(r.x + b, r.y + b, r.width - 2f * b, r.height - 2f * b), Gray(Shade(p.side[1 - p.down])));
    }

    public override void Draw(ChefInput i)
    {
        Rect sr = SurfaceRect;
        Texture2D st = ctx.Tex(kind == Kind.Chicken ? "grill" : "pan");
        if (st != null)
        {
            GUI.DrawTexture(sr, st, ScaleMode.ScaleToFit);
        }
        else if (kind == Kind.Chicken)
        {
            GuiDraw.Frame(sr);
            for (int k = 1; k < 10; k++)
                GuiDraw.Fill(new Rect(sr.x + 8f, sr.y + sr.height * k / 10f, sr.width - 16f, 2f), Gray(0.55f));
        }
        else
        {
            Vector2 c = sr.center;
            float r = Mathf.Min(sr.width, sr.height) * 0.5f;
            GuiDraw.Fill(new Rect(c.x + r * 0.85f, c.y - H(0.012f), r * 0.6f, H(0.024f)), Color.white);
            GuiDraw.Circle(c, r, Color.white);
            GuiDraw.Circle(c, r - 3f, Gray(kind == Kind.Ball ? 0.3f : 0.08f));
        }

        if (!Spin) Disc(PlateRect.center, PlateRect.height * 0.5f, "plate", 0f);

        foreach (var p in pieces) if (p.plated) DrawPiece(p);
        foreach (var p in pieces) if (!p.plated && p != held) DrawPiece(p);
        if (held != null) DrawPiece(held);
    }

    public override void Scores(List<ScoreLine> list)
    {
        float s = 0f;
        foreach (var p in pieces) s += p.score;

        string label = kind == Kind.Chicken ? "Grilled the right amount of time"
                     : kind == Kind.Nugget ? "Cooked the nuggets (not burnt, not raw)"
                     : "Cooked the meatballs (not burnt, not raw)";
        list.Add(new ScoreLine(label, s / pieces.Count));
    }
}