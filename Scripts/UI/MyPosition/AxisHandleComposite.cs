#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class AxisHandleComposite : Clickable
    {
        private Curve3D curve;
        private Vector3 axis;
        private IPosition positionProvider;
        private AxisHandleClickCommand clickCommand;
        public const float axisMaxDotProduct = .95f;
        public const float axisStartFadeDotProduct= .9f;
        public AxisHandleComposite(Composite parent,Curve3D curve,Vector3 axis,IPosition positionProvider) : base(parent)
        {
            this.curve = curve;
            this.axis = axis;
            this.positionProvider = positionProvider;
            this.clickCommand = new AxisHandleClickCommand(positionProvider,axis);
        }
        public float GetAxisDot()
        {
            Vector3 cameraWorldPos = SceneView.lastActiveSceneView.camera.transform.position;//should really use currently rendering scene camera rather than last active
            float dot = Mathf.Abs(Vector3.Dot((cameraWorldPos-positionProvider.Position).normalized, axis));
            return dot;
        }
        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            float dot = GetAxisDot();
            float fadeWidth = axisMaxDotProduct - axisStartFadeDotProduct;
            if (dot < axisMaxDotProduct)
            {
                float alpha = 1-Mathf.Clamp01((dot - axisStartFadeDotProduct) / fadeWidth);
                drawList.Add(new AxisHandleDraw(this, curve, axis,positionProvider.Position,alpha));
            }
        }
        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            var dot = GetAxisDot();
            if (dot < axisMaxDotProduct)
            {
                GetHandleInfo(out Vector3 lineStart, out Vector3 lineEnd, out float handleSize, clickLineStartOffset);
                GUITools.WorldToGUISpace(HandleToWorldSpace(lineEnd), out Vector2 guiPosition, out float screenDepth);
                clickHits.Add(new ClickHitData(this, screenDepth, guiPosition - mousePosition));
            }
        }
        float coneSizeMultiplier = .2f;
        float lineLength = .6f;

        public float drawLineStartOffset = .06f;
        public float clickLineStartOffset = .2f;

        private Vector4 ToHomo(Vector3 v) { return new Vector4(v.x, v.y, v.z, 1); }
        public Vector3 WorldToHandleSpace(Vector3 v) { return Handles.matrix*ToHomo(v); }
        public Vector3 HandleToWorldSpace(Vector3 v) { return Handles.inverseMatrix*ToHomo(v); }
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
            var worldSpaceEnd = HandleToWorldSpace(lineEnd);
            float lineDist = HandleUtility.DistanceToLine(HandleToWorldSpace(lineStart),worldSpaceEnd);
            float coneDist = HandleUtility.DistanceToCircle(worldSpaceEnd,handleSize*coneSizeMultiplier);
            return Mathf.Min(lineDist, coneDist);
        }

        public override IClickCommand GetClickCommand()
        {
            return clickCommand;
        }
    }
    public class AxisHandleClickCommand : IClickCommand
    {
        private IPosition position;
        private Vector3 axis;
        public AxisHandleClickCommand(IPosition position,Vector3 axis)
        {
            this.position = position;
            this.axis = axis;
        }
        private Vector3 startPosition;
        private Vector2 startMousePosition;
        public void ClickDown(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected)
        {
            startPosition = position.Position;
            startMousePosition = mousePos;
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked, List<SelectableGUID> selected)
        {
            float dist = HandleUtility.CalcLineTranslation(startMousePosition, mousePos, startPosition, axis);
            Vector3 worldPosition = startPosition+ axis* dist;
            position.SetPosition(worldPosition,selected);
        }

        public void ClickUp(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected)
        {
        }
    }
}
#endif
