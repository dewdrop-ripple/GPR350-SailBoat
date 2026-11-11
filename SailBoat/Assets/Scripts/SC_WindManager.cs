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

    [SerializeField] private int mWindParticles;


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
        int particles = mWindParticles * Mathf.FloorToInt(magnitude);

        for (int i = 0; i < mMainModules.Length; i++)
        {
            mMainModules[i].emitterVelocity = mWind;
            mMainModules[i].gravityModifier = magnitude * -0.01f;
            mEmmisionModules[i].rateOverTime = particles;
            mTrailModules[i].lifetime = magnitude;
        }
    }

    public Vector3 GetWind() { return mWind; }
}
