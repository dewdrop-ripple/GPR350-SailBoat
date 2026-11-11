using UnityEngine;

public class SailForce : ForceGenerator
{
    private WindManager mWindManager;

    [SerializeField, Range(0f, 1f)] private float mLiftCoefficient;
    [SerializeField, Range(0f, 1f)] private float mDragCoefficient;
    [SerializeField, Range(0f, 1f)] private float mKeelResistance;

    private Boat mAttachedBoat = null;

    private Vector3 mOldWind;
    private Vector3 mCurrentForce = Vector3.zero;

    [SerializeField] private bool mDebug;


    private void Start()
    {
        mWindManager = FindAnyObjectByType<WindManager>();
        mOldWind = mWindManager.GetWind();
    }

    private void Update()
    {
        // Only recalculate if the wind changed
        if (mDebug || mOldWind != mWindManager.GetWind())
        {
            mOldWind = mWindManager.GetWind();
            CalculateForce();
        }
    }

    private void CalculateForce()
    {
        if (mAttachedBoat == null) { return; }

        Vector3 apparentWindVelocity = mWindManager.GetWind() - mAttachedBoat.GetLinearVelocity(); // Velocity of the wind relative to the ship

        Vector3 drag = apparentWindVelocity * mDragCoefficient;
        Vector3 lift = Vector3.Cross(apparentWindVelocity, Vector3.up) * mLiftCoefficient;

        Vector3 totalForce = lift + drag;

        // Idea1: 0 degrees between sail and wind directions (same direction) // cos(0) = 1
        // Max w/o stopping: 150 degrees between sail and wind directions // cos(150) = -0.87

        float forceScale = (Vector3.Dot(totalForce.normalized, mAttachedBoat.GetSailNormal()) + 0.87f) / 1.87f; // % of the actual force based on angle of the sail

        Vector3 actualForce = totalForce * forceScale; // Actual force of the wind on the boat

        // To get the actual amount of force being applied in both directions, 
        // Project onto the direction the boat is facing and perpendicular vector

        Vector3 boatDirection = mAttachedBoat.GetForwardVector();
        Vector3 perpDirection = Vector3.Cross(boatDirection, Vector3.Cross(boatDirection, actualForce));

        Vector3 forwardMovement = Vector3.Project(actualForce, boatDirection) * 2;
        Vector3 sideMovement = Vector3.Project(actualForce, forwardMovement) * mKeelResistance;

        mCurrentForce = forwardMovement + sideMovement;

        if (mDebug)
        {
            // Wind Velocity
            Debug.DrawLine(mAttachedBoat.transform.position + Vector3.up + mWindManager.GetWind(), mAttachedBoat.transform.position + Vector3.up + (mWindManager.GetWind() * 2), Color.antiqueWhite);

            // Boat Velocity
            Debug.DrawLine(mAttachedBoat.transform.position + Vector3.up + mAttachedBoat.GetLinearVelocity(), mAttachedBoat.transform.position + Vector3.up + (mAttachedBoat.GetLinearVelocity() * 2), Color.red);

            // Apparent Wind Velocity
            Debug.DrawLine(mAttachedBoat.transform.position + Vector3.up + apparentWindVelocity, mAttachedBoat.transform.position + Vector3.up + (apparentWindVelocity * 2), Color.white);

            // Drag
            Debug.DrawLine(mAttachedBoat.transform.position + (Vector3.up * 3), mAttachedBoat.transform.position + (Vector3.up * 3) + drag, Color.blue);

            // Lift
            Debug.DrawLine(mAttachedBoat.transform.position + (Vector3.up * 3), mAttachedBoat.transform.position + (Vector3.up * 3) + lift, Color.blue);

            // Total Force
            Debug.DrawLine(mAttachedBoat.transform.position + (Vector3.up * 3), mAttachedBoat.transform.position + (Vector3.up * 3) + totalForce, Color.aliceBlue);

            // Sail Normal
            Debug.DrawLine(mAttachedBoat.transform.position + (Vector3.up * 5), mAttachedBoat.transform.position + mAttachedBoat.GetSailNormal() + (Vector3.up * 5), Color.purple);

            // Boat Direction and Perp Direction
            Debug.DrawLine(mAttachedBoat.transform.position + (Vector3.up * 2) + boatDirection, mAttachedBoat.transform.position + (Vector3.up * 2) + (boatDirection * 2), Color.pink);
            Debug.DrawLine(mAttachedBoat.transform.position + (Vector3.up * 2) + perpDirection, mAttachedBoat.transform.position + (Vector3.up * 2) + (perpDirection * 2), Color.pink);

            // Final Force
            Debug.DrawLine(mAttachedBoat.transform.position + (Vector3.up * 2) + mCurrentForce, mAttachedBoat.transform.position + (Vector3.up * 2) + (mCurrentForce * 2), Color.black);
        }
    }

    public override void GenerateForce(Boat attachedBoat)
    {
        if (mAttachedBoat != attachedBoat)
        {
            mAttachedBoat = attachedBoat;

            CalculateForce();
        }

        attachedBoat.AddForce(mCurrentForce);
    }
}
