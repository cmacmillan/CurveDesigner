namespace ChaseMacMillan.CurveDesigner
{
    public interface ISamplerPoint<DataType>
    {
        void Construct(Sampler<DataType,SamplerPoint<DataType>> owner,Curve3D curve);
        void Construct(ISamplerPoint<DataType> other, Sampler<DataType,SamplerPoint<DataType>> owner, bool createNewGuids, Curve3D curve);
    }
    public interface ISamplerPoint : ISelectable
    {
        float Time { get; set; }
        int SegmentIndex { get; set; }
        void SetDistance(float distance, BezierCurve curve, bool shouldSort = true);
        KeyframeInterpolationMode InterpolationMode { get; set; }
    }
}
