using UnityEngine;

public class BuoyancyForce : ForceGenerator
{
    [SerializeField] private float mWaterDensity = 1025f; // kg/m^3

    private Water mWater;

    private void Start()
    {
        mWater = FindAnyObjectByType<Water>();
    }

    public override void GenerateForce(Boat boat)
    {
        float pointArea = boat.GetPointArea();
        float hullHeight = boat.GetHullSize().y;
        float gravity = boat.GetConstantAcceleration().magnitude;

        foreach (Vector3 pos in boat.GetHullPoints())
        {
            float volume = Mathf.Min(mWater.GetDepth(pos), hullHeight) * pointArea;
            boat.AddForceAtPoint(Vector3.up * (mWaterDensity * gravity * volume), pos);
        }
    }
}