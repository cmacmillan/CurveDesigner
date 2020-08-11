using Assets.NewUI;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public class AxisHandleComposite : IClickable
    {
        private Curve3D curve;
        private Vector3 axis;
        private IPositionProvider positionProvider;
        public AxisHandleComposite(IComposite parent,Curve3D curve,Vector3 axis,IPositionProvider positionProvider) : base(parent)
        {
            this.curve = curve;
            this.axis = axis;
            this.positionProvider = positionProvider;
        }
        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            drawList.Add(new AxisHandleDraw(this, curve, axis,positionProvider.Position));
        }
        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits, EventType eventType)
        {
            GetHandleInfo(out Vector3 lineStart, out Vector3 lineEnd, out float handleSize,clickLineStartOffset);
            GUITools.WorldToGUISpace(HandleToWorldSpace(lineEnd),out Vector2 guiPosition,out float screenDepth);
            clickHits.Add(new ClickHitData(this,screenDepth,guiPosition-mousePosition));
        }
        float coneSizeMultiplier = .2f;
        float lineLength = .6f;

        public float drawLineStartOffset = .06f;
        public float clickLineStartOffset = .2f;

        private Vector4 ToHomo(Vector3 v) { return new Vector4(v.x, v.y, v.z, 1); }
        public Vector3 WorldToHandleSpace(Vector3 v) { return Handles.matrix* ToHomo(v); }
        public Vector3 HandleToWorldSpace(Vector3 v) { return Handles.inverseMatrix* ToHomo(v); }
        public void GetHandleInfo(out Vector3 lineStart, out Vector3 lineEnd, out float handleSize,float lineStartOffset)
        {
            Vector3 position = positionProvider.Position;
            var basePos = WorldToHandleSpace(position);
            handleSize = HandleUtility.GetHandleSize(basePos);
            lineStart = WorldToHandleSpace(position + lineStartOffset * handleSize*axis);
            lineEnd = WorldToHandleSpace(position + lineLength * handleSize*axis);
        }
        public override float DistanceFromMouse(Vector2 mouse)
        {
            GetHandleInfo(out Vector3 lineStart, out Vector3 lineEnd, out float handleSize,clickLineStartOffset);
            float lineDist = HandleUtility.DistanceToLine(HandleToWorldSpace(lineStart),HandleToWorldSpace(lineEnd));
            float coneDist = HandleUtility.DistanceToCircle(HandleToWorldSpace(lineEnd),handleSize*coneSizeMultiplier);
            return Mathf.Min(lineDist, coneDist);
        }

        public override IClickCommand GetClickCommand()
        {
            return new AxisHandleClickCommand();
        }
    }
    public class AxisHandleClickCommand : IClickCommand
    {
        public void ClickDown(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected)
        {
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked, List<SelectableGUID> selected)
        {
        }

        public void ClickUp(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected)
        {
        }
    }
}
