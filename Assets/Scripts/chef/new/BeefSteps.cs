using System.Collections.Generic;
using UnityEngine;

public static class BeefTable
{
    static readonly float[] Temps = { 48f, 54f, 60f, 65f, 71f };

    static readonly Doneness[,] Grid =
    {
        { Doneness.Rare, Doneness.Overcooked, Doneness.Overcooked, Doneness.Overcooked, Doneness.Overcooked },
        { Doneness.Rare, Doneness.MediumRare, Doneness.WellDone, Doneness.Overcooked, Doneness.Overcooked },
        { Doneness.Raw, Doneness.MediumRare, Doneness.Medium, Doneness.WellDone, Doneness.Overcooked },
        { Doneness.Raw, Doneness.Rare, Doneness.Medium, Doneness.MediumWell, Doneness.WellDone },
        { Doneness.Raw, Doneness.Rare, Doneness.MediumRare, Doneness.MediumWell, Doneness.WellDone }
    };

    public static Doneness Lookup(float temp, float minutes)
    {
        if (temp < 44f || minutes < 0.5f) return Doneness.Raw;
        if (temp > 80f) return Doneness.Overcooked;

        int row = 0;
        float best = float.MaxValue;
        for (int r = 0; r < Temps.Length; r++)
        {
            float d = Mathf.Abs(temp - Temps[r]);
            if (d < best) { best = d; row = r; }
        }
        int col = Mathf.Clamp(Mathf.RoundToInt(minutes), 1, 5) - 1;
        return Grid[row, col];
    }
}

public class BeefSeasonStep : ChefStep
{
    const float Target = 1f;
    const float MaxAmount = 1.3f;
    const float MinShake = 1f;
    const float PerTravel = 0.4f;

    readonly DragSet set = new DragSet();
    readonly List<Vector2> specks = new List<Vector2>();
    readonly List<Color> speckColors = new List<Color>();
    Drag salt, oregano;
    float saltAmount, oreganoAmount, shake, speckAcc, advance;

    Rect MeatRect => N(0.08f, 0.3f, 0.36f, 0.3f);

    protected override void Begin()
    {
        title = "Season the beef";
        hint = "Drag the salt and the oregano over the meat and shake them. Let go and they go back.";
        actionLabel = "Skip";
        salt = set.Add("Salt", "salt", new Vector2(0.72f, 0.38f), 0.17f);
        oregano = set.Add("Oregano", "oregano", new Vector2(0.72f, 0.66f), 0.17f);
    }

    public override void Tick(ChefInput i)
    {
        set.Update(i);
        Drag h = set.held;

        if (h != null && i.down)
        {
            float travel = i.delta.magnitude / Screen.height;
            float speed = i.dt > 0f ? travel / i.dt : 0f;
            shake = Mathf.Lerp(shake, speed, 0.35f);

            if (MeatRect.Contains(h.Center) && shake > MinShake)
            {
                float add = travel * PerTravel;
                Sfx.PlayLimited(Sfx.SaltShaker, 0.12f);
                if (h == salt) saltAmount = Mathf.Min(MaxAmount, saltAmount + add);
                else oreganoAmount = Mathf.Min(MaxAmount, oreganoAmount + add);
                AddSpecks(h, add);
            }
        }
        else
        {
            shake = 0f;
        }

        if (saltAmount >= Target && oreganoAmount >= Target)
        {
            advance += i.dt;
            if (advance >= 0.8f) done = true;
        }
        else
        {
            advance = 0f;
        }
    }

    void AddSpecks(Drag from, float amount)
    {
        speckAcc += amount * 50f;
        Rect mr = MeatRect;
        float cx = (from.Center.x - mr.x) / mr.width;
        while (speckAcc >= 1f)
        {
            speckAcc -= 1f;
            if (specks.Count >= 220)
            {
                specks.RemoveAt(0);
                speckColors.RemoveAt(0);
            }
            specks.Add(new Vector2(Mathf.Clamp01(cx + Random.Range(-0.12f, 0.12f)), Random.Range(0.08f, 0.92f)));
            speckColors.Add(from == salt ? Color.white : Gray(0.55f));
        }
    }

    public override void Draw(ChefInput i)
    {
        Rect mr = MeatRect;
        Box(mr, "meat", "Beef", FS(0.04f), 0.2f);

        float s = Mathf.Max(2f, mr.height * 0.025f);
        for (int k = 0; k < specks.Count; k++)
            GuiDraw.Fill(new Rect(mr.x + specks[k].x * mr.width - s / 2f, mr.y + specks[k].y * mr.height - s / 2f, s, s), speckColors[k]);

        Bar(N(0.08f, 0.68f, 0.36f, 0.03f), "Salt", saltAmount, MaxAmount, Target);
        Bar(N(0.08f, 0.78f, 0.36f, 0.03f), "Oregano", oreganoAmount, MaxAmount, Target);
        DrawDrags(set);
    }

    public override void Scores(List<ScoreLine> list)
    {
        float s = (Closeness(saltAmount, Target, Target) + Closeness(oreganoAmount, Target, Target)) / 2f;
        list.Add(new ScoreLine("The right amount of salt and oregano", s));
    }
}

public class BeefPanStep : ChefStep
{
    readonly DragSet set = new DragSet();
    Drag meat;
    float temp = 20f;
    float elapsed;
    float holdTimer;
    bool inPan;
    Doneness result;
    SfxLoop fry;

    Rect PanRect => N(0.5f, 0.3f, 0.38f, 0.26f);
    Rect DisplayRect => N(0.14f, 0.64f, 0.18f, 0.16f);
    Rect UpRect => N(0.33f, 0.64f, 0.05f, 0.07f);
    Rect DownRect => N(0.33f, 0.73f, 0.05f, 0.07f);

    Doneness Current => inPan ? BeefTable.Lookup(temp, elapsed / ctx.secondsPerMinute) : Doneness.Raw;

    public override bool ActionEnabled => inPan;

    protected override void Begin()
    {
        title = "Cook the beef";
        hint = "Drag the meat into the pan and set the temperature with + and -. Take it off when it's done the way the order wants.";
        actionLabel = "Take it off";
        meat = set.Add("Beef", "meat", new Vector2(0.22f, 0.42f), 0.2f, 1.4f, 0.9f);
    }

    public override void Tick(ChefInput i)
    {
        set.Update(i, !inPan);
        if (!inPan && set.released == meat && PanRect.Contains(meat.Center))
        {
            inPan = true;
            Sfx.Play(Sfx.StoveOn);
            fry = Sfx.Loop(Sfx.Frying);
            meat.stay = true;
            meat.locked = true;
            meat.pos = Nrm(PanRect.center);
        }

        if (inPan) elapsed += i.dt;

        int dir = 0;
        if (i.down)
        {
            if (UpRect.Contains(i.m)) dir = 1;
            else if (DownRect.Contains(i.m)) dir = -1;
        }
        if (dir != 0)
        {
            if (i.pressed)
            {
                temp += dir;
                holdTimer = 0f;
                Sfx.Play(Sfx.ThermostatBeep);
            }
            else
            {
                holdTimer += i.dt;
                if (holdTimer > 0.35f)
                {
                    temp += dir * 15f * i.dt;
                    Sfx.PlayLimited(Sfx.ThermostatBeep, 0.15f);
                }
            }
        }
        else
        {
            holdTimer = 0f;
        }
        temp = Mathf.Clamp(temp, 20f, 100f);
        Sfx.Param(fry, "Heat", Mathf.InverseLerp(20f, 100f, temp));

        meat.shade = Mathf.Lerp(0.9f, 0.12f, (int)Current / 6f);
    }

    public override void Draw(ChefInput i)
    {
        Rect pr = PanRect;
        Box(pr, "pan", "", -1, 0.12f);
        GuiDraw.Fill(new Rect(pr.xMax, pr.center.y - H(0.012f), pr.width * 0.3f, H(0.024f)), Color.white);

        int flames = Mathf.RoundToInt(Mathf.InverseLerp(20f, 100f, temp) * 9f);
        for (int k = 0; k < flames; k++)
            GuiDraw.Fill(new Rect(pr.x + pr.width * (0.14f + k * 0.09f), pr.yMax + 6f, pr.width * 0.04f, H(0.03f)), Color.white);

        GuiDraw.Frame(DisplayRect);
        GuiDraw.Label(DisplayRect, Mathf.RoundToInt(temp) + "°", FS(0.07f), Color.white);
        GuiDraw.Button(UpRect, "+", UpRect.Contains(i.m), true, FS(0.035f));
        GuiDraw.Button(DownRect, "-", DownRect.Contains(i.m), true, FS(0.035f));

        float mins = elapsed / ctx.secondsPerMinute;
        string clock = Mathf.FloorToInt(mins) + ":" + Mathf.FloorToInt((mins % 1f) * 60f).ToString("00");
        GuiDraw.Label(N(0.5f, 0.62f, 0.38f, 0.07f), inPan ? clock : "", FS(0.03f), Color.white);

        DrawDrags(set);
    }

    public override void End() => Sfx.StopLoop(ref fry);

    public override void OnAction()
    {
        result = Current;
        ctx.doneness = result;
        done = true;
    }

    public override void Scores(List<ScoreLine> list)
    {
        int diff = Mathf.Abs((int)result - (int)ctx.targetDoneness);
        list.Add(new ScoreLine("Cooked as ordered (wanted " + DonenessInfo.Name(ctx.targetDoneness) + ", got " + DonenessInfo.Name(result) + ")", 1f - diff * 0.34f, 2f));
    }
}