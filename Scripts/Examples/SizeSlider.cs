//This script slides a size point back and forth along a curve. The curve must have at least 2 points and sizeSampler must have useKeyframes=true
using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    public class SizeSlider : MonoBehaviour
    {
        public Curve3D curve;

        void Update()
        {
            var points = curve.sizeSampler.GetPoints(curve.positionCurve); // get the size points
            float distance = (Mathf.Sin(Time.time) + 1) * .5f * curve.CurveLength; // calculate the desired distance
            points[1].SetDistance(distance, curve.positionCurve); // assign this distance to point at index 1
            curve.RequestMeshUpdate(); // update the mesh
        }
    }
}
