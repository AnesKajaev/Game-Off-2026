using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    public float stepDistance = 0.6f;
    public float minInterval = 0.25f;

    Vector3 last;
    float travelled;
    float lastStep;

    void Start() => last = transform.position;

    void Update()
    {
        Vector3 p = transform.position;
        Vector3 d = p - last;
        d.y = 0f;
        last = p;

        if (d.magnitude > 1f)
        {
            travelled = 0f;
            return;
        }

        travelled += d.magnitude;
        if (travelled < stepDistance || Time.time - lastStep < minInterval) return;

        travelled = 0f;
        lastStep = Time.time;
        Sfx.Play(SurfaceZone.FootstepEventAt(p), p);
    }
}