using ChaseMacMillan.CurveDesigner;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessRunnerGameManager : MonoBehaviour
{
    public Curve3D initialCurve;
    public List<Curve3D> possibleCurveSegments;
    private void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            initialCurve.AppendCurve(possibleCurveSegments[Random.Range(0,possibleCurveSegments.Count)]);
        }
    }
}
