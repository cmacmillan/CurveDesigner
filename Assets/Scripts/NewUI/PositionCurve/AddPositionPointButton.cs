using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class AddPositionPointButton : IComposite, IPositionProvider
    {
        private PointComposite PlusButtonPoint;
        private bool isPrepend;
        private BezierCurve positionCurve;
        private TransformBlob transformBlob;
        public AddPositionPointButton(IComposite parent,Curve3D curve,BezierCurve positionCurve,bool isPrepend,TransformBlob transformBlob) : base(parent)
        {
            PlusButtonPoint = new PointComposite(this, this,  PointTextureType.plus, new DoNothingClickCommand(), Color.green, SelectableGUID.Null,false,5);
            this.transformBlob = transformBlob;
            this.positionCurve = positionCurve;
            this.isPrepend = isPrepend;
        }

        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            var centerPos = transformBlob.TransformPoint(GetPointGroup().GetWorldPositionByIndex(PGIndex.Position, positionCurve.dimensionLockMode));
            drawList.Add(new LineDraw(this, centerPos, Position, Color.grey));
            base.Draw(drawList, closestElementToCursor);
        }
        public override IEnumerable<IComposite> GetChildren()
        {
            yield return PlusButtonPoint;
        }

        private const float guiOffsetPixelAmount = 20;
        private PointGroup GetPointGroup()
        {
            if (isPrepend)
                return positionCurve.PointGroups[0];
            else 
                return positionCurve.PointGroups.Last();
        }
        private PGIndex GetIndex()
        {
            if (isPrepend)
                return PGIndex.LeftTangent;
            else
                return PGIndex.RightTangent;
        }
        public Vector3 Position
        {
            get
            {
                PointGroup pointGroup = GetPointGroup();
                PGIndex index = GetIndex();
                var centerPos = transformBlob.TransformPoint(pointGroup.GetWorldPositionByIndex(PGIndex.Position, positionCurve.dimensionLockMode));
                var offsetPos = transformBlob.TransformPoint(pointGroup.GetWorldPositionByIndex(index, positionCurve.dimensionLockMode));
                GUITools.WorldToGUISpace(centerPos, out Vector2 centerGuiPos, out float centerScreenDepth);
                GUITools.WorldToGUISpace(offsetPos, out Vector2 offsetGuiPos, out float offsetScreenDepth);
                var diff = offsetGuiPos - centerGuiPos;
                var direction = diff.normalized*guiOffsetPixelAmount;
                var factor = direction.magnitude / diff.magnitude;
                var depthDiff = offsetScreenDepth - centerScreenDepth;
                var depthDirection = depthDiff * factor;
                return GUITools.GUIToWorldSpace(centerGuiPos+direction, centerScreenDepth + depthDirection);
            }
        }
    }
    public class AddPositionPointClickCommand : IClickCommand
    {
        public AddPositionPointClickCommand(Curve3D curve)
        {

        }
        public void ClickDown(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected)
        {
            throw new NotImplementedException();
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData clicked, List<SelectableGUID> selected)
        {
            throw new NotImplementedException();
        }

        public void ClickUp(Vector2 mousePos, Curve3D curve, List<SelectableGUID> selected)
        {
            throw new NotImplementedException();
        }
    }
}
