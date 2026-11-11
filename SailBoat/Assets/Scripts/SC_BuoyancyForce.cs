using UnityEngine;

public class BuoyancyForce : ForceGenerator
{
    [SerializeField] private int mPointsAcross = 2;
    [SerializeField] private int mPointsAlong = 4;
    [SerializeField] private float mWaterDensity = 1025f; // kg/m^3
    [SerializeField] private float mDamping = 1025;

    private Water mWater;

    private void Start()
    {
        mWater = FindAnyObjectByType<Water>();
    }

    public override void GenerateForce(Boat boat)
    {
        BoxCollider hull = boat.GetHullCollider();
        Vector3 size = Vector3.Scale(hull.size, hull.transform.lossyScale);
        float pointArea = size.x * size.z / (mPointsAcross * mPointsAlong);
        float gravity = boat.GetConstantAcceleration().magnitude;

        for (int x = 0; x < mPointsAcross; x++)
        {
            for (int z = 0; z < mPointsAlong; z++)
            {
                Vector3 cell = new Vector3((x + 0.5f) / mPointsAcross - 0.5f, -0.5f, (z + 0.5f) / mPointsAlong - 0.5f);
                Vector3 pos = hull.transform.TransformPoint(hull.center + Vector3.Scale(cell, hull.size));

                float depth = Mathf.Clamp(mWater.GetHeight(pos) - pos.y, 0f, size.y);
                float volume = depth * pointArea;

                float buoyancy = mWaterDensity * gravity * volume;
                float damping = mDamping * volume * boat.GetPointVelocity(pos).y;

                boat.AddForceAtPoint(Vector3.up * (buoyancy - damping), pos);
            }
        }
    }
}