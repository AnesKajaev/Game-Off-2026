using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class BartenderMinigame3D : MonoBehaviour
{
    [Header("Stage")]
    public GameObject root;
    public Camera stageCamera;
    public Transform servePoint;      // where the active glass sits (base center)
    public Transform queuePoint;      // next glass waits here (to the right)
    public Transform exitPoint;       // finished glasses slide here (to the left)
    public Transform bottle;          // bottle object, sprite pivot at its center
    public SpriteRenderer bottleRenderer;

    [Header("Drinks")]
    public DrinkData[] drinks;
    public Key switchKey = Key.Q;

    [Header("Liquid")]
    public LiquidMetaball liquid;     // on the stage camera: draws the drops as one smooth liquid
    public float dropSize = 0.08f;    // physics size of one drop
    public PhysicsMaterial2D liquidPhysics;
    public int maxDrops = 350;             // fewer drops = faster. Raise dropSize instead of adding more
    public bool continuousCollision = false;   // turn on only if drops tunnel through the glass
    public float sleepSpeed = 0.1f;        // drops slower than this fall asleep (big saving for piles)

    [Header("Bottle control")]
    public Vector2 moveMin = new Vector2(-0.5f, 0.8f);   // limits, relative to servePoint
    public Vector2 moveMax = new Vector2(2.5f, 3f);
    public float followSmooth = 12f;
    public float tugAngle = 8f;        // degrees per wheel tick
    public float returnSpeed = 30f;    // degrees/sec it falls back on its own

    [Header("Shake / spill")]
    public float tugShake = 0.35f;
    public float shakeDecay = 1.2f;
    public float shakeWobble = 0.03f;
    public float shakeAngle = 3f;
    public float spillJitter = 1.2f;

    [Header("Orders & scoring")]
    public int ordersPerShift = 5;
    public float settleSpeed = 0.08f;
    public float settleHold = 0.6f;
    public float slideTime = 0.8f;
    public float maxTiltBonus = 2f;    // score multiplier at full tilt
    public int spillPenalty = 2;       // points lost per spilled drop

    public UnityEvent<int> onShiftFinished;   // passes the total score

    class DropInfo { public Rigidbody2D rb; public DrinkData drink; }
    enum State { Pouring, Serving, Done }

    readonly List<DropInfo> drops = new List<DropInfo>();
    Glass current, next;
    State state;
    int drinkIndex, spawnedGlasses, served, totalScore, spilledThisGlass, poured;
    float tilt, shake, settleTimer, flowInt, tiltInt, spawnAcc;
    bool isPouring;
    Vector3 bottleBase;
    string message = "";

    // ---------- layers ----------
    void SetLayer(GameObject go)
    {
        int layer = LayerMask.NameToLayer("Bartender");
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }

    // ---------- open / close ----------
    public void Open()
    {
        Physics2D.linearSleepTolerance = sleepSpeed;
        root.SetActive(true);
        PlayerMovement.inputLocked = true;
        StartShift();
    }

    public void Close()
    {
        StopAllCoroutines();
        ClearAll();
        PlayerPrefs.Save();
        root.SetActive(false);
        PlayerMovement.inputLocked = false;
    }

    void StartShift()
    {
        StopAllCoroutines();
        ClearAll();
        served = spawnedGlasses = totalScore = 0;
        SelectDrink(0);
        bottleBase = servePoint.position + new Vector3(1.2f, 1.8f, 0f);

        current = SpawnGlass(servePoint.position);
        next = ordersPerShift > 1 ? SpawnGlass(queuePoint.position) : null;
        ResetGlassStats();
        state = State.Pouring;
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
        var d = drinks[Random.Range(0, drinks.Length)];
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

    // ---------- update ----------
    void Update()
    {
        if (!root.activeSelf) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (kb[Key.Escape].wasPressedThisFrame) { Close(); return; }

        float dt = Time.deltaTime;
        CleanupDrops();

        if (state != State.Done) UpdateBottle(kb, mouse, dt);
        if (state == State.Pouring)
        {
            Pour(dt);
            CheckSettled(dt);
        }
    }

    void UpdateBottle(Keyboard kb, Mouse mouse, float dt)
    {
        if (kb[switchKey].wasPressedThisFrame && drinks.Length > 1)
            SelectDrink((drinkIndex + 1) % drinks.Length);
        var d = drinks[drinkIndex];

        // Wheel: up = tug the tilt up (and shake it), down = tilt back
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll > 0.01f) { tilt += tugAngle; shake = Mathf.Min(1f, shake + tugShake); }
        else if (scroll < -0.01f) tilt -= tugAngle;

        tilt = Mathf.Clamp(tilt - returnSpeed * dt, 0f, d.maxTilt);   // falls back on its own
        shake = Mathf.Max(0f, shake - shakeDecay * dt);

        // Mouse position on the stage plane, clamped to the limits
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

        // The visible part is a soft blob that the LiquidMetaball shader merges with its neighbours
        liquid.CreateBlob(go.transform, d.liquidColor, dropSize);

        // Stream leaves along the bottle's neck; shake and high flow add splash
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
                SpillLog.Add(di.drink.drinkName, 1);   // shows up on the night shift
            }
        }
    }

    readonly List<float> levelBuf = new List<float>();
    float checkAcc;
    const float checkInterval = 0.1f;   // the calm/level check runs this often, not every frame

    // Level of the liquid in a glass = average height of its top 10% of drops
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

    // Serve only when: not pouring, liquid above the minimum line, and the liquid is calm
    void CheckSettled(float dt)
    {
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

        // Level = average of the top 10% of drops
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

        // Freeze the liquid into the glass so it slides away with it
        foreach (var di in inside)
        {
            di.rb.linearVelocity = Vector2.zero;
            di.rb.simulated = false;   // out of the physics world, still rides along with the glass
            di.rb.transform.SetParent(g.transform, true);
            drops.Remove(di);
        }

        yield return new WaitForSeconds(0.8f);

        served++;
        var old = current;
        Coroutine slideOld = StartCoroutine(Slide(old.transform, exitPoint.position));

        if (served >= ordersPerShift)
        {
            yield return slideOld;
            Destroy(old.gameObject);
            current = null;
            state = State.Done;
            message = "Shift over! Score: " + totalScore + "   (Esc to close)";
            PlayerPrefs.Save();
            onShiftFinished.Invoke(totalScore);
            yield break;
        }

        current = next;
        yield return StartCoroutine(Slide(current.transform, servePoint.position));

        if (spawnedGlasses < ordersPerShift)
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

    // ---------- UI ----------
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
            "Score: " + totalScore + "   Glass " + Mathf.Min(served + 1, ordersPerShift) + "/" + ordersPerShift,
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