using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class WaiterInteract : MonoBehaviour
{
    public Key interactKey = Key.E;
    public float range = 1.2f;

    PlayerMovement pm;
    Guest target;

    void Awake() => pm = GetComponent<PlayerMovement>();

    void Update()
    {
        target = null;
        if (!pm.isControlled || PlayerMovement.inputLocked) return;

        float best = range * range;
        foreach (var g in Guest.All)
        {
            if (!g.NeedsOrder) continue;
            Vector3 d = g.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < best) { best = d.sqrMagnitude; target = g; }
        }

        if (target != null && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            target.TakeOrder();
            target = null;
        }
    }

    void OnGUI()
    {
        if (target != null && Event.current.type == EventType.Repaint)
            GuiDraw.Bubble(target.HeadPoint, "Press " + interactKey, 110f, -42f);
    }
}