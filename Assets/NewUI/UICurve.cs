using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.NewUI
{
    public class UICurve : IComposite
    {
        private PositionCurveComposite _positionCurve;
        private SizeCurveComposite _sizeCurve;
        private Curve3D _curve;
        public PointOnCurve pointClosestToCursor;

        public UICurve(IComposite parent,Curve3D curve) : base(parent)
        {
            Undo.undoRedoPerformed -= Initialize;
            Undo.undoRedoPerformed += Initialize;
            this._curve = curve;
            Initialize();
        }

        public void Initialize()
        {
            _positionCurve = new PositionCurveComposite(this,_curve);
            _sizeCurve = new SizeCurveComposite(this,_curve.sizeDistanceSampler,_curve);
            _curve.lastMeshUpdateStartTime = DateTime.Now;
            _curve.positionCurve.Recalculate();
        }

        public void FindPointClosestToCursor()
        {
            var samples = _curve.positionCurve.GetSamplePoints();
            foreach (var i in samples)
                i.position = _curve.transform.TransformPoint(i.position);
            int segmentIndex;
            float time;
            UnitySourceScripts.ClosestPointToPolyLine(out segmentIndex, out time, samples);
            foreach (var i in samples)
                i.position = _curve.transform.InverseTransformPoint(i.position);
            float distance = _curve.positionCurve.GetDistanceAtSegmentIndexAndTime(segmentIndex,time);
            pointClosestToCursor = _curve.positionCurve.GetPointAtDistance(distance);
        }

        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            _curve.positionCurve.Recalculate();
            FindPointClosestToCursor();
            base.Click(mousePosition, clickHits);
        }

        public override void Draw(List<IDraw> drawList,ClickHitData clickedElement)
        {
            _curve.positionCurve.Recalculate();
            FindPointClosestToCursor();
            if (_curve.drawNormals)
            {
                float sampleDist = _curve.GetNormalDensityDistance();
                List<PointOnCurve> points = _curve.positionCurve.GetPointsWithSpacing(sampleDist);
                foreach (var i in points)
                {
                    var reference = Quaternion.AngleAxis(_curve.curveRotation, i.tangent) * i.reference;
                    drawList.Add(new LineDraw(this, i.position, reference * _curve.normalsLength + i.position, Color.yellow));
                }
            }
            for (int i = 0; i < _curve.positionCurve.NumSegments; i++)
            {
                var point1 = _curve.transform.TransformPoint(_curve.positionCurve[i, 0]);
                var point2 = _curve.transform.TransformPoint(_curve.positionCurve[i, 3]);
                var tangent1 = _curve.transform.TransformPoint(_curve.positionCurve[i, 1]);
                var tangent2 = _curve.transform.TransformPoint(_curve.positionCurve[i, 2]);
                drawList.Add(new CurveSegmentDraw(this,point1,point2,tangent1,tangent2,LineTextureType.Default,new Color(.6f, .6f, .6f)));
            }
            base.Draw(drawList,clickedElement);
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            if (_curve.editMode == EditMode.PositionCurve)
                yield return _positionCurve;
            else if (_curve.editMode == EditMode.Size)
                yield return _sizeCurve;
        }
    }
}
