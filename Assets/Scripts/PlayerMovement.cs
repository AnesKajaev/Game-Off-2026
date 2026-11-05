using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public SpriteRenderer sprite;
    public bool isControlled = false; 

    CharacterController controller;
    float verticalVelocity;

    void Awake() => controller = GetComponent<CharacterController>();

    void Update()
    {
        float x = 0f, z = 0f;

        var kb = Keyboard.current;
        if (isControlled && kb != null)
        {
            x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
            z = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
        }

        Transform cam = Camera.main.transform;
        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 move = right * x + fwd * z;
        if (move.sqrMagnitude > 1f) move.Normalize();

        verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + Physics.gravity.y * Time.deltaTime;

        controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (sprite != null && Mathf.Abs(x) > 0.01f)
            sprite.flipX = x < 0;
    }
}