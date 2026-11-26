using System.Collections.Generic;
using UnityEngine;

public struct ChefInput
{
    public Vector2 m;
    public Vector2 delta;
    public bool pressed;
    public bool down;
    public bool released;
    public bool rPressed;
    public bool rDown;
    public float dt;
}

public class ScoreLine
{
    public string label;
    public float value;
    public float weight;

    public ScoreLine(string label, float value, float weight = 1f)
    {
        this.label = label;
        this.value = Mathf.Clamp01(value);
        this.weight = weight;
    }
}

public class ChefCtx
{
    public MeatType meat;
    public Order order;
    public Doneness targetDoneness = Doneness.Medium;
    public int targetNuggets = 8;

    public Doneness doneness = Doneness.Medium;
    public int nuggets;
    public int balls;

    public float secondsPerMinute = 6f;
    public float sideSeconds = 6f;
    public int ballsToMake = 5;

    public System.Func<string, Texture2D> art;
    public Texture2D Tex(string n) => art != null ? art(n) : null;
}

public class Drag
{
    public string name;
    public string art;
    public float shade;
    public Vector2 home;
    public Vector2 pos;
    public float h = 0.14f;
    public float aspect = 1f;
    public bool held;
    public bool visible = true;
    public bool stay;
    public bool locked;

    public Vector2 Center => ChefStep.Px(pos);

    public Rect Area
    {
        get
        {
            float hh = h * Screen.height;
            float ww = hh * aspect;
            Vector2 c = Center;
            return new Rect(c.x - ww / 2f, c.y - hh / 2f, ww, hh);
        }
    }
}

public class DragSet
{
    public readonly List<Drag> items = new List<Drag>();
    public Drag held;
    public Drag released;
    public float returnSpeed = 10f;

    Vector2 grab;

    public Drag Add(string name, string art, Vector2 home, float h, float aspect = 1f, float shade = 0f)
    {
        var d = new Drag { name = name, art = art, home = home, pos = home, h = h, aspect = aspect, shade = shade };
        items.Add(d);
        return d;
    }

    public void Update(ChefInput i, bool allowPick = true)
    {
        released = null;

        if (i.pressed && allowPick && held == null)
        {
            for (int k = items.Count - 1; k >= 0; k--)
            {
                var d = items[k];
                if (!d.visible || d.locked || !d.Area.Contains(i.m)) continue;
                held = d;
                d.held = true;
                grab = d.Center - i.m;
                break;
            }
        }

        if (held != null)
        {
            if (i.down)
            {
                held.pos = ChefStep.Nrm(i.m + grab);
            }
            else
            {
                held.held = false;
                released = held;
                held = null;
            }
        }

        foreach (var d in items)
            if (!d.held && !d.stay)
                d.pos = Vector2.Lerp(d.pos, d.home, 1f - Mathf.Exp(-returnSpeed * i.dt));
    }
}

public abstract class ChefStep
{
    protected ChefCtx ctx;
    public string title = "";
    public string hint = "";
    public string actionLabel;
    public bool done;

    public virtual bool ActionEnabled => true;

    public void Init(ChefCtx c)
    {
        ctx = c;
        Begin();
    }

    protected virtual void Begin() { }
    public abstract void Tick(ChefInput i);
    public abstract void Draw(ChefInput i);
    public virtual void OnAction() { done = true; }
    public virtual void End() { }
    public abstract void Scores(List<ScoreLine> list);

    public static Rect N(float x, float y, float w, float h) => new Rect(x * Screen.width, y * Screen.height, w * Screen.width, h * Screen.height);
    public static Vector2 Px(Vector2 n) => new Vector2(n.x * Screen.width, n.y * Screen.height);
    public static Vector2 Nrm(Vector2 p) => new Vector2(p.x / Screen.width, p.y / Screen.height);
    public static int FS(float f) => Mathf.Max(10, Mathf.RoundToInt(Screen.height * f));
    public static float H(float f) => f * Screen.height;

    protected static Color Gray(float s) => new Color(s, s, s);
    protected static float Closeness(float v, float target, float range) => Mathf.Clamp01(1f - Mathf.Abs(v - target) / range);

    protected void Box(Rect r, string art, string label, int size = -1, float shade = 0f)
    {
        Texture2D t = ctx.Tex(art);
        if (t != null)
        {
            GUI.DrawTexture(r, t, ScaleMode.ScaleToFit);
            return;
        }
        GuiDraw.Frame(r, shade);
        if (!string.IsNullOrEmpty(label))
            GuiDraw.Label(r, label, size > 0 ? size : FS(0.022f), shade > 0.5f ? Color.black : Color.white);
    }

    protected void Disc(Vector2 center, float radius, string art, float shade = 0f)
    {
        Texture2D t = ctx.Tex(art);
        if (t != null)
        {
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), t, ScaleMode.ScaleToFit);
            return;
        }
        GuiDraw.Circle(center, radius, Color.white);
        GuiDraw.Circle(center, radius - 2f, Gray(shade));
    }

    protected void DrawDrag(Drag d) => Box(d.Area, d.art, d.name, FS(0.02f), d.shade);

    protected void DrawDrags(DragSet set)
    {
        foreach (var d in set.items)
            if (d.visible && !d.held) DrawDrag(d);
        if (set.held != null && set.held.visible) DrawDrag(set.held);
    }

    protected void Bar(Rect r, string label, float value, float max, float target)
    {
        GuiDraw.Label(new Rect(r.x, r.y - Screen.height * 0.04f, r.width, Screen.height * 0.04f), label, FS(0.022f), Color.white, TextAnchor.MiddleLeft);
        GuiDraw.Frame(r);
        GuiDraw.Fill(new Rect(r.x + 2f, r.y + 2f, (r.width - 4f) * Mathf.Clamp01(value / max), r.height - 4f), Color.white);
        if (target > 0f)
            GuiDraw.Fill(new Rect(r.x + r.width * (target / max) - 1.5f, r.y - 4f, 3f, r.height + 8f), Gray(0.55f));
    }

    static readonly float[] ellipseBuffer = new float[48];

    public static void FillEllipse(Vector2 c, float rx, float ry, Color col)
    {
        for (int k = 0; k < ellipseBuffer.Length; k++)
        {
            float t = k * Mathf.PI * 2f / ellipseBuffer.Length;
            float cs = Mathf.Cos(t);
            float sn = Mathf.Sin(t);
            ellipseBuffer[k] = rx * ry / Mathf.Sqrt(ry * ry * cs * cs + rx * rx * sn * sn);
        }
        GuiDraw.FillPolar(c, ellipseBuffer, col);
    }

    protected static void OutlineEllipse(Vector2 c, float rx, float ry, float fill)
    {
        FillEllipse(c, rx, ry, Color.white);
        FillEllipse(c, rx - 3f, ry - 3f, Gray(fill));
    }
}