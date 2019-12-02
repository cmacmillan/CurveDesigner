using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//Scripts from the unity reference source that have been tweaked slightly
//I didn't write these
public static class UnitySourceScripts
{
    // Get the nearest 3D point.
    public static Vector3 ClosestPointToPolyLine(out int segmentIndex,out float time,List<BeizerCurve.SampleFragment> vertices)
    {
        float dist = HandleUtility.DistanceToLine(vertices[0].position, vertices[1].position);
        int nearest = 0;// Which segment we're closest to
        for (int i = 2; i < vertices.Count; i++)
        {
            float d = HandleUtility.DistanceToLine(vertices[i - 1].position, vertices[i].position);
            if (d < dist)
            {
                dist = d;
                nearest = i - 1;
            }
        }

        Vector3 lineStart = vertices[nearest].position;
        Vector3 lineEnd = vertices[nearest + 1].position;

        Vector2 relativePoint = Event.current.mousePosition - HandleUtility.WorldToGUIPoint(lineStart);
        Vector2 lineDirection = HandleUtility.WorldToGUIPoint(lineEnd) - HandleUtility.WorldToGUIPoint(lineStart);
        float length = lineDirection.magnitude;
        float dot = Vector3.Dot(lineDirection, relativePoint);
        if (length > .000001f)
            dot /= length * length;
        dot = Mathf.Clamp01(dot);

        var pointA = vertices[nearest];
        var pointB = vertices[nearest + 1];
        float lower = pointA.time;
        float higher;
        if (pointA.segmentIndex == pointB.segmentIndex)
            higher = pointB.time;
        else
            higher = 1.0f;
        time = Mathf.Lerp(lower,higher,dot);
        segmentIndex = pointA.segmentIndex;

        return Vector3.Lerp(lineStart, lineEnd, dot);
    }
}
