using UnityEngine;

public class SailForce : ForceGenerator
{
    private WindManager mWindManager;

    [SerializeField, Range(0f, 1f)] private float mLiftCoefficient;
    [SerializeField, Range(0f, 1f)] private float mDragCoefficient;


    private void Start()
    {
        mWindManager = FindAnyObjectByType<WindManager>();
    }

    public override void GenerateForce(Boat attachedBoat)
    {
        Vector3 apparentWindVelocity = mWindManager.GetWind() - attachedBoat.GetLinearVelocity();

        Vector3 sailNormal = attachedBoat.GetSailNormal();

        Vector3 drag = apparentWindVelocity * mDragCoefficient;
        Vector3 lift = Vector3.Cross(apparentWindVelocity, Vector3.up) * mLiftCoefficient;

        Vector3 maxForce = lift + drag;

        // Idea1: 0 degrees between sail and wind directions (same direction) // cos(0) = 1
        // Max w/o stopping: 150 degrees between sail and wind directions // cos(150) = -0.87

        float forceScale = (maxForce.magnitude + 0.87f) / 1.87f;

        attachedBoat.AddForce(forceScale * maxForce);
    }
}
