using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class KeyframeExtensionMethods
{
    public static float SegmentLength(this AnimationCurve curve,int index, PGIndex type)
    {
        switch (type)
        {
            case PGIndex.LeftTangent:
                return curve[index].time - curve[index - 1].time;
            case PGIndex.RightTangent:
                return curve[index + 1].time - curve[index].time;
            default:
                throw new System.ArgumentException();
        }
    }
    public static float GetKeyframeX(this AnimationCurve curve, int index,PGIndex type,bool GetRelativePosition=false)
    {
        float segmentLength;
        switch (type)
        {
            case PGIndex.Position:
                return curve[index].time;
            case PGIndex.LeftTangent:
                if (index <= 0)
                    throw new System.ArgumentException();
                segmentLength = SegmentLength(curve, index, PGIndex.LeftTangent);
                return (GetRelativePosition?0:curve[index].time) - segmentLength * curve[index].inWeight;
            case PGIndex.RightTangent:
                if (index >= curve.length - 1)
                    throw new System.ArgumentException();
                segmentLength = SegmentLength(curve,index,PGIndex.RightTangent);
                return (GetRelativePosition?0:curve[index].time) + segmentLength * curve[index].outWeight;
            default:
                throw new System.ArgumentException();
        }
    }
    public static float GetKeyframeY(this AnimationCurve curve,int index, PGIndex type,bool GetRelativePosition=false)
    {
        var relativeKeyframeX = GetKeyframeX(curve, index, type,true);
        switch (type)
        {
            case PGIndex.Position:
                return curve[index].value;
            case PGIndex.LeftTangent:
                return (GetRelativePosition?0:curve[index].value) + relativeKeyframeX * curve[index].inTangent;
            case PGIndex.RightTangent:
                return (GetRelativePosition?0:curve[index].value) + relativeKeyframeX * curve[index].outTangent;
            default:
                throw new System.ArgumentException();
        }
    }

    public static void SetKeyframeX(this AnimationCurve curve, int index, PGIndex type, float value, bool setRelativeValue = false)
    {
        var keys = curve.keys;
        var key = curve[index];
        if (setRelativeValue)
            value = value + key.time;
        var yBefore = GetKeyframeY(curve, index, type);
        float segmentLength;
        switch (type)
        {
            case PGIndex.Position:
                key.time = value;
                break;
            case PGIndex.LeftTangent:
                segmentLength = SegmentLength(curve, index, PGIndex.LeftTangent);
                key.inWeight = Mathf.Clamp01((key.time - value) / segmentLength);
                break;
            case PGIndex.RightTangent:
                segmentLength = SegmentLength(curve, index, PGIndex.RightTangent);
                key.outWeight = Mathf.Clamp01((value - key.time) / segmentLength);
                break;
        }
        keys[index] = key;
        curve.keys = keys;
        SetKeyframeY(curve,index,type,yBefore); 
    }

    public static void SetKeyframeY(this AnimationCurve curve, int index, PGIndex type, float value,bool setRelativeValue=false)
    {
        var keys = curve.keys;
        var key = curve[index];
        if (setRelativeValue)
            value = value + key.value;
        switch (type)
        {
            case PGIndex.Position:
                key.value = value;
                break;
            case PGIndex.LeftTangent:
                if (key.inWeight == 0)
                    key.inTangent = 0;//avoid divide by zero, we lose the tangent value if you set the weight to 0
                else
                    key.inTangent = (key.value - value) / (SegmentLength(curve, index, PGIndex.LeftTangent) * key.inWeight);
                break;
            case PGIndex.RightTangent:
                if (key.outWeight == 0)
                    key.outTangent = 0;//avoid divide by zero, we lose the tangent value if you set the weight to 0
                else
                    key.outTangent = (value-key.value) / (SegmentLength(curve,index,PGIndex.RightTangent)*key.outWeight);
                break;
        }
        keys[index] = key;
        curve.keys = keys;
    }
}
