#if UNITY_EDITOR
using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public class SecondaryPositionCurveComposite : Composite
    {
        public PositionCurveComposite positionCurve;
        public PointAlongCurveComposite centerPoint;
        private Curve3D _curve;
        public TransformBlob transformBlob;
        public SecondaryPositionCurveComposite(Composite parent,Curve3D curve, ExtrudeSamplerPoint secondaryBezierCurve,ISampler sampler,List<BezierCurve> allCurves,int secondaryCurveIndex) : base (parent)
        {
            this._curve = curve; 
            transformBlob = new TransformBlob(curve.transform,null,curve);
            this.positionCurve = new PositionCurveComposite(this, curve, secondaryBezierCurve.value ,new SecondaryPositionCurveSplitCommand(secondaryBezierCurve.value,curve,this),transformBlob,allCurves,secondaryCurveIndex);
            centerPoint = new PointAlongCurveComposite(this, secondaryBezierCurve, curve.UICurve.positionCurve, UnityEngine.Color.green,secondaryBezierCurve.GUID,sampler);
            transformBlob._additionalTransform = new DynamicMatrix4x4(centerPoint);//works because transform blob is immutable
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return positionCurve;
            yield return centerPoint;
        }
        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            UICurve.GetNormalsTangentsDraw(drawList, _curve, this,transformBlob,positionCurve.positionCurve);
            UICurve.GetCurveDraw(drawList,positionCurve.positionCurve,transformBlob,this);
            base.Draw(drawList, closestElementToCursor);
        }
    }
}
#endif
