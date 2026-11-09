using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class Boat : MonoBehaviour
{
    [SerializeField] private GameObject mBase, mSail;

    private Collider mCollider;
    private Rigidbody mRigidbody;

    [SerializeField] private List<ForceGenerator> mForces;

    private WindManager mWindManager;

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
        mCollider = GetComponent<Collider>();
        mRigidbody = GetComponent<Rigidbody>();
        mWindManager = FindAnyObjectByType<WindManager>();

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
}
