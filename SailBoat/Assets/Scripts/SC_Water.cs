using UnityEngine;

public class Water : MonoBehaviour
{
    public float GetHeight(Vector3 worldPos)
    {
        return transform.position.y;
    }
    
    public float GetDepth(Vector3 worldPos)
    {
        return Mathf.Max(GetHeight(worldPos) - worldPos.y, 0f);
    }
}
