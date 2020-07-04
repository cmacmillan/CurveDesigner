using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public interface IWindowDrawer
    {
        void DrawWindow(Curve3D curve);
    }
    public static class WindowDrawer
    {
        public static void Draw(FloatLinearDistanceSampler sampler,string fieldName, Curve3D curve)
        {
            List<int> selectedPoints = curve.selectedPoints;
            if (selectedPoints.Count == 0)
                return;
            var primaryPointIndex = selectedPoints.Last();
            sampler.CacheOpenCurvePoints(curve.positionCurve);
            var points = sampler.GetPoints(curve);
            var primaryPoint = points[selectedPoints.Last()];
            EditorGUI.BeginChangeCheck();
            primaryPoint.value = EditorGUILayout.FloatField(fieldName, primaryPoint.value);
            primaryPoint.SetDistance(EditorGUILayout.FloatField("Distance along curve", primaryPoint.GetDistance(curve.positionCurve)), curve.positionCurve);
            if (EditorGUI.EndChangeCheck())
            {
                curve.RequestMeshUpdate();
            }
        }
    }
}
