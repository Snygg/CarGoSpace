using Module;
using UnityEngine;

public interface IThruster
{
    float SpeedFactor { get; }
    float MaxSpeedFactor { get; }
    IRigidBodyProvider RigidBodyProvider { get; set; }
    void DirectThrust(Vector2 normalizedDirection);
}