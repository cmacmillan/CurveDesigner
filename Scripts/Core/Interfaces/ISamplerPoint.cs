namespace ChaseMacMillan.CurveDesigner
{
    public interface ISamplerPoint<DataType,SamplerPointType> : ISamplerPoint
    {
        void Construct(ISampler<DataType,SamplerPointType> owner,Curve3D curve);
        void Construct(ISamplerPoint<DataType,SamplerPointType> other, ISampler<DataType,SamplerPointType> owner, bool createNewGuids, Curve3D curve);
        DataType Value { get; set; }
        ISampler<DataType,SamplerPointType> Owner { get; set; }
    }
    public interface ISamplerPoint : ISelectable
    {
        float GetDistance(BezierCurve curve, bool useCached);
        float Time { get; set; }
        int SegmentIndex { get; set; }
        void SetDistance(float distance, BezierCurve curve, bool shouldSort = true);
        KeyframeInterpolationMode InterpolationMode { get; set; }
        float CachedDistance { get; set; }
    }
}
