using UnityEngine;

public class GravityForce : ForceGenerator
{
    public override void GenerateForce(Boat attachedBoat)
    {
        attachedBoat.AddForce(attachedBoat.GetConstantAcceleration() * attachedBoat.GetMass());
    }
}
