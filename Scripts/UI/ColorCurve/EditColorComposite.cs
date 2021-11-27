#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class EditColorComposite : Clickable
    {
        public PointAlongCurveComposite centerPoint;
        private ColorSamplerPoint _point;
        private Curve3D _curve;
        private DoNothingClickCommand clickCommand;
        public override SelectableGUID GUID => _point.GUID;
        public EditColorComposite(Composite parent,ColorSamplerPoint point,ColorSampler sampler,Color color,PositionCurveComposite positionCurveComposite,Curve3D curve) : base(parent)
        {
            _curve = curve;
            _point = point;
            clickCommand = new DoNothingClickCommand();
            centerPoint = new PointAlongCurveComposite(this,point,positionCurveComposite,color,point.GUID,sampler);
        }
        public override IEnumerable<Composite> GetChildren()
        {
            yield return centerPoint;
        }
        public override IClickCommand GetClickCommand()
        {
            return clickCommand;
        }

        public override float DistanceFromMouse(Vector2 mouse)
        {
            if (GUITools.WorldToGUISpace(centerPoint.Position, out Vector2 guiPos, out float distFromCamera))
                if (shrunkPos(guiPos).Contains(mouse))
                    return 0;
            return float.MaxValue;
        }

        private readonly Vector2 rectSize = new Vector2(28,28);
        private readonly Vector2 shrunkRectSize = new Vector2(16,16);
        private Rect shrunkPos(Vector2 guiPos)
        {
            var shrinkOffset = (rectSize - shrunkRectSize) / 2;
            return new Rect(guiPos.x + shrinkOffset.x, guiPos.y + shrinkOffset.y, shrunkRectSize.x, shrunkRectSize.y);
        }

        public override void Draw(List<IDraw> drawList, ClickHitData closestElementToCursor)
        {
            drawList.Add(new EditColorDraw(this));
            base.Draw(drawList, closestElementToCursor);
        }
        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            GUITools.WorldToGUISpace(centerPoint.Position,out Vector2 guiPosition,out float screenDepth);
            float distance = Vector2.Distance(mousePosition,guiPosition);
            clickHits.Add(new ClickHitData(this,screenDepth,guiPosition-mousePosition));
            base.Click(mousePosition, clickHits);
        }
        public void IMGUIElement()
        {
            if (GUITools.WorldToGUISpace(centerPoint.Position, out Vector2 guiPos, out float distFromCamera))
            {
                var colorRect = new Rect(guiPos.x, guiPos.y, rectSize.x,rectSize.y);
                var colorRectShrunk = shrunkPos(guiPos);
                Handles.BeginGUI();
                GUI.Box(colorRect, GUIContent.none, _curve.settings.colorPickerBoxStyle);
                void WrapUp()
                {
                    MouseEater.EatMouseInput(colorRectShrunk);
                }
                try
                {
                    EditorGUI.BeginChangeCheck();
                    _point.value = EditorGUI.ColorField(colorRectShrunk,GUIContent.none, _point.value, showEyedropper: false, showAlpha: true, hdr: false);
                    if (EditorGUI.EndChangeCheck())
                        _curve.RequestMeshUpdate();
                }
                catch (ExitGUIException e) {
                    //not sure if i should rethrow this or not...
                    WrapUp();
                    throw e;
                }
                WrapUp();
                Handles.EndGUI();
            }
        }
    }
    public class EditColorDraw : IDraw, IIMGUI
    {
        private EditColorComposite creator;
        private float _distFromCamera;
        public EditColorDraw(EditColorComposite creator)
        {
            this.creator = creator;
            GUITools.WorldToGUISpace(creator.centerPoint.Position, out Vector2 _guiPos, out _distFromCamera);
        }

        public Composite Creator()
        {
            return creator;
        }

        public float DistFromCamera()
        {
            return _distFromCamera;
        }

        public void Draw(DrawMode mode, SelectionState selectionState)
        {
            creator.IMGUIElement();
        }

        public void Event()
        {
            creator.IMGUIElement();
        }
    }
}
#endif
