using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher : MonoBehaviour
{
    public PlayerMovement[] characters;
    public CameraFollow cameraFollow;
    public int startIndex = 0;
    public Key switchKey = Key.Q;

    int current;

    void Start()
    {
        if (characters.Length == 0) return;
        Select(Mathf.Clamp(startIndex, 0, characters.Length - 1));
    }

    void Update()
    {
        if (PlayerMovement.inputLocked) return;
        if (characters.Length == 0 || Keyboard.current == null) return;

        if (Keyboard.current[switchKey].wasPressedThisFrame)
            Select((current + 1) % characters.Length);
    }

    void Select(int index)
    {
        current = index;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null)
                characters[i].isControlled = (i == current);
        }

        if (cameraFollow != null && characters[current] != null)
            cameraFollow.target = characters[current].transform;
    }
}