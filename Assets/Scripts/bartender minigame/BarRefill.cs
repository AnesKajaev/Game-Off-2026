using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class BarRefill : MonoBehaviour
{
    public Key interactKey = Key.E;

    readonly List<PlayerMovement> inside = new List<PlayerMovement>();

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
        if (who == null || who.role != PlayerMovement.Role.Bartender || PlayerMovement.inputLocked || !inside.Contains(who)) return;
        if (!Keyboard.current[interactKey].wasPressedThisFrame) return;

        CrateHands hands = CrateHands.Mine;
        if (hands.carrying == null)
        {
            hands.Say("Fetch new bottles from the storage first");
            return;
        }
        if (!hands.carrying.IsDrink)
        {
            hands.Say("That crate isn't for the bar");
            return;
        }

        BarStock.Refill(hands.carrying.drink);
        hands.Say("Restocked the " + hands.carrying.drink.drinkName);
        hands.Drop();
        Sfx.Play(Sfx.UiSelect);
    }
}