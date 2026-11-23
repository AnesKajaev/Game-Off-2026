using UnityEngine;

public class InfoPopup : MonoBehaviour
{
    [TextArea] public string message = "No one is waiting at the bar. You can't make a drink yet.";
    public float seconds = 3f;

    float timer;

    public void Show() => timer = seconds;

    public void ShowText(string text)
    {
        message = text;
        timer = seconds;
    }

    void Update()
    {
        if (timer > 0f) timer -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (timer <= 0f || Event.current.type != EventType.Repaint) return;

        var r = new Rect(Screen.width / 2f - 330f, Screen.height * 0.18f, 660f, 70f);
        GuiDraw.Frame(r);
        GuiDraw.Label(r, message, 22, Color.white);
    }
}