using Module;
using UnityEngine;

public interface IThruster
{
    float SpeedFactor { get; }
    float MaxSpeedFactor { get; }
    void DirectThrust(Vector2 normalizedDirection);
}