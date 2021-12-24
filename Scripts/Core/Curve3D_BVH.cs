using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class Curve3D_BVH
    {
        public static bool TestHitTriangle(Ray ray, Vector3 p0,Vector3 p2,Vector3 p3)
        {
            return true;
        }
        public static void RayBary(Ray ray, Vector3 p0,Vector3 p2,Vector3 p3, out float b0, out float b1, out float b2)
        {
            b0 = 0;
            b1 = 0;
            b2 = 0;
        }
    }
}
