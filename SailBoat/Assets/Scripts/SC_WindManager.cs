using UnityEngine;

public class WindManager : MonoBehaviour
{
    [SerializeField] private Vector3 mWind;

    public Vector3 GetWind() { return mWind; }
}
