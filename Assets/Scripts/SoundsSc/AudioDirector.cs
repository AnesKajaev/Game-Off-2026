using System.Reflection;
using UnityEngine;

public class AudioDirector : MonoBehaviour
{
    public int busyGuests = 5;
    public int quietGuests = 3;

    bool busy;
    SfxLoop dining;

#if FMOD_PRESENT
    FMODUnity.StudioListener listener;
    GameObject lastTarget;
    bool warned;
#endif

    void Start() => dining = Sfx.Loop(Sfx.DiningQuiet);

    void Update()
    {
        int n = Guest.All.Count;
        bool wantBusy = busy ? n > quietGuests : n >= busyGuests;
        if (wantBusy != busy)
        {
            busy = wantBusy;
            Sfx.StopLoop(ref dining);
            dining = Sfx.Loop(busy ? Sfx.DiningBusy : Sfx.DiningQuiet);
        }

#if FMOD_PRESENT
        if (listener == null) listener = FindFirstObjectByType<FMODUnity.StudioListener>();

        GameObject target = PlayerMovement.Current != null ? PlayerMovement.Current.gameObject : null;
        if (listener != null && target != lastTarget)
        {
            lastTarget = target;
            SetAttenuation(listener, target);
        }
#endif
    }

#if FMOD_PRESENT
    void SetAttenuation(FMODUnity.StudioListener l, GameObject target)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        System.Type type = l.GetType();

        foreach (string name in new[] { "attenuationObject", "AttenuationObject" })
        {
            FieldInfo field = type.GetField(name, flags);
            if (field != null && field.FieldType == typeof(GameObject))
            {
                field.SetValue(l, target);
                return;
            }

            PropertyInfo prop = type.GetProperty(name, flags);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(GameObject))
            {
                prop.SetValue(l, target);
                return;
            }
        }

        MethodInfo method = type.GetMethod("SetAttenuationObject", flags);
        if (method != null)
        {
            method.Invoke(l, new object[] { target });
            return;
        }

        if (!warned)
        {
            warned = true;
            Debug.LogWarning("This FMOD version has no attenuation object on the Studio Listener. Sounds will fade by distance from the camera.");
        }
    }
#endif

    void OnDestroy() => Sfx.StopLoop(ref dining);
}