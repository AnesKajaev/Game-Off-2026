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
        armed = true;
    }

    void Update()
    {
        if (!characterInside || !armed || requiredCharacter == null) return;
        if (PlayerMovement.inputLocked) return;
        if (!requiredCharacter.isControlled) return;
        if (done && oneTimeOnly) return;
        if (condition is ITaskCondition c && !c.CanStart) return;

        if (!autoStart)
        {
            if (Keyboard.current == null || !Keyboard.current[interactKey].wasPressedThisFrame) return;
        }

        armed = false;
        done = true;
        Debug.Log($"{name} task triggered by {requiredCharacter.name}");
        onInteract.Invoke();
    }
}