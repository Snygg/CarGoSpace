using System.Linq;
using UnityEngine;

namespace Outline
{
    public class OutlineSelectorBehavior : MonoBehaviour
    {
        private Transform _selection;
        private ISelectorStrategy _selectorStrategy;

        private void Awake()
        {
            _selectorStrategy = GetComponent<ISelectorStrategy>();
        }

        void Update()
        {
            if (_selection)
            {
                _selectorStrategy.Deselect(_selection);
            }

            var ray3d = Camera.main.ScreenPointToRay(Input.mousePosition);
        
            var hits = new RaycastHit2D[10];
        
            //we have trouble if returned size = buffer size (may not have gotten them all)
            var size = Physics2D.RaycastNonAlloc(ray3d.origin,Vector2.zero, hits);
        
            _selection = hits
                .Take(size)
                .FirstOrDefault(hit => hit.transform?.gameObject.TryGetComponent<ITargetable>(out _) ?? false)
                .transform;

            if (_selection)
            {
                _selectorStrategy.Select(_selection);
            }
        }
    }
}