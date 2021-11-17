#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class PositionPointClickCommand : IClickCommand
    {
        private PointGroup _group;
        private PointGroupIndex _index;
        private BezierCurve positionCurve;
        private List<BezierCurve> allCurves;
        private TransformBlob _transformBlob;
        public PositionPointClickCommand(PointGroup group,PointGroupIndex indexType,BezierCurve curve,TransformBlob transformBlob,List<BezierCurve> otherCurves)
        {
            this._group = group;
            this._index = indexType;
            this.positionCurve = curve;
            this._transformBlob = transformBlob;
            this.allCurves = otherCurves;
        }

        public void ClickDown(Vector2 mousePos,Curve3D curve,List<SelectableGUID> selected)
        {

        }

        public void ClickDrag(Vector2 mousePos,Curve3D curve,ClickHitData data,List<SelectableGUID> selected)
        {
            var dimensionLockMode = positionCurve.dimensionLockMode;
            var oldPointPosition = _group.GetPositionLocal(_index);
            Vector3 worldPos = Vector3.zero;
            bool shouldSet = true;
            if (dimensionLockMode== DimensionLockMode.none)
                worldPos = GUITools.GUIToWorldSpace(mousePos, data.distanceFromCamera);
            else
            {
                Vector3 planeNormal = Vector3.zero;
                switch (dimensionLockMode)
                {
                    case DimensionLockMode.x:
                        planeNormal = Vector3.right;
                        break;
                    case DimensionLockMode.y:
                        planeNormal = Vector3.up;
                        break;
                    case DimensionLockMode.z:
                        planeNormal = Vector3.forward;
                        break;
                }
                var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
                Ray r = sceneCam.ScreenPointToRay(GUITools.GuiSpaceToScreenSpace(mousePos));
                Vector3 localPos = oldPointPosition;
                Plane p = new Plane(_transformBlob.TransformDirection(planeNormal),_transformBlob.TransformPoint(localPos));
                if (p.Raycast(r, out float enter))
                    worldPos = r.GetPoint(enter);
                else
                    shouldSet = false;    
            }
            if (shouldSet)
            {
                var newPointPosition = _transformBlob.InverseTransformPoint(worldPos);
                Vector3 pointOffset = newPointPosition - oldPointPosition;
                List<PointGroup> selectedPointGroups = new List<PointGroup>();
                foreach (var currCurve in allCurves)
                {
                    HashSet<int> indiciesToRecalculate = new HashSet<int>();
                    int segmentIndex = 0;
                    foreach (var j in currCurve.PointGroups)
                    {
                        if (selected.Contains(j.GUID))
                        {
                            selectedPointGroups.Add(j);
                            AddIndexToRecalculate(indiciesToRecalculate, segmentIndex, currCurve, currCurve.automaticTangents);
                        }
                        segmentIndex++;
                    }
                    foreach (var i in selectedPointGroups)
                    {
                        i.SetPositionLocal(_index, i.GetPositionLocal(_index) + pointOffset);
                    }
                    currCurve.Recalculate(null, indiciesToRecalculate);
                    selectedPointGroups.Clear();
                }
            }
        }

        public static void AddIndexToRecalculate(HashSet<int> indiciesToRecalculate,int segmentIndex,BezierCurve currCurve,bool automaticTangents)
        {
            if (currCurve.isClosedLoop)
            {
                int lower = Utils.ModInt(segmentIndex - 1, currCurve.NumSegments);
                int upper = Utils.ModInt(segmentIndex, currCurve.NumSegments);
                indiciesToRecalculate.Add(lower);
                indiciesToRecalculate.Add(upper);
                if (automaticTangents)
                {
                    int nextBelow = Utils.ModInt(segmentIndex - 2, currCurve.NumSegments);
                    int nextAbove = Utils.ModInt(segmentIndex + 1, currCurve.NumSegments);
                    indiciesToRecalculate.Add(nextAbove);
                    indiciesToRecalculate.Add(nextBelow);
                }
            }
            else
            {
                int lower = segmentIndex - 1;
                int upper = segmentIndex;
                if (lower >= 0)
                {
                    indiciesToRecalculate.Add(lower);
                    if (automaticTangents)
                    {
                        int nextBelow = lower - 1;
                        if (nextBelow >= 0)
                        {
                            indiciesToRecalculate.Add(nextBelow);
                        }
                    }
                }
                if (upper < currCurve.NumSegments)
                {
                    indiciesToRecalculate.Add(upper);
                    if (automaticTangents)
                    {
                        int nextAbove = upper + 1;
                        if (nextAbove < currCurve.NumSegments)
                        {
                            indiciesToRecalculate.Add(nextAbove);
                        }
                    }
                }
            }
        }

        public void ClickUp(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selectedPoints)
        {
        }
    }
}
#endif
