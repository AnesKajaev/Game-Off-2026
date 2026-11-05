using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class TaskTrigger : MonoBehaviour
{
    public PlayerMovement requiredCharacter;
    public Key interactKey = Key.E;
    public bool oneTimeOnly = true;
    public UnityEvent onInteract;        
    bool characterInside;
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
        if (other.GetComponent<PlayerMovement>() == requiredCharacter)
            characterInside = false;
    }

    void Update()
    {
        if (!characterInside || requiredCharacter == null) return;
        if (!requiredCharacter.isControlled) return;  
        if (done && oneTimeOnly) return;
        if (Keyboard.current == null || !Keyboard.current[interactKey].wasPressedThisFrame) return;

        done = true;
        Debug.Log($"{name} task triggered by {requiredCharacter.name}");
        onInteract.Invoke();
    }
}