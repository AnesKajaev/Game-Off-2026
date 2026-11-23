using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GuiDraw
{
    static Texture2D white;
    static GUIStyle text;

    static Texture2D White
    {
        get
        {
            if (white == null)
            {
                white = new Texture2D(1, 1);
                white.SetPixel(0, 0, Color.white);
                white.Apply();
            }
            return white;
        }
    }

    static GUIStyle Text
    {
        get
        {
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };
                text.normal.textColor = Color.black;
            }
            return text;
        }
    }

    public static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, White);
        GUI.color = old;
    }

    static Texture2D circleTex;

    static Texture2D CircleTex
    {
        get
        {
            if (circleTex == null)
            {
                int s = 128;
                circleTex = new Texture2D(s, s, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var px = new Color[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f) / s * 2f - 1f;
                        float dy = (y + 0.5f) / s * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * s * 0.5f));
                    }
                circleTex.SetPixels(px);
                circleTex.Apply();
            }
            return circleTex;
        }
    }

    public static void Circle(Vector2 c, float radius, Color col)
    {
        Color old = GUI.color;
        GUI.color = col;
        GUI.DrawTexture(new Rect(c.x - radius, c.y - radius, radius * 2f, radius * 2f), CircleTex);
        GUI.color = old;
    }

    public static void Line(Vector2 a, Vector2 b, float width, Color col)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        if (len < 0.01f) return;
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a);
        Fill(new Rect(a.x, a.y - width / 2f, len, width), col);
        GUI.matrix = old;
    }

    public static void FillRot(Rect r, float angle, Color col)
    {
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, r.center);
        Fill(r, col);
        GUI.matrix = old;
    }

    public static void FillPolar(Vector2 c, float[] radii, Color col)
    {
        int n = radii.Length;
        float step = Mathf.PI * 2f / n;
        for (int k = 0; k < n; k++)
        {
            float a = k * step;
            float r = radii[k];
            Line(c, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, r * step * 1.35f + 1.5f, col);
        }
    }

    public static void FillFan(Vector2 c, Vector2[] pts, Color col)
    {
        int n = pts.Length;
        for (int k = 0; k < n; k++)
        {
            Vector2 a = pts[k];
            Vector2 b = pts[(k + 1) % n];
            Line(c, a, (a - b).magnitude * 1.4f + 1.5f, col);
        }
    }

    public static Vector2 MousePos()
    {
        if (Mouse.current == null) return new Vector2(-1000f, -1000f);
        Vector2 p = Mouse.current.position.ReadValue();
        return new Vector2(p.x, Screen.height - p.y);
    }

    public static void Label(Rect r, string msg, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var st = Text;
        st.fontSize = size;
        st.alignment = anchor;
        st.normal.textColor = color;
        GUI.Label(r, msg, st);
        st.fontSize = 16;
        st.alignment = TextAnchor.MiddleCenter;
        st.normal.textColor = Color.black;
    }

    public static void Frame(Rect r, float fill = 0f, float t = 2f)
    {
        Fill(r, Color.white);
        Fill(new Rect(r.x + t, r.y + t, r.width - 2f * t, r.height - 2f * t), new Color(fill, fill, fill));
    }

    public static void FrameRot(Rect r, float angle, float fill = 0f, float t = 2f)
    {
        FillRot(r, angle, Color.white);
        FillRot(new Rect(r.x + t, r.y + t, r.width - 2f * t, r.height - 2f * t), angle, new Color(fill, fill, fill));
    }

    public static void Button(Rect r, string msg, bool hover, bool enabled = true, int size = 22)
    {
        Color gray = new Color(0.4f, 0.4f, 0.4f);
        Fill(r, enabled ? Color.white : gray);
        bool solid = enabled && !hover;
        Fill(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), solid ? Color.white : Color.black);
        Label(r, msg, size, !enabled ? gray : solid ? Color.black : Color.white);
    }

    public static void Bubble(Vector3 worldPos, string msg, float width = 220f, float offsetY = 0f)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(worldPos);
        if (sp.z <= 0f) return;

        float h = Text.CalcHeight(new GUIContent(msg), width - 16f) + 12f;
        var r = new Rect(sp.x - width / 2f, Screen.height - sp.y - h + offsetY, width, h);

        Fill(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), new Color(0f, 0f, 0f, 0.3f));
        Fill(r, Color.white);
        GUI.Label(new Rect(r.x + 8f, r.y + 6f, r.width - 16f, r.height - 12f), msg, Text);
    }
}

public class OrderNote
{
    public string title;
    public List<string> lines;
    public float angle;
    public Order order;
    public bool claimed;
    public Guest guest;
}

public class OrderBoard : MonoBehaviour
{
    public static OrderBoard Instance { get; private set; }

    public Color paperColor = new Color(1f, 0.97f, 0.82f);
    public Color inkColor = new Color(0.15f, 0.12f, 0.1f);
    public float paperWidth = 190f;

    readonly List<OrderNote> notes = new List<OrderNote>();
    GUIStyle titleStyle, bodyStyle;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    public OrderNote Add(string label, Order order)
    {
        var n = new OrderNote
        {
            title = label,
            lines = order.Lines(),
            order = order,
            angle = Random.Range(-3f, 3f)
        };
        notes.Add(n);
        return n;
    }

    public OrderNote Claim(MeatType m)
    {
        foreach (var n in notes)
        {
            if (n.claimed || n.order == null || !n.order.HasMeat(m)) continue;
            n.claimed = true;
            return n;
        }
        return null;
    }

    public void Release(OrderNote n)
    {
        if (n != null) n.claimed = false;
    }

    public void Remove(OrderNote n) => notes.Remove(n);

    public void Remove(string label) => notes.RemoveAll(n => n.title == label);
    public void Clear() => notes.Clear();

    [ContextMenu("Add test note")]
    void AddTestNote() => Add("Table 1 - Seat 1", Order.Generate());

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || notes.Count == 0 || !PlayerMovement.SeesFoodUI) return;

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            titleStyle.normal.textColor = inkColor;
            bodyStyle.normal.textColor = inkColor;
        }

        float x = Screen.width - paperWidth - 20f;
        float y = 20f;

        foreach (var n in notes)
        {
            float h = 44f + n.lines.Count * 22f;
            if (y + h > Screen.height - 20f) { x -= paperWidth + 14f; y = 20f; }

            var rect = new Rect(x, y, paperWidth, h);
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(n.angle, rect.center);

            GuiDraw.Fill(new Rect(rect.x + 4f, rect.y + 4f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.25f));
            GuiDraw.Fill(rect, paperColor);
            GuiDraw.Fill(new Rect(rect.x + 10f, rect.y + 36f, rect.width - 20f, 1f), new Color(0f, 0f, 0f, 0.25f));

            GUI.Label(new Rect(rect.x + 10f, rect.y + 6f, rect.width - 20f, 28f), n.title, titleStyle);
            for (int i = 0; i < n.lines.Count; i++)
                GUI.Label(new Rect(rect.x + 10f, rect.y + 42f + i * 22f, rect.width - 20f, 22f), n.lines[i], bodyStyle);

            GUI.matrix = old;
            y += h + 14f;
        }
    }
}