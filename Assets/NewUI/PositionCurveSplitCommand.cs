using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PositionCurveSplitCommand : IClickCommand
    {
        private Curve3D _curve;
        private SplitterPointComposite _splitter;
        public PositionCurveSplitCommand(Curve3D curve,SplitterPointComposite splitter)
        {
            this._curve = curve;
            this._splitter = splitter;
        }
        public void ClickDown(Vector2 mousePos)
        {
            _curve.positionCurve.InsertSegmentAfterIndex(_splitter._splitPoint,_curve.positionCurve.placeLockedPoints,_curve.positionCurve.splitInsertionBehaviour);
        }

        public void ClickDrag(Vector2 mousePos, Curve3D curve, ClickHitData data)
        {
            throw new NotImplementedException();
        }

        public void ClickUp(Vector2 mousePos)
        {
            throw new NotImplementedException();
        }
    }
    public class PositionCurveSplitCommandFactory : ISplitCommandFactory
    {
        //factory should be a singleton
        private static PositionCurveSplitCommandFactory _instance = null;
        public static PositionCurveSplitCommandFactory Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PositionCurveSplitCommandFactory();
                return _instance;
            }
        }
        public IClickCommand Create(SplitterPointComposite owner, Curve3D curve)
        {
            return new PositionCurveSplitCommand(curve,owner);
        }
    }
}
