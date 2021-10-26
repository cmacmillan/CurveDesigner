//here is a script which manipulates the shape of a curve at runtime
//to make the curve move in a sine wave-ish pattern
//if you add more points to the curve the sine wave will get longer
using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    public class SineWaver : MonoBehaviour
    {
        public Curve3D curve;//Assign this in the inspector
        public float frequency = 1;
        public float amplitude = 1;
        public float speed = 1;
        public void Start()
        {
            //automatic tangents lets us avoid having to manually set the tangents
            curve.positionCurve.automaticTangents = true;
        }
        public void Update()
        {
            var pointGroups = curve.positionCurve.PointGroups;
            for (int i = 0; i < curve.positionCurve.PointGroups.Count; i++)
            {
                PointGroup point = pointGroups[i];
                float yOffset = Mathf.Sin((Time.time * speed + i) * frequency) * amplitude;
                Vector3 position = new Vector3(i, yOffset, 0);
                point.SetPositionLocal(PointGroupIndex.Position, position);
            }
            //After modifying the curve’s shape we need to recalculate to rebuild data structures
            curve.Recalculate();
            //Then we want the mesh of the curve to update
            curve.RequestMeshUpdate();
        }
    }
}
