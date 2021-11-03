using System;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public static class Utils
    {
        public static float ModFloat(float x, float m)
        {
            return (x % m + m) % m;
        }
        public static int ModInt(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
