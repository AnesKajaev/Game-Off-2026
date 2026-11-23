using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class TaskTrigger : MonoBehaviour
{
    public PlayerMovement requiredCharacter;
    public bool autoStart = false;
    public Key interactKey = Key.E;
    public bool oneTimeOnly = true;
    [Tooltip("Optional: the task only starts while this script says CanStart (the bartender minigame uses this)")]
    public MonoBehaviour condition;
    public UnityEvent onInteract;
    [Tooltip("Runs when the player tries to use the box but the condition says not yet, for example to show a message")]
    public UnityEvent onBlocked;

    bool characterInside;
    bool armed = true;
    bool blockedShown;
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
        armed = true;
        blockedShown = false;
    }

    void Update()
    {
        if (!characterInside || !armed || requiredCharacter == null) return;
        if (PlayerMovement.inputLocked) return;
        if (!requiredCharacter.isControlled) return;
        if (done && oneTimeOnly) return;

        bool attempt = autoStart || (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame);
        if (!attempt) return;

        if (condition is ITaskCondition c && !c.CanStart)
        {
            if (!autoStart || !blockedShown)
            {
                c.OnBlocked();
                onBlocked.Invoke();
            }
            blockedShown = true;
            return;
        }

        if (autoStart) armed = false;
        done = true;
        onInteract.Invoke();
    }
}