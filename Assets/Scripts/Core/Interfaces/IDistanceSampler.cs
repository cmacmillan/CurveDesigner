using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public interface IDistanceSampler : IActiveElement
    {
        void ConstantField(Rect rect);
        string GetLabel();
        Curve3DEditMode GetEditMode();
        IEnumerable<ISamplerPoint> GetPoints(BezierCurve curve);
        IEnumerable<ISamplerPoint> AllPoints();
        void RecalculateOpenCurveOnlyPoints(BezierCurve curve);
        void Sort(BezierCurve curve);
        int InsertPointAtDistance(float distance,BezierCurve curve);
    }
}
