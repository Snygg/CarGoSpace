using UnityEngine;

namespace Outline
{
    internal interface ISelectorStrategy
    {
        void Select(Transform target);
        void Deselect(Transform target);
    }
}