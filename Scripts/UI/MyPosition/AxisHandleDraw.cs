#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class AxisHandleDraw : IDraw
    {
        private AxisHandleComposite creator;
        private Curve3D curve;
        private Vector3 axis;
        private Vector3 position;
        private float _distFromCamera;
        private float alpha;
        public AxisHandleDraw(AxisHandleComposite creator,Curve3D curve,Vector3 axis,Vector3 position,float alpha)
        {
            creator.GetHandleInfo(out Vector3 lineStart, out Vector3 lineEnd, out float handleSize,creator.drawLineStartOffset);
            GUITools.WorldToGUISpace(creator.HandleToWorldSpace(lineEnd), out Vector2 _guiPos, out _distFromCamera);
            this.creator = creator;
            this.curve = curve;
            this.axis = axis;
            this.position = position;
            this.alpha = alpha;
        }
        public Composite Creator()
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
            color.a *= alpha;
            Handles.color = color;
            creator.GetHandleInfo(out Vector3 lineStart, out Vector3 lineEnd, out float handleSize,creator.drawLineStartOffset);
            Handles.DrawLine(lineStart,lineEnd);
            Handles.ConeHandleCap(-1, lineEnd, Quaternion.LookRotation(direction, ortho), handleSize*coneSize, Event.current.type);
        }
        public void Draw(DrawMode mode, SelectionState selectionState)
        {
            Color Tint(Color c)
            {
                if (mode == DrawMode.hovered)
                    return mode.Tint(selectionState, c);
                else if (mode == DrawMode.clicked)
                    return mode.Tint(selectionState, c);
                return c;
            }
            if (axis == Vector3.forward)
                DrawDirectionHandle(Tint(Color.blue), Vector3.forward, Vector3.up);
            else if (axis == Vector3.up)
                DrawDirectionHandle(Tint(Color.green), Vector3.up, Vector3.forward);
            else if (axis == Vector3.right)
                DrawDirectionHandle(Tint(Color.red), Vector3.right, Vector3.up);
            else
                throw new System.ArgumentException("Direction must be axis aligned");
        }
    }
}
#endif
