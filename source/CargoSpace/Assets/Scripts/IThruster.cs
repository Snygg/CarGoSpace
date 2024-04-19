using UnityEngine;

public interface IThruster
{
    float SpeedFactor { get; }
    float MaxSpeedFactor { get; }
    void ThrustTowards(Vector2 target);
}