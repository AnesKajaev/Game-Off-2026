using UnityEngine;

public class TruckDoorOpen : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    public Axis rotationAxis = Axis.Y;
    public float openAngle = 65f;
    public float smoothSpeed = 5f;

    private bool isOpen;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.localRotation;

        Vector3 offset = Vector3.zero;
        if (rotationAxis == Axis.X) offset.x = openAngle;
        else if (rotationAxis == Axis.Y) offset.y = openAngle;
        else offset.z = openAngle;

        openRotation = closedRotation * Quaternion.Euler(offset);
    }

    void Update()
    {
        Quaternion target = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * smoothSpeed);
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
    }
}