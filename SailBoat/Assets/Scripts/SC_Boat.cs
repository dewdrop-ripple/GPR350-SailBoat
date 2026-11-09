using System.Collections.Generic;
using UnityEngine;

public class Boat : MonoBehaviour
{
    [SerializeField] private GameObject mBase;
    [SerializeField] private GameObject mSail;

    private Collider mCollider;
    private Rigidbody mRigidbody;

    [SerializeField] private List<ForceGenerator> mForces;

    private WindManager mWindManager;


    // ----------------------------
    // BASIC DATA

    private void Start()
    {
        mCollider = GetComponent<Collider>();
        mRigidbody = GetComponent<Rigidbody>();
        mWindManager = FindAnyObjectByType<WindManager>();
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < mForces.Count; i++)
        {
            mForces[i].GenerateForce(this);
        }
    }


    // ----------------------------
    // MOVEMENT

    private void HandlePlayerInput()
    {

    }



    // ----------------------------
    // MATH

    public void AddForce(Vector3 force)
    {
        mRigidbody.AddForce(force); 
    }

    public void AddTorque(Vector3 force)
    {
        mRigidbody.AddTorque(force);
    }

    public void AddForceAtPoint(Vector3 force, Vector3 localPoint)
    {
        mRigidbody.AddForceAtPosition(force, localPoint);
    }

    // ----------------------------
    // UTILITY

    public Vector3 GetLinearVelocity() { return mRigidbody.linearVelocity; }
    public Vector3 GetAngularVelocity() { return mRigidbody.angularVelocity; }

    public Vector3 GetConstantAcceleration() { return mRigidbody.gr}

    public float GetMass() { return mRigidbody.mass; }

    public float GetLinearDamping() { return mRigidbody.linearDamping; }
    public float GetAngularDamping() { return mRigidbody.angularDamping; }

    public Quaternion GetBaseRotation() { return mBase.transform.rotation; }
    public Quaternion GetSailRoatation() { return mSail.transform.rotation; }

    public Vector3 GetInertiaTensor() { return mRigidbody.inertiaTensor; }

    public float GetVolume() { return mCollider.transform.lossyScale.x * mCollider.transform.lossyScale.y * mCollider.transform.lossyScale.z; }
}
