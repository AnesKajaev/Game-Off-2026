using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public enum Role { None, Waiter, Chef, Bartender }

    public static PlayerMovement Current { get; private set; }
    public static bool inputLocked;
    public static bool SeesFoodUI => Current != null && (Current.role == Role.Waiter || Current.role == Role.Chef);
    public static bool CurrentIs(Role r) => Current != null && Current.role == r;

    public float speed = 5f;
    public SpriteRenderer sprite;
    public Role role = Role.None;

    [Header("Home spot")]
    [Tooltip("Where this character walks back to when you switch away. Empty = where it starts in the scene")]
    public Transform homePoint;
    public float returnDelay = 0.5f;
    [Range(0.1f, 1f)] public float returnSpeedFactor = 0.6f;
    public float arriveDistance = 0.1f;

    public bool isControlled
    {
        get => controlled;
        set
        {
            controlled = value;
            if (value) Current = this;
            else if (Current == this) Current = null;
        }
    }

    CharacterController controller;
    NavMeshPath path;
    Vector3 startPosition;
    bool controlled;
    bool hasPath;
    int cornerIndex;
    float idleTimer;
    float repathTimer;
    float verticalVelocity;

    Vector3 Home => homePoint != null ? homePoint.position : startPosition;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        startPosition = transform.position;
        path = new NavMeshPath();
    }

    void Update()
    {
        Vector3 dir = Vector3.zero;

        if (isControlled)
        {
            idleTimer = 0f;
            hasPath = false;
            repathTimer = 0f;
            if (!inputLocked) dir = ReadInput();
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= returnDelay) dir = StepTowardsHome();
        }

        verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + Physics.gravity.y * Time.deltaTime;
        float s = isControlled ? speed : speed * returnSpeedFactor;
        controller.Move((dir * s + Vector3.up * verticalVelocity) * Time.deltaTime);

        UpdateFlip(dir);
    }

    Vector3 ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector3.zero;

        float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float z = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);

        Transform cam = Camera.main.transform;
        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 move = right * x + fwd * z;
        if (move.sqrMagnitude > 1f) move.Normalize();
        return move;
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    Vector3 StepTowardsHome()
    {
        Vector3 target = Home;
        if (Flat(target - transform.position).magnitude <= arriveDistance)
        {
            hasPath = false;
            return Vector3.zero;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = 0.5f;
            hasPath = false;
            if (NavMesh.SamplePosition(transform.position, out var a, 1f, NavMesh.AllAreas)
                && NavMesh.SamplePosition(target, out var b, 1f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path)
                && path.status != NavMeshPathStatus.PathInvalid
                && path.corners.Length > 1)
            {
                hasPath = true;
                cornerIndex = 1;
            }
        }

        Vector3 goal = target;
        if (hasPath)
        {
            while (cornerIndex < path.corners.Length - 1
                   && Flat(path.corners[cornerIndex] - transform.position).magnitude < 0.1f)
                cornerIndex++;
            goal = path.corners[cornerIndex];
        }

        Vector3 step = Flat(goal - transform.position);
        return step.sqrMagnitude < 0.0001f ? Vector3.zero : step.normalized;
    }

    void UpdateFlip(Vector3 dir)
    {
        if (sprite == null || dir.sqrMagnitude < 0.0001f) return;
        float side = Vector3.Dot(dir, Camera.main.transform.right);
        if (Mathf.Abs(side) > 0.01f) sprite.flipX = side < 0f;
    }
}