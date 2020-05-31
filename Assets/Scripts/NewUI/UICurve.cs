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
        public PositionCurveComposite positionCurve;
        public SizeCurveComposite sizeCurve;
        public RotationCurveComposite rotationCurve;
        public DoubleBezierCurveComposite doubleBezierCurve;
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
            positionCurve = new PositionCurveComposite(this,_curve,_curve.positionCurve);
            sizeCurve = new SizeCurveComposite(this,_curve.sizeDistanceSampler,_curve);
            rotationCurve = new RotationCurveComposite(this,_curve.rotationDistanceSampler,_curve);
            doubleBezierCurve = new DoubleBezierCurveComposite(this, _curve.doubleBezierSampler, _curve);
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

        public static void GetCurveDraw(List<IDraw> drawList,BezierCurve curve, Transform transform, IComposite owner)
        {
            for (int i = 0; i < curve.NumSegments; i++)
            {
                var point1 = transform.TransformPoint(curve[i, 0]);
                var point2 = transform.TransformPoint(curve[i, 3]);
                var tangent1 = transform.TransformPoint(curve[i, 1]);
                var tangent2 = transform.TransformPoint(curve[i, 2]);
                drawList.Add(new CurveSegmentDraw(owner, point1, point2, tangent1, tangent2, LineTextureType.Default, new Color(.6f, .6f, .6f)));
            }
        }

        public override void Draw(List<IDraw> drawList,ClickHitData clickedElement)
        {
            _curve.positionCurve.Recalculate();
            FindPointClosestToCursor();
            if (_curve.drawNormals)
            {
                float sampleDist = _curve.GetNormalDensityDistance();
                List<PointOnCurve> points = _curve.positionCurve.GetPointsWithSpacing(sampleDist);
                var visualNormalLength = _curve.VisualNormalsLength();
                var curveLength = _curve.positionCurve.GetLength();
                foreach (var i in points)
                {
                    var rotation = _curve.rotationDistanceSampler.GetValueAtDistance(i.distanceFromStartOfCurve, _curve.isClosedLoop, curveLength,_curve.positionCurve)+_curve.curveRotation;
                    var reference = Quaternion.AngleAxis(rotation, i.tangent) * i.reference;
                    drawList.Add(new LineDraw(this, _curve.transform.TransformPoint(i.position), _curve.transform.TransformPoint(reference * visualNormalLength+ i.position), Color.yellow));
                }
            }
            GetCurveDraw(drawList,_curve.positionCurve,_curve.transform,this);
            base.Draw(drawList,clickedElement);
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            switch (_curve.editMode)
            {
                case EditMode.PositionCurve:
                    yield return positionCurve;
                    break;
                case EditMode.Size:
                    yield return sizeCurve;
                    break;
                case EditMode.Rotation:
                    yield return rotationCurve;
                    break;
                case EditMode.DoubleBezier:
                    yield return doubleBezierCurve;
                    break;
            }
        }
    }
}
