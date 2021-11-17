#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class ColorCurveComposite : Composite, IWindowDrawer, IValueAlongCurvePointProvider
    {
        private ColorSampler sampler;
        private Curve3D curve;
        private List<EditColorComposite> colorPoints = new List<EditColorComposite>();
        private SplitterPointComposite splitterPoint;
        public ColorCurveComposite(Composite parent,ColorSampler sampler,Curve3D curve,PositionCurveComposite positionCurveComposite) : base(parent)
        {
            this.sampler = sampler;
            this.curve = curve;
            var pinkColor = new Color(.95f,.1f,.8f);
            splitterPoint = new SplitterPointComposite(this, positionCurveComposite.transformBlob, PointTextureType.circle,new ValueAlongCurveSplitCommand(curve,sampler,ValueAlongCurveSplitCommand.GetColorCurve),pinkColor,positionCurveComposite);
            foreach (var i in sampler.GetPoints(curve.positionCurve))
                colorPoints.Add(new EditColorComposite(this,i,sampler,pinkColor,positionCurveComposite,curve));
        }

        public void DrawWindow(Curve3D curve)
        {
            WindowDrawer.Draw(sampler.GetPoints(curve.positionCurve),curve);
        }

        public override IEnumerable<Composite> GetChildren()
        {
            yield return splitterPoint;
            foreach (var i in colorPoints)
                yield return i;
        }

        public Clickable GetPointAtIndex(int index)
        {
            return colorPoints[index].centerPoint.point;
        }
    }
}
#endif
