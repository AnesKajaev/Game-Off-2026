using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class PassTable : MonoBehaviour
{
    public Key interactKey = Key.E;
    public int capacity = 6;
    [Tooltip("Optional: a small object shown on the table for every dish")]
    public GameObject dishPrefab;
    [Tooltip("Optional: empty objects on the table top where the dish objects appear")]
    public Transform[] slots;
    public Transform labelPoint;

    readonly List<Dish> dishes = new List<Dish>();
    readonly List<PlayerMovement> inside = new List<PlayerMovement>();
    readonly Dictionary<Dish, int> slotOf = new Dictionary<Dish, int>();
    GameObject[] shown;

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
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
        var who = PlayerMovement.Current;
        if (who == null || PlayerMovement.inputLocked || !inside.Contains(who)) return;
        if (!Keyboard.current[interactKey].wasPressedThisFrame) return;

        if (who.role == PlayerMovement.Role.Chef) Place();
        else if (who.role == PlayerMovement.Role.Waiter) Take();
    }

    void Place()
    {
        var hands = ChefHands.Instance;
        if (hands.kind != ChefHands.Kind.Cooked)
        {
            hands.Say("You have no finished dish to put here");
            return;
        }
        if (dishes.Count >= capacity)
        {
            hands.Say("The table is full");
            return;
        }

        Dish d = hands.Give();
        dishes.Add(d);
        Show(d);
        hands.Say("Put the " + d.Name.ToLower() + " on the table");
    }

    void Take()
    {
        var waiter = WaiterHands.Instance;
        if (dishes.Count == 0)
        {
            waiter.Say("There is nothing to pick up yet");
            return;
        }
        if (!waiter.CanCarry)
        {
            waiter.Say("Your hands are full");
            return;
        }

        Dish d = dishes[0];
        dishes.RemoveAt(0);
        Hide(d);
        waiter.Add(d);
        waiter.Say("Picked up the " + d.Name.ToLower());
    }

    void Show(Dish d)
    {
        if (dishPrefab == null || slots == null) return;
        if (shown == null) shown = new GameObject[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            if (shown[i] != null) continue;
            shown[i] = Instantiate(dishPrefab, slots[i].position, slots[i].rotation);
            slotOf[d] = i;
            return;
        }
    }

    void Hide(Dish d)
    {
        int i;
        if (!slotOf.TryGetValue(d, out i)) return;
        Destroy(shown[i]);
        shown[i] = null;
        slotOf.Remove(d);
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || dishes.Count == 0 || !PlayerMovement.SeesFoodUI) return;

        Camera cam = Camera.main;
        Vector3 sp = cam.WorldToScreenPoint(labelPoint != null ? labelPoint.position : transform.position + Vector3.up * 0.6f);
        if (sp.z <= 0f) return;

        float w = 320f;
        float h = 12f + dishes.Count * 22f;
        var r = new Rect(sp.x - w / 2f, Screen.height - sp.y - h, w, h);
        GuiDraw.Frame(r);
        for (int i = 0; i < dishes.Count; i++)
            GuiDraw.Label(new Rect(r.x, r.y + 6f + i * 22f, w, 22f), dishes[i].Label, 15, Color.white);
    }
}