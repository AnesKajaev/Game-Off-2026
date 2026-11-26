using UnityEngine;

public class Crate
{
    public MeatType meat;
    public DrinkData drink;

    public bool IsDrink => drink != null;
    public string Name => IsDrink ? drink.drinkName + " bottles" : MeatInfo.Raw(meat) + " crate";
}

public class CrateHands : MonoBehaviour
{
    public Crate carrying { get; private set; }

    string toast = "";
    float toastTimer;

    public static CrateHands Mine
    {
        get
        {
            PlayerMovement p = PlayerMovement.Current;
            return p != null ? p.GetComponent<CrateHands>() : null;
        }
    }

    public void Take(Crate c) => carrying = c;

    public Crate Drop()
    {
        Crate c = carrying;
        carrying = null;
        return c;
    }

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
        if (Event.current.type != EventType.Repaint) return;
        if (PlayerMovement.Current == null || PlayerMovement.Current.gameObject != gameObject) return;

        if (carrying != null)
        {
            var box = new Rect(16f, Screen.height - 104f, 460f, 40f);
            GuiDraw.Frame(box);
            GuiDraw.Label(box, "Carrying: " + carrying.Name, 18, Color.white);
        }

        if (toastTimer > 0f)
        {
            var r = new Rect(Screen.width / 2f - 260f, 130f, 520f, 40f);
            GuiDraw.Frame(r);
            GuiDraw.Label(r, toast, 18, Color.white);
        }
    }
}