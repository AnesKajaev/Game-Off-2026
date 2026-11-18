using UnityEngine;
using UnityEngine.InputSystem;

public class DoorTrigger : MonoBehaviour
{
    private TruckDoorOpen door;
    private bool inRange;

    void Start()
    {
        door = GetComponent<TruckDoorOpen>();
    }

    void Update()
    {
        if (inRange &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            door.ToggleDoor();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        inRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        inRange = false;
    }
}