using Module;
using UnityEngine;

public class ThrustBehavior : MonoBehaviour, IThruster
{
    public float speedFactor;
    float IThruster.SpeedFactor => speedFactor;
    public float maxSpeedFactor;
    public IRigidBodyProvider RigidBodyProvider;
    public bool DrawDebugThrust = true;

    IRigidBodyProvider IThruster.RigidBodyProvider
    {
        get => RigidBodyProvider;
        set => RigidBodyProvider = value;
    }
    float IThruster.MaxSpeedFactor => maxSpeedFactor;

    private void Awake()
    {
        if (RigidBodyProvider == null)
        {
            RigidBodyProvider = GetComponent<IRigidBodyProvider>();    
        }
    }

    public void DirectThrust(Vector2 normalizedDirection)
    {
        var factored = normalizedDirection * speedFactor;
        ApplyThrust(factored);

        if (DrawDebugThrust)
        {
            Debug.DrawLine(transform.position,transform.position+(Vector3)factored,Color.magenta,0.1f);    
        }
    }

    private void ApplyThrust(Vector2 force)
    {
        if (RigidBodyProvider == null || !RigidBodyProvider.RigidBody)
        {
            return;
        }
        RigidBodyProvider.RigidBody.AddForce(force);
    }
}