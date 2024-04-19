using UnityEngine;

public class ThrustBehavior : MonoBehaviour, IThruster
{
    public float speedFactor;
    float IThruster.SpeedFactor => speedFactor;
    public float maxSpeedFactor;
    private Rigidbody2D _rigidBody;
    float IThruster.MaxSpeedFactor => maxSpeedFactor;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    public void ThrustTowards(Vector2 target)
    {
        var current = (Vector2)transform.position;
        var path = new Vector2(target.x, target.y) - current;
        var normalized = path.normalized;
        var factored = normalized * speedFactor;
        ApplyThrust(factored);
    }

    private void ApplyThrust(Vector2 force)
    {
        _rigidBody.AddForce(force);
    }
}