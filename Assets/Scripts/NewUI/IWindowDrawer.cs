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
        public static void GenericDraw(IEnumerable<ISelectable> selectables,Curve3D curve)
        {
            List<SelectableGUID> selectedPoints = curve.selectedPoints;
            if (selectedPoints.Count == 0)
                return;
            var primaryPointIndex = selectedPoints.First();
            ////
            //sampler.CacheOpenCurvePoints(curve.positionCurve);
            //var points = sampler.GetPoints(curve);
            /////
            var primaryPoint = selectables.Where(a=>a.GUID==selectedPoints[0]).FirstOrDefault();
            if (primaryPoint == null)
                return;
            EditorGUI.BeginChangeCheck();
            primaryPoint.SelectEdit(curve);
            if (EditorGUI.EndChangeCheck())
            {
                curve.RequestMeshUpdate();
            }           
        }
        /*
        public static void Draw(FloatLinearDistanceSampler sampler,string fieldName, Curve3D curve)
        {
            List<SelectableGUID> selectedPoints = curve.selectedPoints;
            if (selectedPoints.Count == 0)
                return;
            var primaryPointIndex = selectedPoints.First();
            ////
            sampler.CacheOpenCurvePoints(curve.positionCurve);
            var points = sampler.GetPoints(curve);
            /////
            var primaryPoint = points.Where(a=>a.GUID==selectedPoints[0]).FirstOrDefault();
            if (primaryPoint == null)
                return;
            EditorGUI.BeginChangeCheck();
            primaryPoint.value = EditorGUILayout.FloatField(fieldName, primaryPoint.value);
            primaryPoint.SetDistance(EditorGUILayout.FloatField("Distance along curve", primaryPoint.GetDistance(curve.positionCurve)), curve.positionCurve);
            if (EditorGUI.EndChangeCheck())
            {
                curve.RequestMeshUpdate();
            }
        }
        public static void Draw(BezierCurve positionCurve, Curve3D curve)
        {
            List<SelectableGUID> selectedPoints = curve.selectedPoints;
            if (selectedPoints.Count == 0)
                return;           
            var primaryPointIndex = selectedPoints.First();
            ///
            var points = positionCurve.PointGroups;
            var primaryPoint = points.Where(a=>a.GUID==selectedPoints[0]).FirstOrDefault();
            ///
            if (primaryPoint == null)
                return;
            EditorGUI.BeginChangeCheck();
            var dimensionLockMode = curve.lockToPositionZero;
            primaryPoint.SetWorldPositionByIndex(PGIndex.Position,EditorGUILayout.Vector3Field("Position", primaryPoint.GetWorldPositionByIndex(PGIndex.Position,dimensionLockMode)),dimensionLockMode);
            if (EditorGUI.EndChangeCheck())
            {
                curve.RequestMeshUpdate();
            }
        }
        */
    }
}
