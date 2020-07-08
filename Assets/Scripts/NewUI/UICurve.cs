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

        public IWindowDrawer GetWindowDrawer()
        {
            switch (_curve.editMode)
            {
                case EditMode.DoubleBezier:
                    return doubleBezierCurve;
                case EditMode.PositionCurve:
                    return positionCurve;
                case EditMode.Rotation:
                    return rotationCurve;
                case EditMode.Size:
                    return sizeCurve;
                default:
                    throw new NotImplementedException($"Case {_curve.editMode} not defined in switch statement");
            }
        }

        private Curve3D _curve;

        public override SelectableGUID GUID => SelectableGUID.Null;

        public UICurve(IComposite parent,Curve3D curve) : base(parent)
        {
            Undo.undoRedoPerformed -= Initialize;
            Undo.undoRedoPerformed += Initialize;
            _curve = curve;
        }

        public void Initialize()
        {
            positionCurve = new PositionCurveComposite(this,_curve,_curve.positionCurve,new MainPositionCurveSplitCommand(_curve),new TransformBlob(_curve.transform,null));
            sizeCurve = new SizeCurveComposite(this,_curve.sizeDistanceSampler,_curve,positionCurve);
            rotationCurve = new RotationCurveComposite(this,_curve.rotationDistanceSampler,_curve,positionCurve);
            doubleBezierCurve = new DoubleBezierCurveComposite(this, _curve.doubleBezierSampler, _curve,positionCurve);
            _curve.RequestMeshUpdate();
            _curve.positionCurve.Recalculate();
            positionCurve.FindPointClosestToCursor();
            doubleBezierCurve.FindClosestPointsToCursor();
        }

        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            FindClosestPoints();
            base.Click(mousePosition, clickHits);
        }

        public static void GetCurveDraw(List<IDraw> drawList,BezierCurve curve, TransformBlob transform, IComposite owner)
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

        void FindClosestPoints()
        {
            _curve.positionCurve.Recalculate();
            positionCurve.FindPointClosestToCursor();
            doubleBezierCurve.FindClosestPointsToCursor();
        }

        public override void Draw(List<IDraw> drawList,ClickHitData closestElementToCursor)
        {
            FindClosestPoints();
            if (_curve.showNormals)
            {
                float sampleDist = _curve.GetNormalDensityDistance();
                List<PointOnCurve> points = _curve.positionCurve.GetPointsWithSpacing(sampleDist);
                var visualNormalLength = _curve.VisualNormalsLength();
                var curveLength = _curve.positionCurve.GetLength();
                foreach (var i in points)
                {
                    var rotation = _curve.rotationDistanceSampler.GetValueAtDistance(i.distanceFromStartOfCurve, _curve.isClosedLoop, curveLength,_curve.positionCurve)+_curve.rotation;
                    var reference = Quaternion.AngleAxis(rotation, i.tangent) * i.reference;
                    drawList.Add(new LineDraw(this, _curve.transform.TransformPoint(i.position), _curve.transform.TransformPoint(reference * visualNormalLength+ i.position), Color.yellow));
                }
            }
            GetCurveDraw(drawList,_curve.positionCurve,new TransformBlob(_curve.transform,null),this);
            base.Draw(drawList,closestElementToCursor);
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
                default:
                    throw new NotImplementedException($"Case {_curve.editMode} not defined in switch statement");
            }
        }
    }
}
