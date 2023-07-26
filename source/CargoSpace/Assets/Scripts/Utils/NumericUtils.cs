using System;

public static class NumericUtils
{
    public static bool HasChanged(this float thisValue, float other, float epsilon = float.Epsilon)
    {
        return Math.Abs(thisValue - other) > epsilon;
    }
}