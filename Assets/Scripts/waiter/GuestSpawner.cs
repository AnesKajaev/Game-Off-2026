using UnityEngine;
using UnityEngine.AI;

public class GuestSpawner : MonoBehaviour
{
    public Guest guestPrefab;
    public Transform spawnPoint;
    public Transform entrancePoint;
    public Transform exitPoint;
    public float spawnInterval = 6f;
    public float firstDelay = 2f;
    public Sprite[] looks;

    [Header("Pairs")]
    [Range(0f, 1f)] public float pairChance = 0.4f;
    public float pairSpacing = 0.25f;

    float timer;

    void Start() => timer = firstDelay;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        if (Table.EmptyTableCount() == 0) return;

        timer = spawnInterval;

        int size = Random.value < pairChance && Table.EmptyTableCount(2) > 0 ? 2 : 1;
        Table table = Table.PickEmpty(size);
        for (int k = 0; k < size; k++)
            SpawnGuest(k, table);
    }

    void SpawnGuest(int index, Table table)
    {
        Seat seat;
        table.TryPickFreeSeat(out seat);

        Vector3 pos = spawnPoint.position + Vector3.right * pairSpacing * index;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pos, out hit, 1f, NavMesh.AllAreas)) pos = hit.position;

        Guest g = Instantiate(guestPrefab, pos, Quaternion.identity);
        if (looks != null && looks.Length > 0) g.sprite.sprite = looks[Random.Range(0, looks.Length)];

        seat.Reserve(g);
        g.Init(table, seat, entrancePoint, exitPoint != null ? exitPoint : spawnPoint);
    }
}