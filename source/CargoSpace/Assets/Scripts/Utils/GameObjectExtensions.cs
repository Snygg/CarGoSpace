using UnityEngine;

namespace Utils
{
    public static class GameObjectExtensions
    {
        public static bool TryGetComponent<T>(this GameObject gameObject, out T component) where T:Component
        {
            if (gameObject == null)
            {
                component = default;
                return false;
            }
            var type = typeof(T);
            var result = gameObject.TryGetComponent(type, out var c);
            component = c as T;
            return result;
        }
    }
}