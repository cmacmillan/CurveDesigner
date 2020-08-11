using Assets.NewUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public class AxisHandleDraw : IDraw
    {
        private AxisHandleComposite creator;
        private Curve3D curve;
        private Vector3 axis;
        private Vector3 position;
        private float _distFromCamera;
        public AxisHandleDraw(AxisHandleComposite creator,Curve3D curve,Vector3 axis,Vector3 position)
        {
            creator.GetHandleInfo(out Vector3 lineStart, out Vector3 lineEnd, out float handleSize,creator.drawLineStartOffset);
            GUITools.WorldToGUISpace(creator.HandleToWorldSpace(lineEnd), out Vector2 _guiPos, out _distFromCamera);
            this.creator = creator;
            this.curve = curve;
            this.axis = axis;
            this.position = position;
        }
        public IComposite Creator()
        {
            return creator;
        }

        public float DistFromCamera()
        {
            return _distFromCamera;
        }

        private const float coneSize = .2f;
        void DrawDirectionHandle(Color color, Vector3 direction, Vector3 ortho)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Handles.color = color;
            creator.GetHandleInfo(out Vector3 lineStart, out Vector3 lineEnd, out float handleSize,creator.drawLineStartOffset);
            Handles.DrawLine(lineStart,lineEnd);
            Handles.ConeHandleCap(-1, lineEnd, Quaternion.LookRotation(direction, ortho), handleSize*coneSize, Event.current.type);
        }
        public void Draw(DrawMode mode, SelectionState selectionState)
        {
            if (axis == Vector3.forward)
                DrawDirectionHandle(mode.Tint(selectionState,Color.blue), Vector3.forward, Vector3.up);
            else if (axis == Vector3.up)
                DrawDirectionHandle(mode.Tint(selectionState,Color.green), Vector3.up, Vector3.forward);
            else if (axis == Vector3.right)
                DrawDirectionHandle(mode.Tint(selectionState,Color.red), Vector3.right, Vector3.up);
            else
                throw new System.ArgumentException("Direction must be axis aligned");
        }
    }
}
