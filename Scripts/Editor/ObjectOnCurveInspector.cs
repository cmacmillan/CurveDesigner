using UnityEditor;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    [CustomEditor(typeof(ObjectOnCurve))]
    public class ObjectOnCurveInspector : Editor
    {
        private static readonly int _Hint = "ObjectOnCurve".GetHashCode();
        private void OnDisable()
        {
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var objectOnCurve = (target as ObjectOnCurve);
            var serializedObj = new SerializedObject(objectOnCurve);
            var property = serializedObj.FindProperty("curve");
            bool isRed = objectOnCurve.curve == null;
            var oldColor = GUI.color;
            if (isRed)
                GUI.color = Color.red;
            EditorGUILayout.PropertyField(property);
            if (isRed)
                GUI.color = oldColor;
            serializedObj.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var objectOnCurve = (target as ObjectOnCurve);
            var curve = objectOnCurve.curve;
            if (curve == null)
                return;
            int controlID = GUIUtility.GetControlID(_Hint, FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlID);
            float buttonSize = 5*curve.settings.buttonSizeScalar;
            bool inViewport = GUITools.WorldToGUISpace(objectOnCurve.transform.position, out Vector2 guiPos, out _);
            var mousePos = Event.current.mousePosition;
            var buttonOverlapped = Vector2.Distance(mousePos,guiPos)<20;
            switch (eventType)
            {
                case EventType.Repaint:
                    if (inViewport)
                    {
                        Handles.BeginGUI();
                        Color oldColor = GUI.color;
                        var color = Color.green;
                        if (buttonOverlapped || GUIUtility.hotControl == controlID)
                        {
                            color = DrawMode.hovered.Tint(SelectionState.unselected, Color.green);
                        }
                        Ray ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(GUITools.GuiSpaceToScreenSpace(mousePos));
                        curve.RaycastAgainstCurve(ray, out float lengthwise, out float crosswise, objectOnCurve.attachedToFront);
                        GUI.color = color;
                        GUI.DrawTexture(GUITools.GetRectCenteredAtPosition(guiPos, buttonSize, buttonSize), curve.settings.circleIcon);
                        GUI.color = oldColor;
                        Handles.EndGUI();
                    }
                    break;
                case EventType.MouseDown:
                    if (buttonOverlapped && Event.current.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (Event.current.button == 0 && GUIUtility.hotControl == controlID)
                    {
                        Ray ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(GUITools.GuiSpaceToScreenSpace(mousePos));
                        Handles.BeginGUI();
                        curve.RaycastAgainstCurve(ray, out float lengthwise, out float crosswise, objectOnCurve.attachedToFront);
                        Handles.EndGUI();
                        objectOnCurve.lengthwisePosition = lengthwise;
                        objectOnCurve.crosswisePosition = crosswise;
                        objectOnCurve.Update();
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseMove:
                    SceneView.lastActiveSceneView.Repaint();
                    break;
            }
            Tools.hidden = true;
        }
    }
}
