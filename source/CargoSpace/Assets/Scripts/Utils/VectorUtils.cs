using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

        input.Replace("(", "").Replace(")", "").Replace(" ", "");
        var values = input.Split(',');
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
}

