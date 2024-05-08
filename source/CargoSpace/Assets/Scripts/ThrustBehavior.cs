using UnityEngine;

public class ThrustBehavior : MonoBehaviour, IThruster
{
    public float speedFactor;
    float IThruster.SpeedFactor => speedFactor;
    public float maxSpeedFactor;
    public bool DrawDebugThrust = true;
    float IThruster.MaxSpeedFactor => maxSpeedFactor;
    public Rigidbody2D Rigidbody;

    private void Awake()
    {
        if (!Rigidbody)
        {
            Rigidbody = GetComponent<Rigidbody2D>();
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
        if (!Rigidbody)
        {
            return;
        }
        Rigidbody.AddForce(force);
    }
}