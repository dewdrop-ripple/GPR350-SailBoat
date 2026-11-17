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

            // against the moving water, not the ground
            Vector3 relativeVelocity = boat.GetPointVelocity(pos) - mWater.GetVelocity(pos);

            // vertical drag acts straight up, or a tilted hull turns it into a sideways push
            Vector3 forward = Vector3.ProjectOnPlane(hull.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(hull.right, Vector3.up).normalized;

            Vector3 drag = -forward * (Vector3.Dot(relativeVelocity, forward) * mDrag.z)
                           - right * (Vector3.Dot(relativeVelocity, right) * mDrag.x)
                           - Vector3.up * (relativeVelocity.y * mDrag.y);

            boat.AddForceAtPoint(drag * volume, pos);
        }
    }
}