using System;
using Module;
using Npc;
using R3;
using UnityEngine;

/// <summary>
/// represents a connection between a module host and a module that is hierarchical (transform.parent) in the scene
/// </summary>
internal class JointConnectionBehavior: MonoBehaviour, IModuleConnection
{
    private FixedJoint2D _joint2D;
    public SerializableReactiveProperty<bool> Attached { get; } = new SerializableReactiveProperty<bool>(false);
    public IModuleRoot Module { get; private set; }
    public IModuleHost Host { get; private set; }
    public void Detach()
    {
        Destroy(gameObject);
        Destroy(_joint2D);
    }

    private void OnDestroy()
    {
        Detach();
    }

    public void ApplyDamage(float strength)
    {
        hp.Value = Math.Max(0, hp.Value - strength);
    }

    public SerializableReactiveProperty<float> hp = new(1);
    SerializableReactiveProperty<float> IModuleConnection.Hp => hp;

    public void Attach(IModuleHost parent, IModuleRoot child, Vector2 relativeLocation)
    {
        if (child == null)
        {
            return;
        }
        if (Module != null)
        {
            return;
        }
        Module = child;
        Host = parent;
        
        var childGo = child.gameObject;
        _joint2D = childGo.AddComponent<FixedJoint2D>();
        _joint2D.connectedBody = parent.RigidBody;
        //_joint2D.connectedAnchor = relativeLocation;
        //_joint2D.distance = 5f;
        //_joint2D.maxDistanceOnly = true;
        // _joint2D.dampingRatio = 1;
        // _joint2D.frequency = 0;
    }
}