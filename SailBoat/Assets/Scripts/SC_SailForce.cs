using UnityEngine;

public class SailForce : ForceGenerator
{
    private WindManager mWindManager;


    private void Start()
    {
        mWindManager = FindAnyObjectByType<WindManager>();
    }

    public override void GenerateForce(Boat attachedBoat)
    {
        attachedBoat.AddForce(mWindManager.GetWind());
    }
}
