using System.Collections.Generic;
using UnityEngine;

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

public class OrderBoard : MonoBehaviour
{
    public static OrderBoard Instance { get; private set; }

    public Color paperColor = new Color(1f, 0.97f, 0.82f);
    public Color inkColor = new Color(0.15f, 0.12f, 0.1f);
    public float paperWidth = 190f;

    class Note
    {
        public string title;
        public List<string> lines;
        public float angle;
    }

    readonly List<Note> notes = new List<Note>();
    GUIStyle titleStyle, bodyStyle;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Add(string label, Order order)
    {
        notes.Add(new Note
        {
            title = label,
            lines = order.Lines(),
            angle = Random.Range(-3f, 3f)
        });
    }

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