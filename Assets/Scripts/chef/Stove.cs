using UnityEngine;

public class Stove : MonoBehaviour
{
    public ChefMinigame minigame;

    public void Interact()
    {
        var hands = ChefHands.Instance;
        if (hands == null) return;

        if (hands.kind == ChefHands.Kind.Raw)
        {
            if (minigame == null)
            {
                hands.Say("The stove isn't set up yet");
                return;
            }
            minigame.Open(hands.meat);
            return;
        }

        hands.Say(hands.kind == ChefHands.Kind.Cooked ? "Your hands are full" : "Get some meat from the fridge first");
    }
}