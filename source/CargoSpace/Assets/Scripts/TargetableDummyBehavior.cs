using UnityEngine;

public class TargetableDummyBehavior : MonoBehaviour, ITargetable
{
    public void OnDamaged(float strength)
    {
        //intentionally empty
    }

    public bool IsPlayer => false;
}