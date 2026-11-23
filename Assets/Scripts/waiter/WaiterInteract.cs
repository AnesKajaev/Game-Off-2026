using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class WaiterInteract : MonoBehaviour
{
    public Key interactKey = Key.E;
    public float range = 1.2f;

    PlayerMovement pm;
    Guest target;
    bool serving;

    void Awake() => pm = GetComponent<PlayerMovement>();

    void Update()
    {
        target = null;
        if (!pm.isControlled || PlayerMovement.inputLocked) return;

        var hands = WaiterHands.Instance;
        float best = range * range;
        foreach (var g in Guest.All)
        {
            bool canServe = hands != null && hands.HasDishFor(g);
            if (!g.NeedsOrder && !canServe) continue;

            Vector3 d = g.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < best)
            {
                best = d.sqrMagnitude;
                target = g;
                serving = canServe;
            }
        }

        if (target == null || Keyboard.current == null || !Keyboard.current[interactKey].wasPressedThisFrame) return;

        if (serving) Serve(target);
        else target.TakeOrder();
        target = null;
    }

    void Serve(Guest g)
    {
        var hands = WaiterHands.Instance;
        Dish d = hands.DishFor(g);
        hands.Remove(d);
        g.Receive(d);
        hands.Say("Served the " + d.Name.ToLower() + " (" + Mathf.RoundToInt(d.quality * 100f) + "%)");
    }

    void OnGUI()
    {
        if (target != null && Event.current.type == EventType.Repaint)
            GuiDraw.Bubble(target.HeadPoint, "Press " + interactKey + (serving ? " to serve" : ""), 150f, -42f);
    }
}