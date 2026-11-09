using UnityEngine;

public class SailForce : ForceGenerator
{
    public override void GenerateForce(Boat attachedBoat)
    {
        attachedBoat.AddForce(attachedBoat.transform.forward * 10);
    }
}
