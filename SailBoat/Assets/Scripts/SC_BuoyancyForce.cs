using UnityEngine;

public class BuoyancyForce : ForceGenerator
{
    [SerializeField] private float mWaterDensity = 1025f; // kg/m^3

    private Ocean mOcean;

    private void Start()
    {
        mOcean = FindAnyObjectByType<Ocean>();
    }

    public override void GenerateForce(Boat boat)
    {
        float pointArea = boat.GetPointArea();
        float hullHeight = boat.GetHullSize().y;
        float gravity = boat.GetConstantAcceleration().magnitude;

        foreach (Vector3 pos in boat.GetHullPoints())
        {
            float volume = Mathf.Min(mOcean.GetDepth(pos), hullHeight) * pointArea;
            boat.AddForceAtPoint(Vector3.up * (mWaterDensity * gravity * volume), pos);
        }
    }
}