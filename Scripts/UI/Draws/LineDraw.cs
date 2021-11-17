#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    class LineDraw : IDraw
    {
        private Vector3 _startPoint;
        private Vector3 _endPoint;
        private float _distanceToPoint;
        private Composite _creator;
        private Color color;
        public LineDraw(Composite creator,Vector3 startPoint,Vector3 endPoint,Color? color=null)
        {
            this._creator = creator;
            this._startPoint = startPoint;
            this._endPoint = endPoint;
            var avg = (_startPoint + _endPoint) / 2.0f;
            this._distanceToPoint = GUITools.CameraDistanceToPoint(avg);
            if (color.HasValue)
                this.color = color.Value;
            else
                this.color = Color.white;
        }

        public Composite Creator()
        {
            return _creator;
        }

        public float DistFromCamera()
        {
            return _distanceToPoint + (int)DrawSortLayers.Lines;
        }

        public void Draw(DrawMode mode,SelectionState selectionState)
        {
            Color beforeColor = Handles.color;
            Handles.color = color;
            Handles.DrawAAPolyLine(CurveUIStatic.defaultLineTexture, new Vector3[] { _startPoint, _endPoint});
            Handles.color = beforeColor;
        }
    }
}
#endif
