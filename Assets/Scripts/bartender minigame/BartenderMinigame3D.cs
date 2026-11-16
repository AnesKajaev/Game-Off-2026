using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class BartenderMinigame3D : MonoBehaviour, ITaskCondition
{
    [Header("Stage")]
    public GameObject root;
    public Camera stageCamera;
    public Transform servePoint;
    public Transform queuePoint;
    public Transform exitPoint;
    public Transform bottle;
    public SpriteRenderer bottleRenderer;

    [Header("Drinks")]
    public DrinkData[] drinks;
    public Key switchKey = Key.Q;

    [Header("Liquid")]
    public LiquidMetaball liquid;
    public float dropSize = 0.08f;
    public PhysicsMaterial2D liquidPhysics;
    public int maxDrops = 350;
    public bool continuousCollision = false;
    public float sleepSpeed = 0.1f;

    [Header("Bottle control")]
    public Vector2 moveMin = new Vector2(-0.5f, 0.8f);
    public Vector2 moveMax = new Vector2(2.5f, 3f);
    public float followSmooth = 12f;
    public float tugAngle = 8f;
    public float returnSpeed = 30f;

    [Header("Shake / spill")]
    public float tugShake = 0.35f;
    public float shakeDecay = 1.2f;
    public float shakeWobble = 0.03f;
    public float shakeAngle = 3f;
    public float spillJitter = 1.2f;

    [Header("Orders & scoring")]
    public int minDrinks = 2;
    public int maxDrinks = 3;
    public float settleSpeed = 0.08f;
    public float settleHold = 0.6f;
    public float slideTime = 0.8f;
    public float maxTiltBonus = 2f;
    public int spillPenalty = 2;

    public UnityEvent<int> onCustomerServed;
    public UnityEvent<int> onShiftFinished;

    class DropInfo { public Rigidbody2D rb; public DrinkData drink; }
    enum State { Idle, Pouring, Serving, Waiting }

    readonly List<DropInfo> drops = new List<DropInfo>();
    Glass current, next;
    State state;
    int drinkIndex, spawnedGlasses, served, totalScore, spilledThisGlass, poured;
    float tilt, shake, settleTimer, flowInt, tiltInt, spawnAcc;
    bool isPouring;
    Vector3 bottleBase;
    string message = "";
    Guest customer;
    int orders;
    int customersServed;
    float waitCheck;

    void SetLayer(GameObject go)
    {
        int layer = LayerMask.NameToLayer("Bartender");
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }

    public bool CanStart => BarCounter.Instance != null && BarCounter.Instance.NextCustomer() != null;

    List<DrinkData> PickRequest()
    {
        var pool = new List<DrinkData>();
        for (int i = 0; drinks != null && i < drinks.Length; i++)
        {
            var d = drinks[i];
            if (d == null)
                Debug.LogWarning("BartenderMinigame3D: Drinks[" + i + "] is empty (no DrinkData assigned).", this);
            else if (d.glassPrefab == null)
                Debug.LogWarning("BartenderMinigame3D: Drinks[" + i + "] '" + d.name + "' has no usable Glass Prefab (empty, or the prefab lost its Glass script). Click to select the asset.", d);
            else
                pool.Add(d);
        }
        int count = Mathf.Min(Random.Range(minDrinks, maxDrinks + 1), pool.Count);
        var result = new List<DrinkData>();
        for (int n = 0; n < count; n++)
        {
            int i = Random.Range(0, pool.Count);
            result.Add(pool[i]);
            pool.RemoveAt(i);
        }
        return result;
    }

    public void Open()
    {
        if (!CanStart) return;
        Physics2D.linearSleepTolerance = sleepSpeed;
        root.SetActive(true);
        PlayerMovement.inputLocked = true;
        StartShift();
    }

    public void Close()
    {
        if (state != State.Idle) onShiftFinished.Invoke(totalScore);
        StopAllCoroutines();
        ClearAll();
        customer = null;
        state = State.Idle;
        PlayerPrefs.Save();
        root.SetActive(false);
        PlayerMovement.inputLocked = false;
    }

    void StartShift()
    {
        StopAllCoroutines();
        ClearAll();
        state = State.Idle;
        totalScore = 0;
        customersServed = 0;
        SelectDrink(0);
        bottleBase = servePoint.position + new Vector3(1.2f, 1.8f, 0f);
        StartCustomer();
    }

    void StartCustomer()
    {
        ClearDrops();
        customer = BarCounter.Instance != null ? BarCounter.Instance.NextCustomer() : null;
        if (customer == null)
        {
            EnterWaiting();
            return;
        }

        if (customer.drinkRequest == null || customer.drinkRequest.Count == 0) customer.drinkRequest = PickRequest();
        if (customer.drinkRequest.Count == 0)
        {
            Debug.LogError("BartenderMinigame3D on '" + name + "': no usable drinks (" + (drinks == null ? 0 : drinks.Length) + " entries in Drinks). Every entry needs a DrinkData with a Glass Prefab.", this);
            customer.drinkRequest = null;
            Close();
            return;
        }

        orders = customer.drinkRequest.Count;
        served = spawnedGlasses = 0;
        current = SpawnGlass(servePoint.position);
        next = orders > 1 ? SpawnGlass(queuePoint.position) : null;
        ResetGlassStats();
        state = State.Pouring;
    }

    void EnterWaiting()
    {
        customer = null;
        state = State.Waiting;
        waitCheck = 0f;
        message = "No guests at the bar. Esc to leave";
    }

    void WatchForGuests(float dt)
    {
        waitCheck += dt;
        if (waitCheck < 0.25f) return;
        waitCheck = 0f;
        if (BarCounter.Instance != null && BarCounter.Instance.NextCustomer() != null) StartCustomer();
    }

    void ClearDrops()
    {
        foreach (var di in drops) if (di.rb) Destroy(di.rb.gameObject);
        drops.Clear();
    }

    void ClearAll()
    {
        foreach (var di in drops) if (di.rb) Destroy(di.rb.gameObject);
        drops.Clear();
        if (current) Destroy(current.gameObject);
        if (next) Destroy(next.gameObject);
        current = next = null;
    }

    void ResetGlassStats()
    {
        poured = spilledThisGlass = 0;
        flowInt = tiltInt = settleTimer = spawnAcc = checkAcc = 0f;
        isPouring = false;
        message = "Mouse moves the bottle, scroll up to tilt";
    }

    Glass SpawnGlass(Vector3 pos)
    {
        var d = customer.drinkRequest[spawnedGlasses];
        var g = Instantiate(d.glassPrefab, pos, Quaternion.identity, root.transform);
        SetLayer(g.gameObject);
        g.drink = d;
        spawnedGlasses++;
        return g;
    }

    void SelectDrink(int i)
    {
        drinkIndex = i;
        tilt = 0f;
        spawnAcc = 0f;
        bottleRenderer.sprite = drinks[i].bottleSprite;
    }

    void Update()
    {
        if (!root.activeSelf) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (kb[Key.Escape].wasPressedThisFrame) { Close(); return; }
        if (state == State.Idle) return;

        float dt = Time.deltaTime;
        CleanupDrops();

        UpdateBottle(kb, mouse, dt);
        if (state == State.Waiting)
        {
            WatchForGuests(dt);
        }
        else if (state == State.Pouring)
        {
            if (customer == null)
            {
                ClearAll();
                EnterWaiting();
                return;
            }
            Pour(dt);
            CheckSettled(dt);
        }
    }

    void UpdateBottle(Keyboard kb, Mouse mouse, float dt)
    {
        if (kb[switchKey].wasPressedThisFrame && drinks.Length > 1)
            SelectDrink((drinkIndex + 1) % drinks.Length);
        var d = drinks[drinkIndex];

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll > 0.01f) { tilt += tugAngle; shake = Mathf.Min(1f, shake + tugShake); }
        else if (scroll < -0.01f) tilt -= tugAngle;

        tilt = Mathf.Clamp(tilt - returnSpeed * dt, 0f, d.maxTilt);
        shake = Mathf.Max(0f, shake - shakeDecay * dt);

        Ray ray = stageCamera.ScreenPointToRay(mouse.position.ReadValue());
        var plane = new Plane(Vector3.forward, servePoint.position);
        Vector3 c = servePoint.position;
        Vector3 target = bottleBase;
        if (plane.Raycast(ray, out float dist))
        {
            Vector3 m = ray.GetPoint(dist);
            target = new Vector3(
                Mathf.Clamp(m.x, c.x + moveMin.x, c.x + moveMax.x),
                Mathf.Clamp(m.y, c.y + moveMin.y, c.y + moveMax.y),
                c.z);
        }
        bottleBase = Vector3.Lerp(bottleBase, target, 1f - Mathf.Exp(-followSmooth * dt));

        Vector2 wobble = Random.insideUnitCircle * shake * shakeWobble;
        bottle.position = bottleBase + new Vector3(wobble.x, wobble.y, 0f);
        bottle.rotation = Quaternion.Euler(0f, 0f, tilt + Random.Range(-1f, 1f) * shake * shakeAngle);
    }

    void Pour(float dt)
    {
        var d = drinks[drinkIndex];
        float t = Mathf.InverseLerp(d.pourAngle, d.maxTilt, tilt);
        float flow = tilt > d.pourAngle ? Mathf.Lerp(d.minFlow, d.maxFlow, t) : 0f;

        isPouring = flow > 0f;
        if (!isPouring) { spawnAcc = 0f; return; }

        spawnAcc += flow * dt;
        flowInt += flow * dt;
        tiltInt += (tilt / d.maxTilt) * flow * dt;

        while (spawnAcc >= 1f)
        {
            spawnAcc -= 1f;
            if (drops.Count < maxDrops) SpawnDrop(d, t);
        }
    }

    void SpawnDrop(DrinkData d, float t)
    {
        Vector3 mouth = bottle.TransformPoint(d.mouthLocal);
        mouth.z = servePoint.position.z;

        var go = new GameObject("Drop");
        go.layer = LayerMask.NameToLayer("Bartender");
        go.transform.SetParent(root.transform, true);
        go.transform.position = mouth;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = dropSize / 2f;
        if (liquidPhysics) col.sharedMaterial = liquidPhysics;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.linearDamping = 0.2f;
        rb.collisionDetectionMode = continuousCollision ? CollisionDetectionMode2D.Continuous : CollisionDetectionMode2D.Discrete;

        liquid.CreateBlob(go.transform, d.liquidColor, dropSize);

        Vector2 dir = bottle.up;
        Vector2 noise = Random.insideUnitCircle * (0.05f + shake * spillJitter) * Mathf.Lerp(0.5f, 1.5f, t);
        rb.linearVelocity = dir.normalized * Mathf.Lerp(0.4f, 1.6f, t) + noise;

        drops.Add(new DropInfo { rb = rb, drink = d });
        poured++;
    }

    void CleanupDrops()
    {
        float killY = servePoint.position.y - 4f;
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            var di = drops[i];
            if (di.rb == null) { drops.RemoveAt(i); continue; }
            if (di.rb.position.y < killY)
            {
                Destroy(di.rb.gameObject);
                drops.RemoveAt(i);
                spilledThisGlass++;
                SpillLog.Add(di.drink.drinkName, 1);
            }
        }
    }

    readonly List<float> levelBuf = new List<float>();
    float checkAcc;
    const float checkInterval = 0.1f;

    float GlassLevel(Glass g)
    {
        levelBuf.Clear();
        foreach (var di in drops)
        {
            if (di.rb == null) continue;
            Vector3 local = g.transform.InverseTransformPoint(di.rb.position);
            if (g.Contains(local)) levelBuf.Add(local.y + dropSize / 2f);
        }
        if (levelBuf.Count == 0) return 0f;

        levelBuf.Sort();
        int take = Mathf.Max(1, levelBuf.Count / 10);
        float sum = 0f;
        for (int i = levelBuf.Count - take; i < levelBuf.Count; i++) sum += levelBuf[i];
        return sum / take;
    }

    void CheckSettled(float dt)
    {
        if (current == null) return;
        if (isPouring)
        {
            settleTimer = 0f;
            checkAcc = 0f;
            if (poured > 0) message = "Pouring...";
            return;
        }
        if (poured == 0) { settleTimer = 0f; return; }

        checkAcc += dt;
        if (checkAcc < checkInterval) return;
        float step = checkAcc;
        checkAcc = 0f;

        if (GlassLevel(current) < current.MinY)
        {
            settleTimer = 0f;
            message = "Below the line, keep pouring";
            return;
        }

        bool still = true;
        foreach (var di in drops)
        {
            Vector3 local = current.transform.InverseTransformPoint(di.rb.position);
            if (!current.InColumn(local)) continue;
            if (di.rb.linearVelocity.magnitude > settleSpeed) { still = false; break; }
        }

        settleTimer = still ? settleTimer + step : 0f;
        message = still ? "Hold steady..." : "Settling...";

        if (settleTimer >= settleHold)
        {
            state = State.Serving;
            StartCoroutine(ServeRoutine());
        }
    }

    IEnumerator ServeRoutine()
    {
        var g = current;

        var inside = new List<DropInfo>();
        foreach (var di in drops)
            if (g.Contains(g.transform.InverseTransformPoint(di.rb.position))) inside.Add(di);

        int correct = 0;
        var ys = new List<float>();
        foreach (var di in inside)
        {
            if (di.drink == g.drink) correct++;
            ys.Add(g.transform.InverseTransformPoint(di.rb.position).y + dropSize / 2f);
        }

        ys.Sort(); ys.Reverse();
        float level = 0f;
        if (ys.Count > 0)
        {
            int take = Mathf.Max(1, ys.Count / 10);
            for (int i = 0; i < take; i++) level += ys[i];
            level /= take;
        }

        float err = Mathf.Abs(level - g.TargetY);
        float accuracy = ys.Count == 0 ? 0f : Mathf.Clamp01(1f - err / (g.tolerance * 3f));
        float correctRatio = inside.Count > 0 ? correct / (float)inside.Count : 0f;
        float avgTilt = flowInt > 0f ? tiltInt / flowInt : 0f;
        float bonus = Mathf.Lerp(1f, maxTiltBonus, avgTilt);

        int gained = Mathf.Max(0,
            Mathf.RoundToInt(g.drink.points * accuracy * correctRatio * bonus) - spilledThisGlass * spillPenalty);
        totalScore += gained;

        if (correctRatio < 0.8f) message = "Wrong drink! +" + gained;
        else if (err <= g.tolerance) message = "Perfect! +" + gained;
        else message = (level < g.TargetY ? "Underfilled" : "Overfilled") + "  +" + gained;

        foreach (var di in inside)
        {
            di.rb.linearVelocity = Vector2.zero;
            di.rb.simulated = false;
            di.rb.transform.SetParent(g.transform, true);
            drops.Remove(di);
        }

        yield return new WaitForSeconds(0.8f);

        served++;
        var old = current;
        Coroutine slideOld = StartCoroutine(Slide(old.transform, exitPoint.position));

        if (served >= orders)
        {
            yield return slideOld;
            Destroy(old.gameObject);
            current = null;
            PlayerPrefs.Save();
            if (customer != null) { customer.ReceiveDrinks(); customer = null; }
            customersServed++;
            onCustomerServed.Invoke(totalScore);
            StartCustomer();
            yield break;
        }

        current = next;
        yield return StartCoroutine(Slide(current.transform, servePoint.position));

        if (spawnedGlasses < orders)
        {
            next = SpawnGlass(queuePoint.position + Vector3.right * 1.5f);
            StartCoroutine(Slide(next.transform, queuePoint.position));
        }
        else next = null;

        yield return slideOld;
        Destroy(old.gameObject);

        ResetGlassStats();
        state = State.Pouring;
    }

    IEnumerator Slide(Transform t, Vector3 to)
    {
        Vector3 from = t.position;
        for (float e = 0f; e < slideTime; e += Time.deltaTime)
        {
            t.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e / slideTime));
            yield return null;
        }
        t.position = to;
    }

    void OnGUI()
    {
        if (!root.activeSelf) return;

        var big = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.UpperCenter };
        big.normal.textColor = Color.white;
        GUI.Label(new Rect(0, 20, Screen.width, 50), message, big);

        var small = new GUIStyle(GUI.skin.label) { fontSize = 18 };
        small.normal.textColor = Color.white;
        string bottleName = drinks.Length > 0 ? drinks[drinkIndex].drinkName : "-";
        GUI.Label(new Rect(20, 20, 420, 100),
            "Bottle: " + bottleName + "  (" + switchKey + " to switch)\n" +
            "Score: " + totalScore + "   Guests served: " + customersServed + (customer != null ? "   Glass " + Mathf.Min(served + 1, orders) + "/" + orders : ""),
            small);
    }

    void OnDrawGizmosSelected()
    {
        if (!servePoint) return;
        Gizmos.color = Color.yellow;
        Vector3 c = servePoint.position;
        Vector3 a = c + new Vector3(moveMin.x, moveMin.y, 0f);
        Vector3 b = c + new Vector3(moveMax.x, moveMax.y, 0f);
        Gizmos.DrawWireCube((a + b) / 2f, new Vector3(b.x - a.x, b.y - a.y, 0.01f));
    }
}