using System.Collections.Generic;
using UnityEngine;

public class ChickenSauceStep : ChefStep
{
    static readonly string[] Names = { "Garlic powder", "Vinegar", "Olive oil", "Oregano and pepper", "Lemon" };
    static readonly string[] Arts = { "garlic", "vinegar", "oil", "spices", "lemon" };

    static readonly Vector2[] Homes =
    {
        new Vector2(0.1f, 0.34f), new Vector2(0.22f, 0.34f), new Vector2(0.34f, 0.34f),
        new Vector2(0.16f, 0.62f), new Vector2(0.28f, 0.62f)
    };

    static readonly float[] Recipe = { 0.5f, 0.35f, 0.7f, 0.4f, 0.55f };

    readonly DragSet set = new DragSet();
    readonly Drag[] items = new Drag[5];
    readonly float[] amount = new float[5];

    Vector2 BowlCenter => Px(new Vector2(0.72f, 0.54f));
    float BowlRadius => H(0.18f);

    float Total
    {
        get
        {
            float s = 0f;
            foreach (var a in amount) s += a;
            return s;
        }
    }

    public override bool ActionEnabled => Total >= 0.2f;

    protected override void Begin()
    {
        title = "Prepare the sauce";
        hint = "Hold each ingredient over the bowl to add it. The right amounts are a secret.";
        actionLabel = "Done";
        for (int k = 0; k < items.Length; k++)
            items[k] = set.Add(Names[k], Arts[k], Homes[k], 0.15f);
    }

    public override void Tick(ChefInput i)
    {
        set.Update(i);
        Drag h = set.held;
        if (h != null && i.down && (h.Center - BowlCenter).magnitude < BowlRadius)
        {
            int k = System.Array.IndexOf(items, h);
            amount[k] = Mathf.Min(1f, amount[k] + 0.3f * i.dt);
        }
    }

    public override void Draw(ChefInput i)
    {
        Vector2 bc = BowlCenter;
        float br = BowlRadius;
        Disc(bc, br, "bowl", 0f);
        if (Total > 0f) GuiDraw.Circle(bc, br * 0.78f * Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(Total / 2.5f)), Gray(0.6f));

        for (int k = 0; k < items.Length; k++)
        {
            Rect r = items[k].Area;
            GuiDraw.Frame(new Rect(r.x, r.yMax + 4f, r.width, 8f));
            GuiDraw.Fill(new Rect(r.x + 2f, r.yMax + 6f, (r.width - 4f) * Mathf.Clamp01(amount[k]), 4f), Color.white);
        }
        DrawDrags(set);
    }

    public override void Scores(List<ScoreLine> list)
    {
        float sum = Total;
        float rsum = 0f;
        foreach (var r in Recipe) rsum += r;

        float score = 0f;
        if (sum >= 0.2f)
        {
            float tv = 0f;
            for (int k = 0; k < Recipe.Length; k++) tv += Mathf.Abs(amount[k] / sum - Recipe[k] / rsum);
            score = Mathf.Pow(1f - tv / 2f, 2f);
        }
        list.Add(new ScoreLine("The right balance of ingredients in the sauce", score));
    }
}

public class ChickenPaintStep : ChefStep
{
    const int Cols = 24;
    const int Rows = 12;

    static readonly Vector2 Home = new Vector2(0.2f, 0.58f);
    static readonly Vector2 E1C = new Vector2(0.27f, 0.5f);
    static readonly Vector2 E2C = new Vector2(0.73f, 0.5f);
    static readonly Vector2 ER = new Vector2(0.25f, 0.44f);

    readonly float[] coat = new float[Cols * Rows];
    readonly bool[] valid = new bool[Cols * Rows];
    Vector2 brushPos = Home;
    Vector2 grab;
    bool holding;
    float wet;
    int validCount;

    Rect ChickenRect => N(0.42f, 0.26f, 0.5f, 0.36f);
    Vector2 BowlCenter => Px(new Vector2(0.2f, 0.78f));
    float BowlRadius => H(0.085f);
    float BrushW => H(0.05f);
    float BrushH => H(0.2f);

    static bool InEllipse(Vector2 p, Vector2 c, Vector2 r)
    {
        float dx = (p.x - c.x) / r.x;
        float dy = (p.y - c.y) / r.y;
        return dx * dx + dy * dy <= 1f;
    }

    protected override void Begin()
    {
        title = "Paint the chicken";
        hint = "Dip the brush in the bowl and run it over the chicken. The sauce is almost invisible, so cover everything and don't let the brush dry.";
        actionLabel = "Done";

        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
            {
                Vector2 u = new Vector2((x + 0.5f) / Cols, (y + 0.5f) / Rows);
                bool ok = InEllipse(u, E1C, ER) || InEllipse(u, E2C, ER);
                valid[y * Cols + x] = ok;
                if (ok) validCount++;
            }
    }

    void PaintAt(Vector2 tip)
    {
        Rect cr = ChickenRect;
        float cw = cr.width / Cols;
        float ch = cr.height / Rows;
        float radius = H(0.04f);
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
            {
                int idx = y * Cols + x;
                if (!valid[idx]) continue;
                Vector2 c = new Vector2(cr.x + (x + 0.5f) * cw, cr.y + (y + 0.5f) * ch);
                if ((c - tip).magnitude < radius) coat[idx] = Mathf.Max(coat[idx], wet);
            }
    }

    public override void Tick(ChefInput i)
    {
        Vector2 center = Px(brushPos);
        var brushRect = new Rect(center.x - BrushW / 2f, center.y - BrushH / 2f, BrushW, BrushH);

        if (i.pressed && !holding && brushRect.Contains(i.m))
        {
            holding = true;
            grab = center - i.m;
        }

        if (holding)
        {
            if (i.down) brushPos = Nrm(i.m + grab);
            else holding = false;
        }
        else
        {
            brushPos = Vector2.Lerp(brushPos, Home, 1f - Mathf.Exp(-10f * i.dt));
        }

        if (!holding || !i.down) return;

        Vector2 tip = Px(brushPos) + new Vector2(0f, BrushH * 0.45f);
        if ((tip - BowlCenter).magnitude < BowlRadius) wet = Mathf.Min(1f, wet + 1.5f * i.dt);

        if (ChickenRect.Contains(tip) && wet > 0.02f)
        {
            float travel = i.delta.magnitude / Screen.height;
            PaintAt(tip);
            wet = Mathf.Max(0f, wet - travel * 0.9f);
        }
    }

    float Coverage()
    {
        int n = 0;
        for (int k = 0; k < coat.Length; k++)
            if (valid[k] && coat[k] > 0.15f) n++;
        return n / (float)validCount;
    }

    public override void Draw(ChefInput i)
    {
        Rect cr = ChickenRect;
        Texture2D t = ctx.Tex("chicken");
        if (t != null)
        {
            GUI.DrawTexture(cr, t, ScaleMode.ScaleToFit);
        }
        else
        {
            OutlineEllipse(new Vector2(cr.x + E1C.x * cr.width, cr.y + E1C.y * cr.height), ER.x * cr.width, ER.y * cr.height, 0.12f);
            OutlineEllipse(new Vector2(cr.x + E2C.x * cr.width, cr.y + E2C.y * cr.height), ER.x * cr.width, ER.y * cr.height, 0.12f);
        }

        float cw = cr.width / Cols;
        float ch = cr.height / Rows;
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
            {
                int idx = y * Cols + x;
                if (valid[idx] && coat[idx] > 0.1f)
                    GuiDraw.Fill(new Rect(cr.x + x * cw, cr.y + y * ch, cw + 1f, ch + 1f), new Color(1f, 1f, 1f, 0.1f * coat[idx]));
            }

        Disc(BowlCenter, BowlRadius, "bowl", 0.3f);

        Vector2 c = Px(brushPos);
        var br = new Rect(c.x - BrushW / 2f, c.y - BrushH / 2f, BrushW, BrushH);
        Texture2D bt = ctx.Tex("brush");
        if (bt != null)
        {
            GUI.DrawTexture(br, bt, ScaleMode.ScaleToFit);
        }
        else
        {
            GuiDraw.Fill(new Rect(br.x + br.width * 0.25f, br.y, br.width * 0.5f, br.height * 0.7f), Color.white);
            GuiDraw.Frame(new Rect(br.x, br.y + br.height * 0.7f, br.width, br.height * 0.3f), Mathf.Lerp(0.15f, 0.8f, wet));
        }
        GuiDraw.Frame(new Rect(br.x, br.yMax + 4f, br.width, 8f));
        GuiDraw.Fill(new Rect(br.x + 2f, br.yMax + 6f, (br.width - 4f) * wet, 4f), Color.white);
    }

    public override void Scores(List<ScoreLine> list)
    {
        list.Add(new ScoreLine("Painted the whole chicken", Coverage()));
    }
}