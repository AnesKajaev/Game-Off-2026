using UnityEngine;

public class PaperBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        Vector3 dir = Camera.main.transform.forward;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}