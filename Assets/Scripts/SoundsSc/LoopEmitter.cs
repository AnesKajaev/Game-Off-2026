using UnityEngine;

public class LoopEmitter : MonoBehaviour
{
    [Tooltip("For example event:/Ambience/Fridge or event:/Ambience/DeliveryBay")]
    public string eventPath = Sfx.FridgeHum;
    public bool is3D = true;

    SfxLoop loop;

    void OnEnable() => loop = Sfx.Loop(eventPath, is3D ? transform : null);
    void OnDisable() => Sfx.StopLoop(ref loop);
}