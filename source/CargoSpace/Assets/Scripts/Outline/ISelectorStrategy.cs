using UnityEngine;

internal interface ISelectorStrategy
{
    void Select(Transform target);
    void Deselect(Transform target);
}