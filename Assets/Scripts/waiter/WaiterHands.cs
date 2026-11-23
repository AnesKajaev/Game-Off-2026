using System.Collections.Generic;
using UnityEngine;

public class WaiterHands : MonoBehaviour
{
    public static WaiterHands Instance { get; private set; }

    public int limit = 2;

    public readonly List<Dish> carrying = new List<Dish>();

    string toast = "";
    float toastTimer;

    public bool CanCarry => carrying.Count < limit;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Add(Dish d) => carrying.Add(d);
    public void Remove(Dish d) => carrying.Remove(d);

    public Dish DishFor(Guest g)
    {
        foreach (var d in carrying)
            if (d.note != null && d.note.guest == g) return d;
        return null;
    }

    public bool HasDishFor(Guest g) => g.CanReceive && DishFor(g) != null;

    public void Say(string msg)
    {
        toast = msg;
        toastTimer = 2.5f;
    }

    void Update()
    {
        if (toastTimer > 0f) toastTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !PlayerMovement.CurrentIs(PlayerMovement.Role.Waiter)) return;

        string text = "Hands: empty";
        if (carrying.Count > 0)
        {
            text = "Carrying: ";
            for (int i = 0; i < carrying.Count; i++)
                text += (i > 0 ? ", " : "") + carrying[i].Name;
        }

        var box = new Rect(16f, Screen.height - 56f, 560f, 40f);
        GuiDraw.Frame(box);
        GuiDraw.Label(box, text, 18, Color.white);

        if (toastTimer > 0f)
        {
            var r = new Rect(Screen.width / 2f - 260f, 80f, 520f, 40f);
            GuiDraw.Frame(r);
            GuiDraw.Label(r, toast, 18, Color.white);
        }
    }
}