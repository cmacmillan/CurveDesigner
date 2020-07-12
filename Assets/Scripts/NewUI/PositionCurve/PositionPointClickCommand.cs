﻿using System;
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
        private TransformBlob _transformBlob;
        public PositionPointClickCommand(PointGroup group,PGIndex indexType,BezierCurve curve,TransformBlob transformBlob)
        {
            this._group = group;
            this._index = indexType;
            this.positionCurve = curve;
            this._transformBlob = transformBlob;
        }

        public void ClickDown(Vector2 mousePos,Curve3D curve,List<SelectableGUID> selected)
        {
        }

        public void ClickDrag(Vector2 mousePos,Curve3D curve,ClickHitData data,List<SelectableGUID> selected)
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
                var selectedPointGroups = selected.GetSelected(positionCurve.PointGroups);
                foreach (var i in selectedPointGroups)
                    i.SetWorldPositionByIndex(_index, newPointPosition, dimensionLockMode);

            }
        }

        public void ClickUp(Vector2 mousePos,Curve3D curve, List<SelectableGUID> selectedPoints)
        {
        }
    }
}
