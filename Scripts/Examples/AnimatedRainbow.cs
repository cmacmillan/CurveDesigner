//This script will produce an animated rainbow along a curve
//Make sure you've assigned materials in the texture category that read from vertex color
//such as CurveDesigner/Art/Materials/DrawColorSurface
using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    public class AnimatedRainbow : MonoBehaviour
    {
        public Curve3D curve; //Assign this in the inspector
        public int colorPointCount = 24;
        public float length = 1;
        public float speed = 1;
        public void Start()
        {
            curve.colorSampler.UseKeyframes = true;//lets us interpolate a value along the curve
            curve.colorSampler.points.Clear();
            for (int i = 0; i < colorPointCount; i++)
            {
                float distance = i * curve.CurveLength / (colorPointCount - 1);
                curve.colorSampler.InsertPointAtDistance(distance, curve.positionCurve);
            }
            //must be called whenever sampler points get reordered/added/removed
            curve.colorSampler.Sort(curve.positionCurve);
        }
        public void Update()
        {
            foreach (var i in curve.colorSampler.GetPoints(curve.positionCurve))
            {
                float distance = i.GetDistance(curve.positionCurve);
                float hue = (Time.time * speed + length * distance / curve.CurveLength) % 1.0f;
                i.value = Color.HSVToRGB(hue, 1, 1);
            }
            //We haven’t changed the shape of the curve so we don’t need to call Recalculate()
            //But we do want the curve’s mesh to update to display the new colors
            curve.RequestMeshUpdate();
        }
    }
}
