using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public interface ISampler<T,SamplerPointType> : ISampler
    {
        T CloneValue(T val,bool shouldCreateGuids);
    }
    public interface ISampler : IActiveElement
    {
        string GetLabel();
        Curve3DEditMode GetEditMode();
        IEnumerable<ISamplerPoint> GetPoints(BezierCurve curve);
        IEnumerable<ISamplerPoint> AllPoints();
        void RecalculateOpenCurveOnlyPoints(BezierCurve curve);
        void Sort(BezierCurve curve);
        int InsertPointAtDistance(float distance,BezierCurve curve);
    }
}
