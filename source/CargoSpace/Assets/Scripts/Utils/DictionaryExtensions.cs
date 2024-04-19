using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class DictionaryExtensions {
        public static bool TryGetVector2(
            this IReadOnlyDictionary<string, string> body,
            string key,
            out Vector2 vector2)
        {
            if (body == null)
            {
                vector2 = default;
                return false;
            }
            if (!body.TryGetValue(key, out string sValue))
            {
                vector2 = default;
                return false;
            }

            return sValue.TryParseVector2(out vector2);
        }
    
        public static bool TryGetVector3(
            this IReadOnlyDictionary<string, string> body,
            string key,
            out Vector3 vector3)
        {
            if (body == null)
            {
                vector3 = default;
                return false;
            }
            if (!body.TryGetValue(key, out string sValue))
            {
                vector3 = default;
                return false;
            }

            return sValue.TryParseVector3(out vector3);
        }
    
        public static bool TryGetFloat(
            this IReadOnlyDictionary<string, string> body,
            string key,
            out float fValue)
        {
            if (body == null)
            {
                fValue = default;
                return false;
            }
            if (!body.TryGetValue(key, out string sValue))
            {
                fValue = default;
                return false;
            }

            return float.TryParse(sValue,out fValue);
        }
    
        public static bool TryGetDouble(
            this IReadOnlyDictionary<string, string> body,
            string key,
            out double dValue)
        {
            if (body == null)
            {
                dValue = default;
                return false;
            }
            if (!body.TryGetValue(key, out string sValue))
            {
                dValue = default;
                return false;
            }

            return double.TryParse(sValue,out dValue);
        }
    }
}