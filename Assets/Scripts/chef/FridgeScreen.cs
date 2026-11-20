using UnityEngine;
using UnityEngine.InputSystem;

public class FridgeScreen : MonoBehaviour
{
    static readonly MeatType[] options = { MeatType.Fish, MeatType.Chicken, MeatType.Beef, MeatType.Ground };

    bool open;

    static Rect N(float x, float y, float w, float h) => ChefStep.N(x, y, w, h);
    static int FS(float f) => ChefStep.FS(f);

    Rect OptionRect(int i) => N(0.34f, 0.28f + i * 0.1f, 0.32f, 0.08f);
    Rect PutAwayRect => N(0.34f, 0.69f, 0.32f, 0.06f);

    public void Open()
    {
        open = true;
        PlayerMovement.inputLocked = true;
    }

    public void Close()
    {
        open = false;
        PlayerMovement.inputLocked = false;
    }

    void Update()
    {
        if (!open) return;

        if (Keyboard.current[Key.Escape].wasPressedThisFrame) { Close(); return; }
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 m = GuiDraw.MousePos();
        var hands = ChefHands.Instance;

        for (int i = 0; i < options.Length; i++)
        {
            if (!OptionRect(i).Contains(m)) continue;
            hands.HoldRaw(options[i]);
            hands.Say("Took " + MeatInfo.Raw(options[i]).ToLower());
            Close();
            return;
        }

        if (!hands.Empty && PutAwayRect.Contains(m))
        {
            hands.Clear();
            Close();
        }
    }

    void OnGUI()
    {
        if (!open || Event.current.type != EventType.Repaint) return;

        Vector2 m = GuiDraw.MousePos();

        GuiDraw.Fill(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);
        GuiDraw.Frame(N(0.3f, 0.16f, 0.4f, 0.68f));
        GuiDraw.Label(N(0.3f, 0.18f, 0.4f, 0.08f), "Fridge", FS(0.05f), Color.white);

        for (int i = 0; i < options.Length; i++)
        {
            Rect r = OptionRect(i);
            GuiDraw.Button(r, MeatInfo.Raw(options[i]), r.Contains(m), true, FS(0.034f));
        }

        if (!ChefHands.Instance.Empty)
            GuiDraw.Button(PutAwayRect, "Put it away", PutAwayRect.Contains(m), true, FS(0.026f));

        GuiDraw.Label(N(0.3f, 0.76f, 0.4f, 0.06f), "Esc to close", FS(0.022f), Color.white);
    }
}