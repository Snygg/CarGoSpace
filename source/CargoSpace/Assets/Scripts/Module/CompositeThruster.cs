using System.Collections.Generic;
using UnityEngine;

namespace Module
{
    internal class CompositeThruster : IThruster
    {
        public float SpeedFactor { get; }
        public float MaxSpeedFactor { get; }
        private readonly List<IThrusterSource> _thrusterSources = new(5);
        public void ThrustTowards(Vector2 target)
        {
            foreach (var thrusterSource in _thrusterSources)
            {
                foreach (var thruster in thrusterSource.GetThrusters())
                {
                    thruster.ThrustTowards(target);    
                }
            }
        }

        public void Add(IThrusterSource thrusterSource)
        {
            _thrusterSources.Add(thrusterSource);
        }

        public bool Remove(IThrusterSource thruster)
        {
            return _thrusterSources.Remove(thruster);
        }
    }
}