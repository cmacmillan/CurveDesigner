using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestKeyFrameExtensionMethod : MonoBehaviour
{
    public AnimationCurve curve;

    public Vector2 position1;
    public Vector2 position2;
    public Vector2 position3;
    public Vector2 position4;

    [ContextMenu("printdata")]
    public void PrintData()
    {
        Debug.Log($"X:{curve.GetKeyframeX(0, PGIndex.Position)} Y:{curve.GetKeyframeY(0, PGIndex.Position)}");
        Debug.Log($"X:{curve.GetKeyframeX(0, PGIndex.RightTangent)} Y:{curve.GetKeyframeY(0, PGIndex.RightTangent)}");
        Debug.Log($"X:{curve.GetKeyframeX(1, PGIndex.LeftTangent)} Y:{curve.GetKeyframeY(1, PGIndex.LeftTangent)}");
        Debug.Log($"X:{curve.GetKeyframeX(1, PGIndex.Position)} Y:{curve.GetKeyframeY(1, PGIndex.Position)}");
    }
    [ContextMenu("setdata")]
    public void SetData()
    {
        //1
        curve.SetKeyframeX(0,PGIndex.Position,position1.x);
        curve.SetKeyframeY(0,PGIndex.Position,position1.y);
        //2
        curve.SetKeyframeX(0,PGIndex.RightTangent,position2.x);
        curve.SetKeyframeY(0,PGIndex.RightTangent,position2.y);
        //3
        curve.SetKeyframeX(1,PGIndex.LeftTangent,position3.x);
        curve.SetKeyframeY(1,PGIndex.LeftTangent,position3.y);
        //4
        curve.SetKeyframeX(1,PGIndex.Position,position4.x);
        curve.SetKeyframeY(1,PGIndex.Position,position4.y);
    }
}
