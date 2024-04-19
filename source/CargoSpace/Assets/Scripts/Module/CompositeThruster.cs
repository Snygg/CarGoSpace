using System.Collections.Generic;
using UnityEngine;

internal class CompositeThruster : IThruster
{
    public float SpeedFactor { get; }
    public float MaxSpeedFactor { get; }
    private readonly List<IThrusterSource> _thrusters = new(5);
    public void ThrustTowards(Vector2 target)
    {
        foreach (var thrusterSource in _thrusters)
        {
            foreach (var thruster in thrusterSource.GetThrusters())
            {
                thruster.ThrustTowards(target);    
            }
        }
    }

    public void Add(IThrusterSource thrusterSource)
    {
        _thrusters.Add(thrusterSource);
    }

    public bool Remove(IThrusterSource thruster)
    {
        return _thrusters.Remove(thruster);
    }
}