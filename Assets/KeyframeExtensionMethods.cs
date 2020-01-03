using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class KeyframeExtensionMethods
{
    public static float GetKeyFrameX(this AnimationCurve curve, int index,PGIndex type,bool GetRelativePosition=false)
    {
        float segmentLength;
        switch (type)
        {
            case PGIndex.Position:
                return curve[index].time;
            case PGIndex.LeftTangent:
                if (index <= 0)
                    throw new System.ArgumentException();
                segmentLength = curve[index].time - curve[index - 1].time;
                return (GetRelativePosition?0:curve[index].time) - segmentLength * curve[index].inWeight;
            case PGIndex.RightTangent:
                if (index >= curve.length - 1)
                    throw new System.ArgumentException();
                segmentLength = curve[index+1].time - curve[index].time;
                return (GetRelativePosition?0:curve[index].time) + segmentLength * curve[index].inWeight;
            default:
                throw new System.ArgumentException();
        }
    }
    public static float GetKeyframeY(this AnimationCurve curve,int index, PGIndex type)
    {
        var relativeKeyframeX = GetKeyFrameX(curve, index, type,true);
        switch (type)
        {
            case PGIndex.Position:
                return curve[index].value;
            case PGIndex.LeftTangent:
            case PGIndex.RightTangent:
                return curve[index].value + relativeKeyframeX * curve[index].inTangent;
            default:
                throw new System.ArgumentException();
        }
    }

    public static void SetKeyframeY(this AnimationCurve curve, int index, PGIndex type, float value)
    {
        var keys = curve.keys;
        var key = curve[index];
        switch (type)
        {
            case PGIndex.Position:
                key.value = value;
                break;
        }
        keys[index] = key;
        curve.keys = keys;
    }
    /*public static float GetY(this Keyframe frame)
    {
    }
    public static float SetX(this Keyframe frame)
    {
    }
    public static float SetY(this Keyframe frame)
    {
    }*/
}
