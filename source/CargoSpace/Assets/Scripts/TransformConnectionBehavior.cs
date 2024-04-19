using System;
using Npc;
using R3;
using UnityEngine;

/// <summary>
/// represents a connection between a module host and a module that is hierarchical (transform.parent) in the scene
/// </summary>
internal class TransformConnectionBehavior: MonoBehaviour, IModuleConnection
{
    private DistanceJoint2D _joint2D;
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

    public void Attach(GameObject parent, GameObject child)
    {
        child.transform.parent = parent.transform;
        _joint2D = child.gameObject.AddComponent<DistanceJoint2D>();
        _joint2D.connectedBody = parent.gameObject.GetComponent<Rigidbody2D>();
        _joint2D.distance = 0.1f;
        //_joint2D.maxDistanceOnly = true;
        // _joint2D.dampingRatio = 1;
        // _joint2D.frequency = 0;
    }
}