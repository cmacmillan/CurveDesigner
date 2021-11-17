using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public interface IValueSampler<T> : IValueSampler
    {
        T ConstValue { get; set; }
    }
    public interface IValueSampler : ISampler
    {
        bool UseKeyframes { get; set; }
#if UNITY_EDITOR
        void ConstantField(Rect rect);
#endif
    }
}
