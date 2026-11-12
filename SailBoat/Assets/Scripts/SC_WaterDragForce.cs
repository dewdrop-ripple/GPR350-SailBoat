using UnityEngine;

public class WaterDragForce : ForceGenerator
{
    [SerializeField] private Vector3 mDrag = new Vector3(500f, 3000f, 50f); // sideways, vertical, forward: per m^3 submerged, per m/s

    private Water mWater;

    private void Start()
    {
        mWater = FindAnyObjectByType<Water>();
    }

    public override void GenerateForce(Boat boat)
    {
        Transform hull = boat.GetHullCollider().transform;
        float pointArea = boat.GetPointArea();
        float hullHeight = boat.GetHullSize().y;

        foreach (Vector3 pos in boat.GetHullPoints())
        {
            float volume = Mathf.Min(mWater.GetDepth(pos), hullHeight) * pointArea;

            // Resist motion along each hull axis by its own strength
            Vector3 localVelocity = hull.InverseTransformDirection(boat.GetPointVelocity(pos));
            Vector3 drag = hull.TransformDirection(-Vector3.Scale(localVelocity, mDrag) * volume);

            boat.AddForceAtPoint(drag, pos);
        }
    }
}