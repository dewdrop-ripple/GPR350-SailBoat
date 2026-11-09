using System.Collections.Generic;
using UnityEngine;

public class Boat : MonoBehaviour
{
    private Vector3 mLinearVelocity = Vector3.zero;
    private Vector3 mAngularVelocity = Vector3.zero;
    private Vector3 mLinearAcceleration = Vector3.zero;
    private Vector3 mAngularAcceleration = Vector3.zero;

    [SerializeField] private Vector3 mContantAcceleration;

    private Vector3 mForceAccum = Vector3.zero;
    private Vector3 mTorqueAccum = Vector3.zero;

    [SerializeField] private float mMass;
    [SerializeField] private float mLinearDamping;
    [SerializeField] private float mAngularDamping;
    [SerializeField] private GameObject mCenterOfMass;

    [SerializeField] private GameObject mBase;
    [SerializeField] private GameObject mSail;

    private Collider mCollider;

    [SerializeField] private List<ForceGenerator> mForces;

    private WindManager mWindManager;


    // ----------------------------
    // BASIC DATA

    private void Start()
    {
        mCollider = GetComponent<Collider>();
        mWindManager = FindAnyObjectByType<WindManager>();
    }

    private void FixedUpdate()
    {
        
    }


    // ----------------------------
    // MOVEMENT

    private void HandlePlayerInput()
    {

    }



    // ----------------------------
    // MATH

    private void Integrate(float dt)
    {

    }

    public void AddForce(Vector3 force)
    {

    }

    public void AddTorque(Vector3 force)
    {

    }

    public void AddForceAtPoint(Vector3 force, Vector3 localPoint)
    {

    }

    private void ClearAccumulators()
    {

    }


    // ----------------------------
    // UTILITY

    public Vector3 GetLinearVelocity() { return mLinearVelocity; }
    public Vector3 GetAngularVelocity() { return mAngularVelocity; }
    public Vector3 GetLinearAcceleration() { return mLinearAcceleration; }
    public Vector3 GetAngularAcceleration() { return mAngularAcceleration; }
    public Vector3 GetConstantAcceleration() { return mContantAcceleration; }

    public float GetMass() { return mMass; }

    public float GetLinearDamping() { return mLinearDamping; }
    public float GetAngularDamping() { return mAngularDamping; }

    public Vector3 GetCenterOfMass() { return mCenterOfMass.transform.position; }

    public Quaternion GetBaseRotation() { return mBase.transform.rotation; }
    public Quaternion GetSailRoatation() { return mSail.transform.rotation; }

    public Matrix4x4 GetInverseInertiaTensorWorld()
    {
        return Matrix4x4.zero;
    }

    public float GetVolume() { return mCollider.transform.lossyScale.x * mCollider.transform.lossyScale.y * mCollider.transform.lossyScale.z; }
}
