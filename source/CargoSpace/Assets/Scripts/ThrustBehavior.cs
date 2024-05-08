using Module;
using UnityEngine;

public class ThrustBehavior : MonoBehaviour, IThruster
{
    public float speedFactor;
    float IThruster.SpeedFactor => speedFactor;
    public float maxSpeedFactor;
    public IRigidBodyProvider RigidBodyProvider;

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

    public void ThrustTowards(Vector2 target)
    {
        var current = (Vector2)transform.position;
        var path = new Vector2(target.x, target.y) - current;
        var normalized = path.normalized;
        var factored = normalized * speedFactor;
        ApplyThrust(factored);
        
        Debug.DrawLine(transform.position,normalized *10,Color.magenta,0.1f);
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