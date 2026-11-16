using System.Collections.Generic;
using UnityEngine;

public class WindManager : MonoBehaviour
{
    private Vector3 mWind;

    [SerializeField] private ParticleSystem mParticleSystemBase;
    private ParticleSystem[] mParticleSystems;
    private ParticleSystem.MainModule[] mMainModules;
    private ParticleSystem.EmissionModule[] mEmmisionModules;
    private ParticleSystem.TrailModule[] mTrailModules;

    private Boat[] mBoats;

    [SerializeField] private float mWindParticles;


    // Randomness

    [SerializeField, Range(0, 6.283f)] private float mWindDirection = 0; // 0 - 2pi
    [SerializeField, Range(5f, 20f)] private float mWindMagnitude = 7; // 5 - 20

    private bool mDirectionIncreasing = true;
    private bool mMagnitudeIncreasing = false;

    [SerializeField] private float mChangeFrequency;
    private float mTimer = 0f;


    private void Start()
    {
        mBoats = FindObjectsByType<Boat>();

        mParticleSystems = new ParticleSystem[mBoats.Length];
        mMainModules = new ParticleSystem.MainModule[mBoats.Length];
        mEmmisionModules = new ParticleSystem.EmissionModule[mBoats.Length];
        mTrailModules = new ParticleSystem.TrailModule[mBoats.Length];

        for (int i = 0; i < mParticleSystems.Length; i++)
        {
            mParticleSystems[i] = Instantiate(mParticleSystemBase, mBoats[i].transform);
            mMainModules[i] = mParticleSystems[i].main;
            mEmmisionModules[i] = mParticleSystems[i].emission;
            mTrailModules[i] = mParticleSystems[i].trails;
        }

        UpdateWind();
    }

    private void Update()
    {
        mTimer += Time.deltaTime;

        if (mTimer > mChangeFrequency)
        {
            mTimer = 0f;
            UpdateWind();
        }
    }

    private void UpdateWind()
    {
        // Randomness
        float rand = Random.Range(0f, 1f);
        if (rand <= 0.15f) { mDirectionIncreasing = !mDirectionIncreasing; }

        if (mWindMagnitude > 20f) { mMagnitudeIncreasing = false; }
        else if (mWindMagnitude < 5f) { mMagnitudeIncreasing = true; }
        else
        {
            rand = Random.Range(0f, 1f);
            if (rand <= 0.25f) { mMagnitudeIncreasing = !mMagnitudeIncreasing; }
        }

        float directionChange = Random.Range(0.1f, 0.64f);
        float magnitudeChange = Random.Range(0.1f, 2f);

        if (!mDirectionIncreasing) { directionChange *= -1; }
        if (!mMagnitudeIncreasing) { directionChange *= -1; }

        mWindDirection += directionChange;
        mWindMagnitude += magnitudeChange;

        mWind = new Vector3(Mathf.Cos(mWindDirection) * mWindMagnitude, 0, Mathf.Sin(mWindDirection) * mWindMagnitude);


        // Update particles
        float magnitude = mWind.magnitude;
        int particles = Mathf.FloorToInt(mWindParticles * magnitude);

        for (int i = 0; i < mMainModules.Length; i++)
        {
            mMainModules[i].emitterVelocity = mWind;
            mMainModules[i].gravityModifier = -0.05f;
            mEmmisionModules[i].rateOverTime = particles;
            mTrailModules[i].lifetime = magnitude;
        }
    }

    public Vector3 GetWind() { return mWind; }
}
