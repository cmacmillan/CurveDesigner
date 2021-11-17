#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class AddPositionPointButton : Composite, IPositionProvider
    {
        private PointComposite PlusButtonPoint;
        private bool isPrepend;
        private BezierCurve positionCurve;
        private TransformBlob transformBlob;
        public AddPositionPointButton(Composite parent,Curve3D curve,BezierCurve positionCurve,bool isPrepend,TransformBlob transformBlob,PositionCurveComposite curveComposite,int secondaryCurveIndex) : base(parent)
        {
            this.transformBlob = transformBlob;
            this.positionCurve = positionCurve;
            this.isPrepend = isPrepend;
            PlusButtonPoint = new PointComposite(this, this,  PointTextureType.plus, new AddPositionPointClickCommand(isPrepend,positionCurve,this,secondaryCurveIndex,transformBlob), Color.green, SelectableGUID.Null,false,5);
        }

        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            var centerPos = transformBlob.TransformPoint(GetPointGroup().GetPositionLocal(PointGroupIndex.Position));
            drawList.Add(new LineDraw(this, centerPos, Position, Color.white));
            base.Draw(drawList, closestElementToCursor);
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return PlusButtonPoint;
        }

        private const float guiOffsetPixelAmount = 40;
        private PointGroup GetPointGroup()
        {
            if (isPrepend)
                return positionCurve.PointGroups[0];
            else 
                return positionCurve.PointGroups.Last();
        }
        private PointGroupIndex GetIndex()
        {
            //backwards, then we reflect
            if (isPrepend)
                return PointGroupIndex.RightTangent;
            else
                return PointGroupIndex.LeftTangent;
        }
        public Vector3 Position
        {
            get
            {
                PointGroup pointGroup = GetPointGroup();
                PointGroupIndex index = GetIndex();
                var centerPos = transformBlob.TransformPoint(pointGroup.GetPositionLocal(PointGroupIndex.Position,true));
                var offsetPos = transformBlob.TransformPoint(pointGroup.GetPositionLocal(index,true));
                GUITools.WorldToGUISpace(centerPos, out Vector2 centerGuiPos, out float centerScreenDepth);
                bool needFlip = !GUITools.WorldToGUISpace(offsetPos, out Vector2 offsetGuiPos, out float offsetScreenDepth);
                if (needFlip)
                    offsetGuiPos = 2 * centerGuiPos - offsetGuiPos;
                var diff = offsetGuiPos - centerGuiPos;
                var direction = diff.normalized*guiOffsetPixelAmount;
                var factor = direction.magnitude / diff.magnitude;
                var depthDiff = offsetScreenDepth - centerScreenDepth;
                var depthDirection = depthDiff * factor;
                return GUITools.GUIToWorldSpace(centerGuiPos+direction, centerScreenDepth + depthDirection);
            }
        }
    }
}
#endif
