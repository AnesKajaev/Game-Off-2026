using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class StorageShelf : MonoBehaviour
{
    public Key interactKey = Key.E;
    public BartenderMinigame3D bar;

    [Header("Station stock")]
    public int fridgeCapacity = 6;
    public int bottleCapacity = 900;

    class Option
    {
        public string label;
        public bool enabled;
        public Crate crate;
    }

    readonly List<PlayerMovement> inside = new List<PlayerMovement>();
    readonly List<Option> options = new List<Option>();
    bool open;

    Rect OptionRect(int i) => ChefStep.N(0.32f, 0.22f + i * 0.09f, 0.36f, 0.075f);
    Rect BackRect => ChefStep.N(0.32f, 0.76f, 0.36f, 0.065f);

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void Awake()
    {
        FridgeStock.Reset(fridgeCapacity);
        BarStock.Reset(bottleCapacity);
    }

    void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponent<PlayerMovement>();
        if (p != null && !inside.Contains(p)) inside.Add(p);
    }

    void OnTriggerExit(Collider other)
    {
        var p = other.GetComponent<PlayerMovement>();
        if (p != null) inside.Remove(p);
    }

    void Update()
    {
        if (open)
        {
            HandleOpen();
            return;
        }

        var who = PlayerMovement.Current;
        if (who == null || PlayerMovement.inputLocked || !inside.Contains(who)) return;
        if (!Keyboard.current[interactKey].wasPressedThisFrame) return;

        OpenFor(who);
    }

    void BuildOptions(PlayerMovement who, CrateHands hands)
    {
        options.Clear();
        bool busy = hands.carrying != null;

        if (who.role == PlayerMovement.Role.Chef)
        {
            foreach (MeatType m in System.Enum.GetValues(typeof(MeatType)))
                options.Add(new Option
                {
                    label = MeatInfo.Raw(m) + "   fridge " + FridgeStock.Count(m) + " / " + FridgeStock.Capacity,
                    enabled = !busy && FridgeStock.NeedsRefill(m),
                    crate = new Crate { meat = m }
                });
        }
        else if (who.role == PlayerMovement.Role.Bartender && bar != null)
        {
            foreach (var d in bar.drinks)
                options.Add(new Option
                {
                    label = d.drinkName + "   bottle " + Mathf.RoundToInt(BarStock.Fraction(d) * 100f) + "%",
                    enabled = !busy && BarStock.NeedsRefill(d),
                    crate = new Crate { drink = d }
                });
        }
    }

    void OpenFor(PlayerMovement who)
    {
        CrateHands hands = CrateHands.Mine;
        if (hands == null)
        {
            Debug.LogWarning("Add a CrateHands component to " + who.name);
            return;
        }

        BuildOptions(who, hands);
        if (options.Count == 0)
        {
            hands.Say("There is nothing for you to refill");
            return;
        }

        open = true;
        PlayerMovement.inputLocked = true;
    }

    void Close()
    {
        open = false;
        PlayerMovement.inputLocked = false;
    }

    void HandleOpen()
    {
        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            Sfx.Play(Sfx.UiBack);
            Close();
            return;
        }
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 m = GuiDraw.MousePos();
        CrateHands hands = CrateHands.Mine;

        for (int i = 0; i < options.Count; i++)
        {
            if (!options[i].enabled || !OptionRect(i).Contains(m)) continue;
            Sfx.Play(Sfx.UiSelect);
            hands.Take(options[i].crate);
            hands.Say("Took " + options[i].crate.Name);
            Close();
            return;
        }

        if (hands.carrying != null && BackRect.Contains(m))
        {
            Sfx.Play(Sfx.UiSelect);
            hands.Drop();
            hands.Say("Put the crate back");
            Close();
        }
    }

    void OnGUI()
    {
        if (!open || Event.current.type != EventType.Repaint) return;

        Vector2 m = GuiDraw.MousePos();
        GuiDraw.Fill(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);
        GuiDraw.Frame(ChefStep.N(0.3f, 0.12f, 0.4f, 0.76f));
        GuiDraw.Label(ChefStep.N(0.3f, 0.14f, 0.4f, 0.08f), "Storage", ChefStep.FS(0.05f), Color.white);

        for (int i = 0; i < options.Count; i++)
        {
            Rect r = OptionRect(i);
            GuiDraw.Button(r, options[i].label, r.Contains(m), options[i].enabled, ChefStep.FS(0.026f));
        }

        if (CrateHands.Mine.carrying != null)
            GuiDraw.Button(BackRect, "Put the crate back", BackRect.Contains(m), true, ChefStep.FS(0.026f));

        GuiDraw.Label(ChefStep.N(0.3f, 0.84f, 0.4f, 0.04f), "Only what needs refilling can be taken   (Esc to close)", ChefStep.FS(0.02f), Color.white);
    }
}