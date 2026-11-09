using UnityEngine;

public abstract class ForceGenerator : MonoBehaviour
{
    public abstract void GenerateForce(Boat attachedBoat);
}
