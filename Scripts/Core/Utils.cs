using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
