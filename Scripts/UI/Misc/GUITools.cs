#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace ChaseMacMillan.CurveDesigner
{
    public static class GUITools
    {
        public static Rect GetRectCenteredAtPosition(Vector2 position, float halfWidth, float halfHeight)
        {
            return new Rect(position.x - halfWidth, position.y - halfHeight, 2 * halfWidth, 2 * halfHeight);
        }
        public static float CameraDistanceToPoint(Vector3 worldPos)
        {
            WorldToGUISpace(worldPos, out Vector2 guiPosition, out float depth);
            return depth;
        }
        public static bool WorldToGUISpace(Vector3 worldPos, out Vector2 guiPosition, out float screenDepth)
        {
            Profiler.BeginSample("WorldToGUISpace");
            var sceneCam = UnityEditor.SceneView.currentDrawingSceneView.camera;
            //var sceneCam = Camera.current;//UnityEditor.SceneView.lastActiveSceneView.camera;//Consider replacing with Camera.current?
            Vector3 screen_pos = sceneCam.WorldToScreenPoint(worldPos);
            screenDepth = screen_pos.z;
            if (screen_pos.z < 0)
            {
                //guiPosition = Vector2.zero;
                guiPosition = ScreenSpaceToGuiSpace(screen_pos);
                Profiler.EndSample();
                return false;
            }
            guiPosition = ScreenSpaceToGuiSpace(screen_pos);
            Profiler.EndSample();
            return true;
        }
        public static Vector3 GUIToWorldSpace(Vector2 guiPos, float screenDepth)
        {
            var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
            Vector3 screen_pos = GuiSpaceToScreenSpace(guiPos);
            screen_pos.z = screenDepth;
            return sceneCam.ScreenToWorldPoint(screen_pos);
        }
        public static Vector2 ScreenSpaceToGuiSpace(Vector2 screenPos)
        {
            return new Vector2(screenPos.x, UnityEditor.SceneView.lastActiveSceneView.camera.pixelHeight - screenPos.y)/EditorGUIUtility.pixelsPerPoint;
        }
        public static Vector2 GuiSpaceToScreenSpace(Vector2 guiPos)
        {
            guiPos *= EditorGUIUtility.pixelsPerPoint;
            return new Vector2(guiPos.x, UnityEditor.SceneView.lastActiveSceneView.camera.pixelHeight - guiPos.y);
        }

        private class Matrix3x4
        {
            private Vector4 GetRow(int row)
            {
                switch (row)
                {
                    case 0:
                        return row0;
                    case 1:
                        return row1;
                    case 2:
                        return row2;
                    default:
                        throw new ArgumentException();
                }
            }
            private void SetRow(int row, Vector4 val)
            {
                switch (row)
                {
                    case 0:
                        row0 = val;
                        break;
                    case 1:
                        row1 = val;
                        break;
                    case 2:
                        row2 = val;
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            private float GetColumn(int column, Vector4 row)
            {
                switch (column)
                {
                    case 0:
                        return row.w;
                    case 1:
                        return row.x;
                    case 2:
                        return row.y;
                    case 3:
                        return row.z;
                    default:
                        throw new ArgumentException();
                }
            }
            private Vector4 SetColumn(int column, Vector4 row,float val)
            {
                switch (column)
                {
                    case 0:
                        row.w = val;
                        break;
                    case 1:
                        row.x = val;
                        break;
                    case 2:
                        row.y = val;
                        break;
                    case 3:
                        row.z = val;
                        break;
                    default:
                        throw new ArgumentException();
                }
                return row;
            }
            public float this[int row, int column]
            {
                get
                {
                    return GetColumn(column,GetRow(row));
                }
                set
                {
                    SetRow(row, SetColumn(column,GetRow(row),value));
                }
            }
            public void SwapRows(int first, int second)
            {
                var firstRow = GetRow(first);
                var secondRow = GetRow(second);
                SetRow(first, secondRow);
                SetRow(second, firstRow);
            }
            public void NormalizeRow(int rowIndex, int columnToNormalizeAround)
            {
                var row = GetRow(rowIndex);
                float normalizeValue = GetColumn(columnToNormalizeAround, row);
                row /= normalizeValue;
                SetRow(rowIndex,row);
            }
            public void CombineCancel(int srcRowIndex, int targetRowIndex,int column)
            {
                var srcRow = GetRow(srcRowIndex);
                var targetRow = GetRow(targetRowIndex);
                var scaleFactor = -GetColumn(column, targetRow) /GetColumn(column,srcRow);
                targetRow += srcRow * scaleFactor;
                SetRow(targetRowIndex,targetRow);
            }
            public bool RowReduce()
            {
                int topRow = -1;
                for (int i = 0; i < 3; i++)
                    if (this[i, 0] != 0.0f)
                    {
                        topRow = i;
                        break;
                    }
                SwapRows(0, topRow);
                //top row now contains a nonzero value in the first column
                NormalizeRow(0, 0);
                //Matrix now has a 1 in the top left
                CombineCancel(0, 1, 0);
                CombineCancel(0, 2, 0);
                //matrix now has first column
                //1
                //0
                //0
                if (this[1, 1] == 0.0f)
                    SwapRows(1, 2);
                //second row now contains a nonzero value in the second column
                NormalizeRow(1,1);
                //1,1 is now a 1
                CombineCancel(1, 0, 1);
                CombineCancel(1, 2, 1);
                //matrix now has
                //1 0
                //0 1
                //0 0
                int columnIndex;
                if (this[2, 2] != 0.0f)
                    columnIndex = 2;
                else if (this[2, 3] != 0.0f)
                    columnIndex = 3;
                else
                    return false;
                NormalizeRow(2, columnIndex);
                CombineCancel(2, 0, columnIndex);
                CombineCancel(2, 1, columnIndex);
                return true;
            }
            public Matrix3x4(Vector3 c0, Vector3 c1, Vector3 c2, Vector3 c3)
            {
                void Set(Vector3 vect, int column)
                {
                    this[0, column] = vect.x;
                    this[1, column] = vect.y;
                    this[2, column] = vect.z;
                }
                Set(c0, 0);
                Set(c1, 1);
                Set(c2, 2);
                Set(c3, 3);
            }
            public override string ToString()
            {
                return $"[{row0.w} {row0.x} {row0.y} {row0.z} {Environment.NewLine} {row1.w} {row1.x} {row1.y} {row1.z}  {Environment.NewLine} {row2.w} {row2.x} {row2.y} {row2.z}]";
            }
            public static void Test()
            {
                Matrix3x4 mat = new Matrix3x4(new Vector3(1, 2, 3), new Vector3(4, 3, 6), new Vector3(7, 8, 9), new Vector3(10, 11, 13));
                Debug.Log(mat);
                mat.RowReduce();
                Debug.Log(mat);
                mat = new Matrix3x4(new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9), new Vector3(10, 11, 13));
                Debug.Log(mat);
                mat.RowReduce();
                Debug.Log(mat);
            }
            Vector4 row0;
            Vector4 row1;
            Vector4 row2;
        }

        public static bool GetClosestPointBetweenTwoLines(Vector3 line1Point, Vector3 line1Slope, Vector3 line2Point, Vector3 line2Slope, out Vector3 result)
        {
            if (line1Slope == Vector3.zero || line2Slope == Vector3.zero)
            {
                result = Vector3.zero;
                return false;
            }
            Vector3 slope = Vector3.Cross(line1Slope,line2Slope);
            if (slope == Vector3.zero)
            {
                result = Vector3.zero;
                return false;
            }
            Vector3 f = line1Point - line2Point;
            Matrix3x4 matrix = new Matrix3x4(line1Slope,slope,-line2Slope,f);
            matrix.RowReduce();
            result = matrix[2, 3] * line2Slope + line2Point;
            return true;
        }
    }
}
#endif
