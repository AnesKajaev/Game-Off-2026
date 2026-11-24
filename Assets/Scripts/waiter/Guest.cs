using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Guest : MonoBehaviour
{
    public enum State
    {
        WalkingToEntrance, WalkingToTable, Sitting, WaitingForWaiter, Ordering, WaitingForFood,
        StandingUp, WalkingToBar, WaitingAtBar, ReturningToSeat, Eating, Leaving
    }

    public static readonly List<Guest> All = new List<Guest>();

    public SpriteRenderer sprite;
    public float headHeight = 0.6f;
    public float sitSpeed = 1.5f;
    public float orderSpeakTime = 3f;
    public float eatTime = 8f;

    [Header("Bar trips")]
    [Tooltip("A seated guest waits a random time between these, then gets up to ask for drinks")]
    public float barDelayMin = 15f;
    public float barDelayMax = 45f;
    public int maxBarTrips = 1;
    [Tooltip("Seconds a guest waits at the bar before giving up. 0 = waits forever")]
    public float barPatience = 45f;

    [HideInInspector] public Table table;
    [HideInInspector] public Seat seat;
    [HideInInspector] public Order order;
    [HideInInspector] public List<DrinkData> drinkRequest;

    public State state { get; private set; }
    public bool NeedsOrder => state == State.WaitingForWaiter;
    public bool NeedsDrinks => state == State.WaitingAtBar;
    public bool CanReceive => state == State.WaitingForFood;
    public float PatienceLeft01 => barPatience <= 0f ? 1f : Mathf.Clamp01(1f - barWait / barPatience);
    public float satisfaction { get; private set; }
    public Vector3 HeadPoint => transform.position + Vector3.up * headHeight;

    NavMeshAgent agent;
    Transform entrance;
    Transform exit;
    Transform barSpot;
    OrderNote note;
    bool leaving;
    float eatTimer;
    float barWait;
    string reaction = "";
    float reactionTimer;
    State resumeState = State.WaitingForWaiter;
    float speakTimer;
    float barTimer;
    int barTrips;
    string speech = "";

    void Awake() => agent = GetComponent<NavMeshAgent>();
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void OnDestroy()
    {
        if (seat != null) seat.Release(this);
        if (BarCounter.Instance != null) BarCounter.Instance.Release(this);
    }

    public void Init(Table t, Seat s, Transform entrancePoint, Transform exitPoint)
    {
        table = t;
        seat = s;
        entrance = entrancePoint;
        exit = exitPoint;

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("Guest spawned off the NavMesh. Move the spawn point onto the baked NavMesh.", this);
            return;
        }

        if (entrance != null)
        {
            state = State.WalkingToEntrance;
            agent.SetDestination(entrance.position);
        }
        else
        {
            state = State.WalkingToTable;
            agent.SetDestination(seat.approachPoint.position);
        }
    }

    bool Arrived()
    {
        return !agent.pathPending
            && agent.remainingDistance <= agent.stoppingDistance + 0.02f
            && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);
    }

    void Update()
    {
        if (table == null || seat == null) return;
        float dt = Time.deltaTime;
        if (reactionTimer > 0f) reactionTimer -= dt;

        switch (state)
        {
            case State.WalkingToEntrance:
                if (agent.isOnNavMesh && Arrived())
                {
                    state = State.WalkingToTable;
                    agent.SetDestination(seat.approachPoint.position);
                }
                break;

            case State.WalkingToTable:
                if (agent.isOnNavMesh && Arrived())
                {
                    agent.enabled = false;
                    resumeState = State.WaitingForWaiter;
                    state = State.Sitting;
                }
                break;

            case State.Sitting:
                transform.position = Vector3.MoveTowards(transform.position, seat.sitPoint.position, sitSpeed * dt);
                if ((transform.position - seat.sitPoint.position).sqrMagnitude < 0.0001f)
                {
                    state = resumeState;
                    barTimer = Random.Range(barDelayMin, barDelayMax);
                }
                break;

            case State.WaitingForWaiter:
            case State.WaitingForFood:
                CountdownToBar(dt);
                break;

            case State.Ordering:
                speakTimer -= dt;
                if (speakTimer <= 0f)
                {
                    state = State.WaitingForFood;
                    note = OrderBoard.Instance.Add(table.SeatLabel(seat), order);
                    note.guest = this;
                }
                break;

            case State.StandingUp:
                transform.position = Vector3.MoveTowards(transform.position, seat.approachPoint.position, sitSpeed * dt);
                if ((transform.position - seat.approachPoint.position).sqrMagnitude < 0.0001f)
                {
                    agent.enabled = true;
                    agent.Warp(seat.approachPoint.position);
                    if (leaving)
                    {
                        agent.SetDestination(exit.position);
                        state = State.Leaving;
                    }
                    else
                    {
                        agent.SetDestination(barSpot.position);
                        state = State.WalkingToBar;
                    }
                }
                break;

            case State.WalkingToBar:
                if (agent.isOnNavMesh && Arrived())
                {
                    barWait = 0f;
                    if (BartenderMinigame3D.Instance != null) drinkRequest = BartenderMinigame3D.Instance.PickRequest();
                    state = State.WaitingAtBar;
                }
                break;

            case State.WaitingAtBar:
                if (barPatience > 0f)
                {
                    barWait += dt;
                    if (barWait >= barPatience) GiveUpBar();
                }
                break;

            case State.Eating:
                eatTimer -= dt;
                if (eatTimer <= 0f)
                {
                    leaving = true;
                    state = State.StandingUp;
                }
                break;

            case State.Leaving:
                if (agent.isOnNavMesh && Arrived()) Destroy(gameObject);
                break;

            case State.ReturningToSeat:
                if (agent.isOnNavMesh && Arrived())
                {
                    agent.enabled = false;
                    state = State.Sitting;
                }
                break;
        }

        UpdateFacing();
    }

    void CountdownToBar(float dt)
    {
        if (barTrips >= maxBarTrips || BarCounter.Instance == null) return;

        barTimer -= dt;
        if (barTimer > 0f) return;

        if (BarCounter.Instance.TryReserve(this, out barSpot))
        {
            barTrips++;
            resumeState = state;
            state = State.StandingUp;
        }
        else
        {
            barTimer = 3f;
        }
    }

    bool IsSeatedState =>
        state == State.Sitting || state == State.WaitingForWaiter || state == State.Ordering ||
        state == State.WaitingForFood || state == State.StandingUp || state == State.Eating;

    void UpdateFacing()
    {
        if (sprite == null || Camera.main == null) return;

        Vector3 dir;
        if (agent.enabled && agent.velocity.sqrMagnitude > 0.01f) dir = agent.velocity;
        else if (state == State.WaitingAtBar && BarCounter.Instance != null && BarCounter.Instance.lookTarget != null)
            dir = BarCounter.Instance.lookTarget.position - transform.position;
        else if (IsSeatedState) dir = table.transform.position - transform.position;
        else return;

        float side = Vector3.Dot(dir, Camera.main.transform.right);
        if (Mathf.Abs(side) > 0.02f) sprite.flipX = side < 0f;
    }

    public void TakeOrder()
    {
        if (state != State.WaitingForWaiter) return;
        order = Order.Generate();
        speech = order.Speech();
        speakTimer = orderSpeakTime;
        state = State.Ordering;
    }

    static string DrinkList(List<DrinkData> list)
    {
        var names = new List<string>();
        foreach (var d in list) names.Add(d.drinkName.ToLower());
        if (names.Count == 1) return names[0];
        if (names.Count == 2) return names[0] + " and " + names[1];
        return string.Join(", ", names.GetRange(0, names.Count - 1)) + " and " + names[names.Count - 1];
    }

    void GiveUpBar()
    {
        BarCounter.Instance.Release(this);
        barSpot = null;
        reaction = "Too slow, I'm going back.";
        reactionTimer = 3f;
        agent.SetDestination(seat.approachPoint.position);
        state = State.ReturningToSeat;
    }

    void DrawBarBubble()
    {
        string text = drinkRequest != null && drinkRequest.Count > 0 ? "I'd like " + DrinkList(drinkRequest) + "." : "Drinks, please!";
        GuiDraw.Bubble(HeadPoint, text, 260f);

        Vector3 sp = Camera.main.WorldToScreenPoint(HeadPoint);
        if (sp.z <= 0f) return;
        var bar = new Rect(sp.x - 75f, Screen.height - sp.y + 6f, 150f, 10f);
        GuiDraw.Frame(bar);
        GuiDraw.Fill(new Rect(bar.x + 2f, bar.y + 2f, (bar.width - 4f) * PatienceLeft01, bar.height - 4f), Color.white);
    }

    public void Receive(Dish d)
    {
        if (state != State.WaitingForFood) return;

        satisfaction = d.quality;
        reaction = d.quality >= 0.8f ? "Delicious!" : d.quality >= 0.5f ? "It's fine." : "This isn't what I wanted...";
        reactionTimer = 3f;
        OrderBoard.Instance.Remove(note);
        eatTimer = eatTime;
        state = State.Eating;
    }

    public void ReceiveDrinks()
    {
        if (state != State.WaitingAtBar) return;
        drinkRequest = null;
        if (BarCounter.Instance != null) BarCounter.Instance.Release(this);
        barSpot = null;
        agent.SetDestination(seat.approachPoint.position);
        state = State.ReturningToSeat;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        if (reactionTimer > 0f && (PlayerMovement.SeesFoodUI || PlayerMovement.CurrentIs(PlayerMovement.Role.Bartender)))
        {
            GuiDraw.Bubble(HeadPoint, reaction);
            return;
        }

        if (state == State.WaitingForWaiter && PlayerMovement.SeesFoodUI)
            GuiDraw.Bubble(HeadPoint, "...", 60f);
        else if (state == State.Ordering && PlayerMovement.SeesFoodUI)
            GuiDraw.Bubble(HeadPoint, speech);
        else if (state == State.WaitingAtBar && PlayerMovement.CurrentIs(PlayerMovement.Role.Bartender) && !BartenderMinigame3D.Active)
            DrawBarBubble();
    }
}