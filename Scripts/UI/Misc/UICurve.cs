using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace ChaseMacMillan.CurveDesigner
{
    public class UICurve : Composite
    {
        public Curve3D _curve;
        public UICurve(Composite parent,Curve3D curve) : base(parent)
        {
#if UNITY_EDITOR
            Undo.undoRedoPerformed -= Initialize;
            Undo.undoRedoPerformed += Initialize;
#endif
            _curve = curve;
        }
        public void Initialize()
        {
#if UNITY_EDITOR
            if (_curve == null)
            {
                Undo.undoRedoPerformed -= Initialize;
                return;
            }

            _curve.BindDataToPositionCurve();
            _curve.Recalculate();
            var mainPositionCurve = new List<BezierCurve>();
            mainPositionCurve.Add(_curve.positionCurve);
            positionCurve = new PositionCurveComposite(this,_curve,_curve.positionCurve,new MainPositionCurveSplitCommand(_curve),new TransformBlob(_curve.transform,null,null),mainPositionCurve,-1);
            sizeCurve = new SizeCurveComposite(this,_curve.sizeSampler,_curve,positionCurve);
            rotationCurve = new RotationCurveComposite(this,_curve.rotationSampler,_curve,positionCurve);
            colorCurve = new ColorCurveComposite(this, _curve.colorSampler, _curve, positionCurve);
            thicknessCurve = new ThicknessCurveComposite(this, _curve.thicknessSampler, _curve, positionCurve);
            arcCurve = new ArcCurveComposite(this,_curve.arcOfTubeSampler,_curve,positionCurve);
            extrudeCurve = new ExtrudeCurveComposite(this, _curve.extrudeSampler, _curve,positionCurve);
            BakeBlobs();
            _curve.RequestMeshUpdate();
            if (Event.current != null)
            {
                positionCurve.FindPointClosestToCursor();
                extrudeCurve.FindClosestPointsToCursor();
            }
#endif
        }
#if UNITY_EDITOR
        public PositionCurveComposite positionCurve;
        public SizeCurveComposite sizeCurve;
        public RotationCurveComposite rotationCurve;
        public ColorCurveComposite colorCurve;
        public ExtrudeCurveComposite extrudeCurve;
        public ThicknessCurveComposite thicknessCurve;
        public ArcCurveComposite arcCurve;

        public IWindowDrawer GetWindowDrawer()
        {
            switch (_curve.editMode)
            {
                case Curve3DEditMode.Extrude:
                    return extrudeCurve;
                case Curve3DEditMode.PositionCurve:
                    return positionCurve;
                case Curve3DEditMode.Rotation:
                    return rotationCurve;
                case Curve3DEditMode.Size:
                    return sizeCurve;
                case Curve3DEditMode.Color:
                    return colorCurve;
                case Curve3DEditMode.Thickness:
                    return thicknessCurve;
                case Curve3DEditMode.Arc:
                    return arcCurve;
                default:
                    throw new NotImplementedException($"Case {_curve.editMode} not defined in switch statement");
            }
        }


        public override SelectableGUID GUID => SelectableGUID.Null;

        public void BakeBlobs()
        {
            positionCurve.transformBlob.Bake();
            foreach (var i in extrudeCurve._secondaryCurves)
                i.transformBlob.Bake();
        }

        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            FindClosestPoints();
            base.Click(mousePosition, clickHits);
        }

        public static void GetNormalsTangentsDraw(List<IDraw> drawList,Curve3D _curve,Composite creator,TransformBlob transform,BezierCurve curveToDraw)
        {
            if (_curve.showNormals || _curve.showTangents)
            {
                float sampleDist = _curve.GetVertexDensityDistance();
                List<PointOnCurve> points = curveToDraw.GetPointsWithSpacing(sampleDist);
                var visualNormalLength = sampleDist * .5f;
                var curveLength = curveToDraw.GetLength();
                foreach (var i in points)
                {
                    var rotation = _curve.rotationSampler.GetValueAtDistance(i.distanceFromStartOfCurve, _curve.positionCurve);
                    var reference = Quaternion.AngleAxis(rotation, i.tangent) * i.reference;
                    if (_curve.showNormals)
                        drawList.Add(new LineDraw(creator, transform.TransformPoint(i.position), transform.TransformPoint(reference * visualNormalLength+ i.position), Color.yellow));
                    if (_curve.showTangents)
                        drawList.Add(new LineDraw(creator, transform.TransformPoint(i.position), transform.TransformPoint(i.tangent* visualNormalLength+ i.position), Color.cyan));
                }
            }
        }
        public static void GetCurveDraw(List<IDraw> drawList,BezierCurve curve, TransformBlob transform, Composite owner)
        {
            for (int i = 0; i < curve.NumSegments; i++)
            {
                var point1 = transform.TransformPoint(curve[i, 0]);
                var point2 = transform.TransformPoint(curve[i, 3]);
                var tangent1 = transform.TransformPoint(curve[i, 1]);
                var tangent2 = transform.TransformPoint(curve[i, 2]);
                drawList.Add(new CurveSegmentDraw(owner, point1, point2, tangent1, tangent2, new Color(.6f, .6f, .6f)));
            }
        }

        void FindClosestPoints()
        {
            positionCurve.FindPointClosestToCursor();
            if (_curve.type == MeshGenerationMode.Extrude)
                extrudeCurve.FindClosestPointsToCursor();
        }

        public override void Draw(List<IDraw> drawList,ClickHitData closestElementToCursor)
        {
            Profiler.BeginSample("find closest points");
            FindClosestPoints();
            Profiler.EndSample();
            GetNormalsTangentsDraw(drawList, _curve, this, positionCurve.transformBlob,_curve.positionCurve);
            GetCurveDraw(drawList,_curve.positionCurve,positionCurve.transformBlob,this);
            Profiler.BeginSample("draw ui curve");
            base.Draw(drawList,closestElementToCursor);
            Profiler.EndSample();
        }

        public override IEnumerable<Composite> GetChildren()
        {
            switch (_curve.editMode)
            {
                case Curve3DEditMode.PositionCurve:
                    yield return positionCurve;
                    break;
                case Curve3DEditMode.Size:
                    yield return sizeCurve;
                    break;
                case Curve3DEditMode.Rotation:
                    yield return rotationCurve;
                    break;
                case Curve3DEditMode.Extrude:
                    yield return extrudeCurve;
                    break;
                case Curve3DEditMode.Color:
                    yield return colorCurve;
                    break;
                case Curve3DEditMode.Thickness:
                    yield return thicknessCurve;
                    break;
                case Curve3DEditMode.Arc:
                    yield return arcCurve;
                    break;
                default:
                    throw new NotImplementedException($"Case {_curve.editMode} not defined in switch statement");
            }
        }
#endif
    }
}
