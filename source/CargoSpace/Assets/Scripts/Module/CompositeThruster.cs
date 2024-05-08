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

        private readonly List<IThrusterSource> _thrusterSources = new(5);
        private IRigidBodyProvider _rigidBodyProvider;

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
            foreach (var thruster in thrusterSource.GetThrusters())
            {
                thruster.RigidBodyProvider = _rigidBodyProvider;    
            }
        }

        public bool Remove(IThrusterSource thruster)
        {
            return _thrusterSources.Remove(thruster);
        }
    }
}