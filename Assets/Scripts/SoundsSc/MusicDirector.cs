using UnityEngine;

public class MusicDirector : MonoBehaviour
{
    public float switchDelay = 0.6f;

    SurfaceZone.Area current = SurfaceZone.Area.None;
    SurfaceZone.Area candidate = SurfaceZone.Area.None;
    float candidateTime;
    SfxLoop music;

    static string PathFor(SurfaceZone.Area a)
    {
        switch (a)
        {
            case SurfaceZone.Area.Kitchen: return Sfx.MusicKitchen;
            case SurfaceZone.Area.Bar: return Sfx.MusicBar;
            case SurfaceZone.Area.Storage: return Sfx.MusicStorage;
            default: return Sfx.MusicDining;
        }
    }

    void Update()
    {
        PlayerMovement who = PlayerMovement.Current;
        if (who == null) return;

        SurfaceZone.Area a = SurfaceZone.AreaAt(who.transform.position);
        if (a != candidate)
        {
            candidate = a;
            candidateTime = 0f;
        }
        candidateTime += Time.deltaTime;

        if (candidate == current) return;
        if (current != SurfaceZone.Area.None && candidateTime < switchDelay) return;

        current = candidate;
        Sfx.StopLoop(ref music);
        music = Sfx.Loop(PathFor(current));
    }

    void OnDestroy() => Sfx.StopLoop(ref music);
}