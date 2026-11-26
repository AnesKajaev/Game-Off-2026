using System.Collections.Generic;
using UnityEngine;

public class SfxLoop
{
#if FMOD_PRESENT
    public FMOD.Studio.EventInstance instance;
#endif
    public Transform follow;
}

#if FMOD_PRESENT
public class SfxRunner : MonoBehaviour
{
    static SfxRunner instance;
    readonly List<SfxLoop> loops = new List<SfxLoop>();

    public static void Track(SfxLoop loop)
    {
        if (instance == null)
        {
            var go = new GameObject("SfxRunner");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SfxRunner>();
        }
        instance.loops.Add(loop);
    }

    void LateUpdate()
    {
        for (int i = loops.Count - 1; i >= 0; i--)
        {
            SfxLoop l = loops[i];
            if (l.follow == null || !l.instance.isValid())
            {
                loops.RemoveAt(i);
                continue;
            }
            l.instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(l.follow));
        }
    }
}
#endif

public static class Sfx
{
    public const string UiHover = "event:/UI/Hover";
    public const string UiSelect = "event:/UI/Select";
    public const string UiBack = "event:/UI/Back";

    public const string FootCarpet = "event:/Footsteps/Carpet";
    public const string FootConcrete = "event:/Footsteps/Concrete";
    public const string FootTiles = "event:/Footsteps/Tiles";

    public const string DiningQuiet = "event:/Ambience/DiningQuiet";
    public const string DiningBusy = "event:/Ambience/DiningBusy";
    public const string DeliveryBayAmbience = "event:/Ambience/DeliveryBay";
    public const string FridgeHum = "event:/Ambience/Fridge";

    public const string BottleSwap = "event:/Bartender/Swap";
    public const string GlassSlide = "event:/Bartender/GlassSlide";
    public const string LiquidFill = "event:/Bartender/LiquidFill";

    public const string Frying = "event:/Kitchen/Frying";
    public const string MeatShape = "event:/Kitchen/MeatShape";
    public const string SaltShaker = "event:/Kitchen/SaltShaker";
    public const string KnifeChop = "event:/Kitchen/KnifeChop";
    public const string KnifeDice = "event:/Kitchen/KnifeDice";
    public const string StoveOn = "event:/Kitchen/StoveOn";
    public const string ThermostatBeep = "event:/Kitchen/ThermostatBeep";
    public const string Descale = "event:/Kitchen/Descale";

    public const string TruckMotor = "event:/Delivery/TruckMotor";
    public const string ContainerDoor = "event:/Delivery/ContainerDoor";
    public const string BackingBeeps = "event:/Delivery/BackingBeeps";
    public const string BoxLift = "event:/Delivery/BoxLift";

    public const string MusicDining = "event:/Music/DiningRoom";
    public const string MusicKitchen = "event:/Music/Kitchen";
    public const string MusicBar = "event:/Music/Bar";
    public const string MusicStorage = "event:/Music/Storage";

    static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    static readonly HashSet<string> warned = new HashSet<string>();

#if FMOD_PRESENT
    static bool TryCreate(string path, out FMOD.Studio.EventInstance inst)
    {
        inst = default(FMOD.Studio.EventInstance);
        try
        {
            FMOD.Studio.EventDescription desc = FMODUnity.RuntimeManager.GetEventDescription(path);
            desc.createInstance(out inst);
            return inst.isValid();
        }
        catch (System.Exception)
        {
            if (warned.Add(path)) Debug.LogWarning("FMOD event not found: " + path);
            return false;
        }
    }
#endif

    public static void Play(string path, Vector3? at = null)
    {
#if FMOD_PRESENT
        FMOD.Studio.EventInstance inst;
        if (!TryCreate(path, out inst)) return;
        if (at.HasValue) inst.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(at.Value));
        inst.start();
        inst.release();
#endif
    }

    public static void PlayLimited(string path, float minInterval, Vector3? at = null)
    {
        float last;
        if (lastPlayed.TryGetValue(path, out last) && Time.unscaledTime >= last && Time.unscaledTime - last < minInterval) return;
        lastPlayed[path] = Time.unscaledTime;
        Play(path, at);
    }

    public static SfxLoop Loop(string path, Transform follow = null, Vector3? at = null)
    {
#if FMOD_PRESENT
        FMOD.Studio.EventInstance inst;
        if (!TryCreate(path, out inst)) return null;

        var loop = new SfxLoop { instance = inst, follow = follow };
        if (follow != null)
        {
            inst.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(follow));
            SfxRunner.Track(loop);
        }
        else if (at.HasValue)
        {
            inst.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(at.Value));
        }

        inst.start();
        inst.release();
        return loop;
#else
        return null;
#endif
    }

    public static void StopLoop(ref SfxLoop loop, bool fade = true)
    {
#if FMOD_PRESENT
        if (loop != null) loop.instance.stop(fade ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
#endif
        loop = null;
    }

    public static void KeyOff(SfxLoop loop)
    {
#if FMOD_PRESENT
        if (loop != null) loop.instance.keyOff();
#endif
    }

    public static void Param(SfxLoop loop, string name, float value)
    {
#if FMOD_PRESENT
        if (loop != null) loop.instance.setParameterByName(name, value);
#endif
    }
}