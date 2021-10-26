using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    public class AnimatedRainbow : MonoBehaviour
    {
        public Curve3D curve;//assign in inspector
        public int colorPointCount = 8;
        public void Start()
        {
            curve.colorSampler.UseKeyframes = true;//we want to interpolate a value along the curve
            curve.colorSampler.points.Clear();
            for (int i = 0; i < colorPointCount; i++)
                curve.colorSampler.InsertPointAtDistance(i*curve.CurveLength/(colorPointCount-1),curve.positionCurve);
            //must be called whenever sampler points get reordered
            curve.colorSampler.RecalculateOpenCurveOnlyPoints(curve.positionCurve);
        }
        public void Update()
        {
            foreach (var i in curve.colorSampler.GetPoints(curve.positionCurve))
            {
                i.value = Color.HSVToRGB((Time.time+i.GetDistance(curve.positionCurve)/curve.CurveLength)%1.0f,1,1);
            }
            curve.RequestMeshUpdate();
        }
    }
}
