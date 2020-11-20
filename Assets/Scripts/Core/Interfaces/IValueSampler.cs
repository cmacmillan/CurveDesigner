namespace ChaseMacMillan.CurveDesigner
{
    public interface IValueSampler<T> : IValueSampler
    {
        T ConstValue { get; set; }
    }
    public interface IValueSampler : IDistanceSampler
    {
        bool UseKeyframes { get; set; }
    }
}
