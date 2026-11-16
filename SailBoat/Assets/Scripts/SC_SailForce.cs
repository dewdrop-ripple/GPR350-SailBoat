using UnityEngine;

public class SailForce : ForceGenerator
{
    private WindManager mWindManager;

    [SerializeField, Range(0f, 1f)] private float mLiftCoefficient;
    [SerializeField, Range(0f, 1f)] private float mDragCoefficient;
    [SerializeField, Range(0f, 1f)] private float mKeelResistance;

    [SerializeField] private bool mDebug;
    [SerializeField] private Vector3 mDebugForce; 


    private void Start()
    {
        mWindManager = FindAnyObjectByType<WindManager>();
    }

    public override void GenerateForce(Boat attachedBoat)
    {
        Vector3 apparentWindVelocity = (mWindManager.GetWind() - attachedBoat.GetLinearVelocity()) * (attachedBoat.GetMass() / 25); // Velocity of the wind relative to the ship

        // Normal vector of the sail moving in the same direction as the boat
        Vector3 sailNormal = attachedBoat.GetSailNormal();
        if (Vector3.Dot(sailNormal, attachedBoat.GetForwardVector()) < 0f) { sailNormal *= -1; }

        // Lift in the same direction as the sail normal
        Vector3 lift = Vector3.Cross(apparentWindVelocity, Vector3.up) * mLiftCoefficient;
        if (Vector3.Dot(sailNormal, lift) < 0f) { lift *= -1; }

        Vector3 drag = apparentWindVelocity * mDragCoefficient;
        Vector3 totalForce = lift + drag;

        // To get the actual amount of force being applied in both directions, 
        // Project onto the direction the boat is facing and perpendicular vector

        Vector3 boatDirection = attachedBoat.GetForwardVector();
        Vector3 perpDirection = Vector3.Cross(boatDirection, Vector3.Cross(boatDirection, totalForce));

        Vector3 forwardMovement = Vector3.Project(totalForce, boatDirection);
        Vector3 sideMovement = Vector3.Project(totalForce, perpDirection) * mKeelResistance;

        // Make sure the boat doesn't slide backwards all the time
        if (Vector3.Dot(forwardMovement, attachedBoat.GetForwardVector()) < 0f) { forwardMovement *= -2; }
        else { forwardMovement *= 4; }

        Vector3 finalForce = forwardMovement + sideMovement;

        // Adjust to make sure the boat can't sail straight into the wind

        // Idea1: 0 degrees between sail and wind directions (same direction) // cos(0) = 1
        // Max w/o stopping: 150 degrees between sail and wind directions // cos(150) = -0.87

        float forceScale = (Vector3.Dot(totalForce.normalized, sailNormal) + 0.87f) / 1.87f; // % of the actual force based on angle of the sail

        finalForce *= forceScale;

        if (mDebug)
        {
            // Wind Velocity
            Debug.DrawLine(attachedBoat.transform.position + Vector3.up + mWindManager.GetWind(), attachedBoat.transform.position + Vector3.up + (mWindManager.GetWind() * 2), Color.antiqueWhite);

            // Boat Velocity
            Debug.DrawLine(attachedBoat.transform.position + Vector3.up + attachedBoat.GetLinearVelocity(), attachedBoat.transform.position + Vector3.up + (attachedBoat.GetLinearVelocity() * 2), Color.red);

            // Apparent Wind Velocity
            Debug.DrawLine(attachedBoat.transform.position + Vector3.up + apparentWindVelocity, attachedBoat.transform.position + Vector3.up + (apparentWindVelocity * 2), Color.white);

            // Drag
            Debug.DrawLine(attachedBoat.transform.position + (Vector3.up * 3), attachedBoat.transform.position + (Vector3.up * 3) + drag, Color.blue);

            // Lift
            Debug.DrawLine(attachedBoat.transform.position + (Vector3.up * 3), attachedBoat.transform.position + (Vector3.up * 3) + lift, Color.blue);

            // Total Force
            Debug.DrawLine(attachedBoat.transform.position + (Vector3.up * 3), attachedBoat.transform.position + (Vector3.up * 3) + totalForce, Color.aliceBlue);

            // Sail Normal
            Debug.DrawLine(attachedBoat.transform.position + (Vector3.up * 5), attachedBoat.transform.position + attachedBoat.GetSailNormal() + (Vector3.up * 5), Color.purple);

            // Boat Direction and Perp Direction
            Debug.DrawLine(attachedBoat.transform.position + (Vector3.up * 2) + boatDirection, attachedBoat.transform.position + (Vector3.up * 2) + (boatDirection * 2), Color.pink);
            Debug.DrawLine(attachedBoat.transform.position + (Vector3.up * 2) + perpDirection, attachedBoat.transform.position + (Vector3.up * 2) + (perpDirection * 2), Color.pink);

            // Final Force
            Debug.DrawLine(attachedBoat.transform.position + (Vector3.up * 2) + finalForce, attachedBoat.transform.position + (Vector3.up * 2) + (finalForce * 2), Color.black);
        }

        attachedBoat.AddForce(finalForce);

        mDebugForce = finalForce;
    }
}
