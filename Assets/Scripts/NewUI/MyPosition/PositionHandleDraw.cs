using Assets.NewUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.NewUI.MyPosition
{
    public class PositionHandleDraw : IDraw
    {
        private IComposite creator;
        private Curve3D curve;
        private Vector3 direction;
        private float _distFromCamera;
        public PositionHandleDraw(IComposite creator,Curve3D curve,Vector3 direction,Vector3 position)
        {
            GUITools.WorldToGUISpace(position, out Vector2 _guiPos, out _distFromCamera);
            this.creator = creator;
            this.curve = curve;
            this.direction = direction;
        }
        public IComposite Creator()
        {
            return creator;
        }

        public float DistFromCamera()
        {
            return _distFromCamera;
        }

        void DrawDirectionHandle(Color color, Vector3 direction, Vector3 ortho)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Handles.color = color;
            float size;
            Vector3 initialPos = curve.transform.TransformPoint(curve.positionCurve.PointGroups[0].GetWorldPositionByIndex(PGIndex.Position, DimensionLockMode.none));
            var initialHandleSpace = Handles.matrix * new Vector4(initialPos.x, initialPos.y, initialPos.z, 1);
            float offsetAmount = 3f;
            size = HandleUtility.GetHandleSize(initialHandleSpace) * .2f;
            var offsetInitialPos = initialPos + size * direction * offsetAmount;
            var finalHandleSpace = Handles.matrix * new Vector4(offsetInitialPos.x, offsetInitialPos.y, offsetInitialPos.z, 1);
            Handles.DrawLine(initialHandleSpace, finalHandleSpace);
            Handles.ConeHandleCap(-1, finalHandleSpace, Quaternion.LookRotation(direction, ortho), size, Event.current.type);
        }
        public void Draw(DrawMode mode, SelectionState selectionState)
        {
            if (direction == Vector3.forward)
                DrawDirectionHandle(Color.blue, Vector3.forward, Vector3.up);
            else if (direction == Vector3.up)
                DrawDirectionHandle(Color.green, Vector3.up, Vector3.forward);
            else if (direction == Vector3.right)
                DrawDirectionHandle(Color.red, Vector3.right, Vector3.up);
            else
                throw new System.ArgumentException("Direction must be axis aligned");
        }
    }
}
