using System.Collections.Generic;
using UnityEngine;

public class WindManager : MonoBehaviour
{
    [SerializeField] private Vector3 mWind;

    [SerializeField] private ParticleSystem mParticleSystemBase;
    private ParticleSystem[] mParticleSystems;
    private ParticleSystem.MainModule[] mMainModules;
    private ParticleSystem.EmissionModule[] mEmmisionModules;
    private ParticleSystem.TrailModule[] mTrailModules;

    private Boat[] mBoats;

    [SerializeField] private float mWindParticles;


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
    }

    private void Update()
    {
        float magnitude = mWind.magnitude;
        int particles = Mathf.FloorToInt(mWindParticles * magnitude);
        float invMass = mBoats[0].GetMass();

        for (int i = 0; i < mMainModules.Length; i++)
        {
            mMainModules[i].emitterVelocity = mWind * invMass * 0.0005f;
            mMainModules[i].gravityModifier = 0;
            mEmmisionModules[i].rateOverTime = particles;
            mTrailModules[i].lifetime = magnitude * invMass * 0.0001f;
        }
    }

    public Vector3 GetWind() { return mWind; }
}
