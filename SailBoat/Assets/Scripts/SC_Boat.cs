using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class Boat : MonoBehaviour
{
    [SerializeField] private GameObject mBase, mSail;

    private BoxCollider mCollider;
    private Rigidbody mRigidbody;

    [SerializeField] private int mPointsAcross = 2;
    [SerializeField] private int mPointsAlong = 4;
    private Vector3[] mHullPoints;

    [SerializeField] private List<ForceGenerator> mForces;

    // Input
    private InputAction mBoatCW, mBoatCCW, mSailCW, mSailCCW;
    private InputAction mLookAction;

    [Range(0f, 1f)][SerializeField] private float mLookSensitivity = .05f;
    [Range(0f, 90f)][SerializeField] private float mPitchLimit = 88f;

    private float mPitch = 90;
    [SerializeField] private bool mInvertY = false;

    [SerializeField] private Camera mPlayerCamera;
    [SerializeField] private GameObject mPlayer;

    [SerializeField] private float mBoatTurnSpeed = .05f;


    // ----------------------------
    // BASIC DATA

    private void Start()
    {
        mCollider = GetComponentInChildren<BoxCollider>();
        mRigidbody = GetComponent<Rigidbody>();

        mHullPoints = new Vector3[mPointsAcross * mPointsAlong];

        mBoatCW = InputSystem.actions.FindAction("RotateBoatCW", throwIfNotFound: true);
        mBoatCCW = InputSystem.actions.FindAction("RotateBoatCCW", throwIfNotFound: true);
        mSailCW = InputSystem.actions.FindAction("RotateSailCW", throwIfNotFound: true);
        mSailCCW = InputSystem.actions.FindAction("RotateSailCCW", throwIfNotFound: true);
        mLookAction = InputSystem.actions.FindAction("Look", throwIfNotFound: true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        HandlePlayerInput();
        UpdateHullPoints();

        for (int i = 0; i < mForces.Count; i++)
        {
            mForces[i].GenerateForce(this);
        }
    }


    // ----------------------------
    // MOVEMENT

    private void HandlePlayerInput()
    {
        // Looking
        var lookInput = mLookAction.ReadValue<Vector2>();

        mPitch += lookInput.y * mLookSensitivity * -1;
        mPitch = Mathf.Clamp(mPitch, -mPitchLimit, mPitchLimit);

        mPlayerCamera.transform.localRotation = Quaternion.Euler(mPitch, 0, 0);
        mPlayer.transform.Rotate(0, lookInput.x * mLookSensitivity, 0);

        // Boat
        float boatRotation = 0; 

        if (mBoatCW.IsPressed()) { boatRotation++; }
        if (mBoatCCW.IsPressed()) { boatRotation--; }

        transform.Rotate(0, boatRotation * mBoatTurnSpeed, 0);

        // Sail
        float sailRotation = 0;

        if (mSailCW.IsPressed()) { sailRotation++; }
        if (mSailCCW.IsPressed()) { sailRotation--; }

        mSail.transform.Rotate(0, sailRotation * mBoatTurnSpeed, 0);
    }



    // ----------------------------
    // MATH

    public void AddForce(Vector3 force)
    {
        mRigidbody.AddForce(force, ForceMode.Force);  
    }

    public void AddTorque(Vector3 force)
    {
        mRigidbody.AddTorque(force);
    }

    public void AddForceAtPoint(Vector3 force, Vector3 worldPoint)
    {
        mRigidbody.AddForceAtPosition(force, worldPoint, ForceMode.Force);
    }

    // ----------------------------
    // DATA

    // Center of each grid cell on the bottom face of the hull, in world space
    private void UpdateHullPoints()
    {
        int index = 0;
        for (int x = 0; x < mPointsAcross; x++)
        {
            for (int z = 0; z < mPointsAlong; z++)
            {
                // Center of this cell, from -0.5 to 0.5 across the box (the collider's local space)
                float across = (x + 0.5f) / mPointsAcross - 0.5f;
                float along = (z + 0.5f) / mPointsAlong - 0.5f;
                float bottom = -0.5f;

                Vector3 local = mCollider.center + Vector3.Scale(new Vector3(across, bottom, along), mCollider.size);
                mHullPoints[index] = mCollider.transform.TransformPoint(local);
                index++;
            }
        }
    }

    // ----------------------------
    // UTILITY

    public Vector3 GetLinearVelocity() { return mRigidbody.linearVelocity; }
    public Vector3 GetAngularVelocity() { return mRigidbody.angularVelocity; }

    public Vector3 GetConstantAcceleration() { return Physics.gravity; }

    public float GetMass() { return mRigidbody.mass; }

    public float GetLinearDamping() { return mRigidbody.linearDamping; }
    public float GetAngularDamping() { return mRigidbody.angularDamping; }

    public Quaternion GetBaseRotation() { return mBase.transform.rotation; }
    public Quaternion GetSailRoatation() { return mSail.transform.rotation; }

    public Vector3 GetInertiaTensor() { return mRigidbody.inertiaTensor; }

    public float GetVolume() { return mCollider.transform.lossyScale.x * mCollider.transform.lossyScale.y * mCollider.transform.lossyScale.z; }

    public Vector3 GetForwardVector() { return transform.forward; }
    public Vector3 GetSailNormal() { return mSail.transform.right; }

    public Vector3 GetPointVelocity(Vector3 worldPoint) { return mRigidbody.GetPointVelocity(worldPoint); }
    public BoxCollider GetHullCollider() { return mCollider; }
    public Vector3[] GetHullPoints() { return mHullPoints; }
    public Vector3 GetHullSize() { return Vector3.Scale(mCollider.size, mCollider.transform.lossyScale); }
    public float GetPointArea() { return GetHullSize().x * GetHullSize().z / mHullPoints.Length; }
}
