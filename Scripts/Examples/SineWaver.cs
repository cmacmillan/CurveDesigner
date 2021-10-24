//here is a script which manipulates the shape of a curve at runtime
//to make the curve move in a sine wave-ish pattern
using System.Collections.Generic;
using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    public class SineWaver : MonoBehaviour
    {
        public Curve3D curve;//Assign this in the inspector
        public float frequency = 1;
        public float amplitude = 1;
        public float speed = 1;
        public List<Vector3> initialPositions = new List<Vector3>();
        public List<float> initialDistances = new List<float>();
        public void Start()
        {
            curve.Recalculate();//shouldn't have to do this, this is stupid
            //why don't i just say fuck it and serialize the segments
            var pointGroups = curve.positionCurve.PointGroups;
            for (int i = 0; i < curve.positionCurve.PointGroups.Count; i++)
            {
                PointGroup point = pointGroups[i];
                initialPositions.Add(point.GetPositionLocal(PointGroupIndex.Position));
                initialDistances.Add(point.GetDistance(curve.positionCurve));
            }
        }
        public void Update()
        {
            var pointGroups = curve.positionCurve.PointGroups;
            for (int i = 0; i < curve.positionCurve.PointGroups.Count; i++)
            {
                PointGroup point = pointGroups[i];
                float distance = initialDistances[i];
                float yOffset = Mathf.Sin((Time.time * speed + distance) * frequency) * amplitude;
                Vector3 offset = new Vector3(0, yOffset, 0);
                Vector3 finalPosition = offset + initialPositions[i];
                point.SetPositionLocal(PointGroupIndex.Position, finalPosition);
            }
            //After modifying the curve we need to recalculate to rebuild some data structures
            curve.Recalculate();
            //Then we want the mesh of the curve to update
            curve.RequestMeshUpdate();
        }
    }
}
