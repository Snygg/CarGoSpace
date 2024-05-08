using System.Collections.Generic;
using UnityEngine;

namespace Module
{
    internal class CompositeThruster : IThruster
    {
        public float SpeedFactor { get; }
        public float MaxSpeedFactor { get; }

        private readonly List<IThrusterProvider> _thrusterSources = new(5);

        public void DirectThrust(Vector2 normalizedDirection)
        {
            foreach (var thrusterSource in _thrusterSources)
            {
                foreach (var thruster in thrusterSource.GetThrusters())
                {
                    thruster.DirectThrust(normalizedDirection);    
                }
            }
        }

        public void Add(IThrusterProvider thrusterProvider)
        {
            _thrusterSources.Add(thrusterProvider);
        }

        public bool Remove(IThrusterProvider thruster)
        {
            return _thrusterSources.Remove(thruster);
        }
    }
}