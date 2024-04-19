using System;
using Npc;
using R3;
using UnityEngine;

/// <summary>
/// represents a connection between a module host and a module that is hierarchical (transform.parent) in the scene
/// </summary>
internal class TransformConnectionBehavior: MonoBehaviour, IModuleConnection
{
    public void Detach()
    {
        Destroy(this);
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
    }
}