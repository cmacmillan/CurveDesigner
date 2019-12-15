using UnityEngine;

public interface IFieldKeyframe<T>
{
    float Distance { get; set; }
    T Value { get; set; }
    IFieldKeyframe<T> Clone();
    T Lerp(IFieldKeyframe<T> end,float time);
}
public abstract class FieldKeyframe<T>: IFieldKeyframe<T>
{
    public FieldKeyframe(float distance, T value)
    {
        this._distance = distance;
        this._value = value;
    }

    [SerializeField]
    private float _distance;
    [SerializeField]
    private T _value;

    public float Distance { get { return _distance; } set { _distance= value; } }
    public T Value { get { return _value; } set { _value = value; } }
    public abstract IFieldKeyframe<T> Clone();
    public abstract T Lerp(IFieldKeyframe<T> end,float time);
}
//We can't serialize generic fields because unity sux, so we gotta do this mess
[System.Serializable]
public class FloatFieldKeyframe : FieldKeyframe<float> {
    public FloatFieldKeyframe(float distance,float value):base (distance,value) { }

    public override IFieldKeyframe<float> Clone()
    {
        return new FloatFieldKeyframe(Distance,Value);
    }

    public override float Lerp(IFieldKeyframe<float> end, float time)
    {
        return Mathf.Lerp(Value, end.Value, time);
    }
}
