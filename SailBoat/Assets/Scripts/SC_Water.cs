using UnityEngine;

public class Water : MonoBehaviour
{
    public float GetHeight(Vector3 worldPos)
    {
        return transform.position.y;
    }
}
