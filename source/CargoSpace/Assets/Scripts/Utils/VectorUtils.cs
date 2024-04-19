using UnityEngine;

namespace Utils
{
    public static class VectorUtils
    {
        public static bool TryParseVector3(this string input, out Vector3 output)
        {
            //"(0.0, 0.0, 0.0)"
            output = new Vector3();
            if (string.IsNullOrEmpty(input) || !(input.StartsWith("(") && input.EndsWith(")")))
            {
                return false;
            }

            var replaced = input.Replace("(", "").Replace(")", "").Replace(" ", "");
            var values = replaced.Split(',');
            if (values.Length != 3)
            {
                return false;
            }

            if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y) && float.TryParse(values[2], out float z))
            {
                output = new Vector3(x, y, z);
                return true;
            }

            return false;
        }
    
        public static bool TryParseVector2(this string input, out Vector2 output)
        {
            //"(0.0, 0.0, 0.0)"
            if (string.IsNullOrEmpty(input) || !(input.StartsWith("(") && input.EndsWith(")")))
            {
                output = Vector2.zero;
                return false;
            }

            var replaced = input.Replace("(", "").Replace(")", "").Replace(" ", "");
            var values = replaced.Split(',');
            if (values.Length < 2)
            {
                output = Vector2.zero;
                return false;
            }

            if (!float.TryParse(values[0], out float x) || !float.TryParse(values[1], out float y))
            {
                output = Vector2.zero;
                return false;
            }
        
            output = new Vector2(x, y);
            return true;
        }
    }
}