using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class TaskTrigger : MonoBehaviour
{
    public PlayerMovement requiredCharacter;   // the only one who can use this box
    public bool autoStart = false;             // true = starts on walk-in, no key needed
    public Key interactKey = Key.E;
    public bool oneTimeOnly = true;
    public UnityEvent onInteract;              // hook up the task in the Inspector

    bool characterInside;
    bool armed = true;
    bool done;

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() == requiredCharacter)
            characterInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() != requiredCharacter) return;
        characterInside = false;
        armed = true;   // walking out re-arms the box
    }

    void Update()
    {
        if (!characterInside || !armed || requiredCharacter == null) return;
        if (PlayerMovement.inputLocked) return;            // a minigame is open
        if (!requiredCharacter.isControlled) return;       // must be the active character
        if (done && oneTimeOnly) return;

        if (!autoStart)
        {
            if (Keyboard.current == null || !Keyboard.current[interactKey].wasPressedThisFrame) return;
        }

        armed = false;   // prevents reopening right after the minigame closes
        done = true;
        Debug.Log($"{name} task triggered by {requiredCharacter.name}");
        onInteract.Invoke();
    }
}