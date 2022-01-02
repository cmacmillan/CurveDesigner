using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    //this class needs to serialize
    public class Curve3D_BVH
    {
        private struct TriData
        {
            public Vector3 p0;
            public Vector3 p1;
            public Vector3 p2;
            public float crosswise0;
            public float crosswise1;
            public float crosswise2;
            public float lengthwise0;
            public float lengthwise1;
            public float lengthwise2;
        }
        private List<TriData> tris = new List<TriData>();
        //private MeshGeneratorOutput meshGenOutput;
        public Curve3D_BVH(MeshGeneratorOutput meshGenOutput)
        {
            var t = meshGenOutput.triangles;
            var v = meshGenOutput.data;
            var d = meshGenOutput.distances;
            for (int i=0;i<t.Count;i+=3)
            {

                tris.Add(new TriData()
                {
                    p0 = v[t[i]].position,
                    p1 = v[t[i + 1]].position,
                    p2 = v[t[i + 2]].position,
                    crosswise0 = d[t[i]].crosswise,
                    crosswise1 = d[t[i+1]].crosswise,
                    crosswise2 = d[t[i+2]].crosswise,
                    lengthwise0 = d[t[i]].lengthwise,
                    lengthwise1 = d[t[i+1]].lengthwise,
                    lengthwise2 = d[t[i+2]].lengthwise,
                });
            }
        }
        public bool RaycastSurface(Ray ray, out float lengthwise,out float crosswise, out Vector3 hitPoint)
        {
            lengthwise = 0;
            crosswise = 0;
            hitPoint = Vector3.zero;
            //transform ray into local space
            float minSqrDist = float.MaxValue;
            foreach (var i in tris)
            {
                if (HitTriangle(ray, i.p0, i.p1, i.p2, out float b0, out float b1, out float b2,out Vector3 point))
                {
                    float sqrDist = (point - ray.origin).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        crosswise = i.crosswise0 * b0 + i.crosswise1 * b1 + i.crosswise2 * b2;
                        lengthwise = i.lengthwise0 * b0 + i.lengthwise1 * b1 + i.lengthwise2 * b2;
                        hitPoint = point;
                    }
                }
            }
            if (minSqrDist == float.MaxValue)
                return false;
            return true;
        }
        public static bool HitTriangle(Ray ray, Vector3 p0,Vector3 p1,Vector3 p2, out float b0, out float b1, out float b2, out Vector3 point)
        {
            b0 = 0;
            b1 = 0;
            b2 = 0;
            Vector3 p01CrossP02 = Vector3.Cross(p2 - p0, p1 - p0);
            Vector3 normal = p01CrossP02.normalized;
            point = Vector3.zero;
            float nDotR = Vector3.Dot(normal,ray.direction);
            if (nDotR == 0.0f)
                return false;
            float d = Vector3.Project(p0,normal).magnitude;
            float t = -(Vector3.Dot(normal, ray.origin)+d)/nDotR;
            if (t < 0)
                return false;
            Vector3 planeHit = ray.GetPoint(t);
            Vector3 p01Crossp0h = Vector3.Cross(planeHit-p0,p1-p0);
            if (Vector3.Dot(p01Crossp0h, normal) <= 0)
                return false;
            Vector3 p12Crossp1h = Vector3.Cross(planeHit - p1,p2 - p1);
            if (Vector3.Dot(p12Crossp1h, normal) <= 0)
                return false;
            Vector3 p20Crossp2h = Vector3.Cross(planeHit - p2,p0 - p2);
            if (Vector3.Dot(p20Crossp2h, normal) <= 0)
                return false;
            point = planeHit;
            float area = p01CrossP02.magnitude;// div by 2 technically
            b0 = p12Crossp1h.magnitude / area;
            b1 = p20Crossp2h.magnitude / area;
            b2 = p01Crossp0h.magnitude / area;
            return true;
        }
    }
}
