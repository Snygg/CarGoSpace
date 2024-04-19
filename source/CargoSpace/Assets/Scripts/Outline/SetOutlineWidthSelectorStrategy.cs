using UnityEngine;

namespace Outline
{
    internal class SetOutlineWidthSelectorStrategy : MonoBehaviour, ISelectorStrategy
    {
        public float selectedOutlineWidth = 10;
        public void Select(Transform target)
        {
            if (target.gameObject.TryGetComponent<QuickOutline.Scripts.Outline>(out var outline))
            {
                outline.OutlineWidth = selectedOutlineWidth;
            }
        }

        public void Deselect(Transform target)
        {
            if (target.gameObject.TryGetComponent<QuickOutline.Scripts.Outline>(out var outline))
            {
                outline.OutlineWidth = 0;
            }
        }
    }
}