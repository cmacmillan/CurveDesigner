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
        private MeshGeneratorOutput meshGenOutput;
        public Curve3D_BVH(MeshGeneratorOutput meshGenOutput)
        {

        }
        public static bool HitTriangle(Ray ray, Vector3 p0,Vector3 p1,Vector3 p2, out float b0, out float b1, out float b2)
        {
            b0 = 0;
            b1 = 0;
            b2 = 0;
            Vector3 p01CrossP02 = Vector3.Cross(p1 - p0, p2 - p0);
            Vector3 normal = p01CrossP02.normalized;
            //Vector3 normal = Vector3.Cross(p2-p0,p1-p0).normalized;
            float nDotR = Vector3.Dot(normal,ray.direction);
            if (nDotR == 0.0f)
                return false;
            float d = Vector3.Project(p0,normal).magnitude;
            float t = -(Vector3.Dot(normal, ray.origin)+d)/nDotR;
            if (t < 0)
                return false;
            Vector3 planeHit = ray.GetPoint(t);
            Vector3 p01Crossp0h = Vector3.Cross(p1-p0,planeHit-p0);
            if (Vector3.Dot(p01Crossp0h, normal) <= 0)
                return false;
            Vector3 p12Crossp1h = Vector3.Cross(p2 - p1, planeHit - p1);
            if (Vector3.Dot(p12Crossp1h, normal) <= 0)
                return false;
            Vector3 p20Crossp2h = Vector3.Cross(p0 - p2, planeHit - p2);
            if (Vector3.Dot(p20Crossp2h, normal) <= 0)
                return false;
            return true;
        }
        public static void Test()
        {
            var ray = new Ray(new Vector3(0,10,10),new Vector3(1,0,0));
            Vector3 p0 = new Vector3(3,9,9);
            Vector3 p1 = new Vector3(3,10,11);
            Vector3 p2 = new Vector3(3,11,9);
            Debug.Log(HitTriangle(ray,p0,p1,p2, out _, out _, out _));
        }
    }
}
