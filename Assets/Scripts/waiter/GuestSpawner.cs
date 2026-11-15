using UnityEngine;

public class GuestSpawner : MonoBehaviour
{
    public Guest guestPrefab;
    public Transform spawnPoint;
    public Transform entrancePoint;
    public float spawnInterval = 6f;
    public float firstDelay = 2f;
    public Sprite[] looks;

    float timer;

    void Start() => timer = firstDelay;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (Table.FreeSeatCount() == 0) return;

        timer = spawnInterval;
        Spawn();
    }

    void Spawn()
    {
        if (guestPrefab == null || spawnPoint == null) return;
        if (!Table.TryPickFree(out Table table, out Seat seat)) return;

        Guest g = Instantiate(guestPrefab, spawnPoint.position, Quaternion.identity);
        if (looks != null && looks.Length > 0 && g.sprite != null)
            g.sprite.sprite = looks[Random.Range(0, looks.Length)];

        seat.Reserve(g);
        g.Init(table, seat, entrancePoint);
    }
}