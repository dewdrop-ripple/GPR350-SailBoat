using UnityEngine;

public class WindManager : MonoBehaviour
{
    [SerializeField] private Vector3 mWind;

    [SerializeField] private ParticleSystem mParticleSystem;


    private void Update()
    {
    }

    public Vector3 GetWind() { return mWind; }
}
