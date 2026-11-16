using UnityEngine;
using UnityEngine.InputSystem;

public class FridgeScreen : MonoBehaviour
{
    static readonly MeatType[] options = { MeatType.Fish, MeatType.Chicken, MeatType.Beef, MeatType.Ground };

    bool open;

    static Rect N(float x, float y, float w, float h) => new Rect(x * Screen.width, y * Screen.height, w * Screen.width, h * Screen.height);
    static int FS(float f) => Mathf.Max(10, Mathf.RoundToInt(Screen.height * f));

    Rect PanelRect => N(0.3f, 0.16f, 0.4f, 0.68f);
    Rect OptionRect(int i) => N(0.34f, 0.28f + i * 0.1f, 0.32f, 0.08f);
    Rect PutAwayRect => N(0.34f, 0.69f, 0.32f, 0.06f);

    bool CarryingDish => ChefHands.Instance != null && ChefHands.Instance.kind == ChefHands.Kind.Cooked;

    public void Open()
    {
        if (open || ChefHands.Instance == null) return;
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

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null && kb[Key.Escape].wasPressedThisFrame) { Close(); return; }
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Vector2 m = GuiDraw.MousePos();
        var hands = ChefHands.Instance;

        if (!CarryingDish)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (!OptionRect(i).Contains(m)) continue;
                hands.HoldRaw(options[i]);
                hands.Say("Took " + MeatInfo.Raw(options[i]).ToLower() + " from the fridge");
                Close();
                return;
            }
        }

        if (!hands.Empty && PutAwayRect.Contains(m))
        {
            hands.Clear();
            hands.Say("Put it away");
            Close();
        }
    }

    void OnGUI()
    {
        if (!open || Event.current.type != EventType.Repaint) return;

        Vector2 m = GuiDraw.MousePos();
        var hands = ChefHands.Instance;

        GuiDraw.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.55f));
        GuiDraw.Fill(PanelRect, new Color(0.88f, 0.93f, 0.96f, 0.98f));
        GuiDraw.Label(N(0.3f, 0.18f, 0.4f, 0.08f), "Fridge", FS(0.05f), new Color(0.1f, 0.15f, 0.2f));

        for (int i = 0; i < options.Length; i++)
        {
            Rect r = OptionRect(i);
            GuiDraw.Button(r, MeatInfo.Raw(options[i]), r.Contains(m), !CarryingDish, FS(0.034f));
        }

        if (hands != null && !hands.Empty)
        {
            string label = CarryingDish ? "Put the dish away" : "Put it back";
            GuiDraw.Button(PutAwayRect, label, PutAwayRect.Contains(m), true, FS(0.026f));
        }

        string hint = CarryingDish ? "Your hands are full with a finished dish" : "Pick the meat you want to cook";
        GuiDraw.Label(N(0.3f, 0.76f, 0.4f, 0.06f), hint + "   (Esc to close)", FS(0.022f), new Color(0.2f, 0.25f, 0.3f));
    }
}