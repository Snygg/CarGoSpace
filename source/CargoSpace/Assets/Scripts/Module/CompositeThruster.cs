using System.Collections.Generic;
using UnityEngine;

namespace Module
{
    internal class CompositeThruster : IThruster
    {
        public float SpeedFactor { get; }
        public float MaxSpeedFactor { get; }

        public IRigidBodyProvider RigidBodyProvider
        {
            get => _rigidBodyProvider;
            set
            {
                _rigidBodyProvider = value;
                foreach (var thrusterSource in _thrusterSources)
                {
                    foreach (var thruster in thrusterSource.GetThrusters())
                    {
                        thruster.RigidBodyProvider = _rigidBodyProvider;    
                    }
                }
            }
        }

        private readonly List<IThrusterProvider> _thrusterSources = new(5);
        private IRigidBodyProvider _rigidBodyProvider;

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
            foreach (var thruster in thrusterProvider.GetThrusters())
            {
                thruster.RigidBodyProvider = _rigidBodyProvider;    
            }
        }

        public bool Remove(IThrusterProvider thruster)
        {
            return _thrusterSources.Remove(thruster);
        }
    }
}