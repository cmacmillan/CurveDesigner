using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PositionPointClickCommand : IClickCommand
    {
        private PointGroup _group;
        private PGIndex _index;
        private BezierCurve positionCurve;
        public PositionPointClickCommand(PointGroup group,PGIndex indexType,BezierCurve curve)
        {
            this._group = group;
            this._index = indexType;
            this.positionCurve = curve;
        }
        public void ClickDown(Vector2 mousePos)
        {
        }

        public void ClickDrag(Vector2 mousePos,Curve3D curve,ClickHitData data)
        {
            var dimensionLockMode = positionCurve.dimensionLockMode;
            var oldPointPosition = _group.GetWorldPositionByIndex(_index,dimensionLockMode);
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
                Plane p = new Plane(planeNormal,curve.transform.TransformPoint(localPos));
                if (p.Raycast(r, out float enter))
                    worldPos = r.GetPoint(enter);
                else
                    shouldSet = false;    
            }
            if (shouldSet)
            {
                var newPointPosition = curve.transform.InverseTransformPoint(worldPos);
                _group.SetWorldPositionByIndex(_index, newPointPosition, dimensionLockMode);
            }
        }

        public void ClickUp(Vector2 mousePos)
        {
        }
    }
}
