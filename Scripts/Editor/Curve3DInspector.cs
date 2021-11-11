using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChaseMacMillan.CurveDesigner
{

    [CustomEditor(typeof(Curve3D))]
    public class Curve3DInspector : Editor
    {
        private static readonly int _CurveHint = "NewGUI.CURVE".GetHashCode();

        MonoScript script;
        private void OnEnable()
        {
            if (target!=null)
                script = MonoScript.FromMonoBehaviour((Curve3D)target);
        }
        private void OnDisable()
        {
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            EnsureValidEditMode();
            var curve3d = (target as Curve3D);
            curve3d.TryInitStyles();
            curve3d.TryInitialize();
            EnsureValidEditMode();
            curve3d.BindDataToPositionCurve();
            if (curve3d.UICurve == null)
            {
                curve3d.UICurve = new UICurve(null, curve3d);
                curve3d.UICurve.Initialize();
            }
            else if (curve3d.UICurve._curve == null)
            {
                curve3d.UICurve._curve = curve3d;
            }
            GUI.enabled = false;
            script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
            GUI.enabled = true;

            float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
            EditorGUIUtility.labelWidth = 0;
            EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth - 4;

            EditorGUILayout.BeginVertical();
            bool isDisabled = false;

            GUILayout.BeginVertical(curve3d.effectBgStyle);
            {
                for (int i = 0; i < curve3d.collapsableCategories.Length; i++)
                {
                    var curr = curve3d.collapsableCategories[i];
                    bool isInitial = i == 0;
                    GUIStyle headerStyle;
                    int headerHeight;
                    if (isInitial)
                    {
                        headerHeight = 25;
                        headerStyle = curve3d.initialHeaderStyle;
                    }
                    else
                    {
                        headerHeight = 15;
                        headerStyle = curve3d.nonInitialHeaderStyle;
                    }
                    Rect headerRect = GUILayoutUtility.GetRect(width, headerHeight);
                    int iconSize = 21;
                    Rect iconRect = new Rect(headerRect.x + 4, headerRect.y + 2, iconSize, iconSize);
                    if (curr.IsExpanded(curve3d))
                    {
                        using (new EditorGUI.DisabledScope(isDisabled))
                        {
                            GUIStyle m_ModulePadding = new GUIStyle();
                            m_ModulePadding.padding = new RectOffset(3, 3, 4, 2);
                            Rect moduleSize = EditorGUILayout.BeginVertical(m_ModulePadding);
                            {
                                moduleSize.y -= 4;
                                moduleSize.height += 4;
                                GUI.Label(moduleSize, GUIContent.none, curve3d.shurikenModuleBg);
                                curr.Draw(curve3d);
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
                    if (isInitial && curve3d.settings.uiIcon != null)
                    {
                        GUI.DrawTexture(iconRect, curve3d.settings.uiIcon, ScaleMode.StretchToFill, true);
                    }
                    GUIContent headerLabel = new GUIContent();
                    headerLabel.text = curr.GetName(curve3d);
                    curr.SetIsExpanded(curve3d,GUI.Toggle(headerRect, curr.IsExpanded(curve3d), headerLabel, headerStyle));
                    GUILayout.Space(1);
                }
                GUILayout.Space(-1);
            }
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            curve3d.ReadMaterialsFromRenderer();

            Undo.RecordObject(curve3d, "curve");
            Undo.RecordObject(curve3d.Renderer, "curveRenderer");

            int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.KeyDown:
                    HandleKeys(curve3d);
                    break;
            }
            curve3d.UpdateMesh();
        }

        private void WindowFunc(int id)
        {
            bool isWideMode = EditorGUIUtility.wideMode;
            float initialLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.wideMode = true;
            EditorGUIUtility.labelWidth = 90;
            var curve = (target as Curve3D);
            var editModes = curve.editModeCategories;
            if (curve.UICurve != null)
            {
                int pointCount = curve.ActiveElement.NumSelectables(curve);
                if (pointCount == 0)
                {
                    GUILayout.Label($"Click on the curve in the scene view to place a {curve.ActiveElement.GetPointName()} control point", curve.centeredStyle);
                }
                else
                {
                    int selectedPointCount = 0;
                    int numPoints = curve.ActiveElement.NumSelectables(curve);
                    for (int i = 0; i < numPoints; i++)
                        if (curve.selectedPoints.Contains(curve.ActiveElement.GetSelectable(i, curve).GUID))
                            selectedPointCount++;
                    if (selectedPointCount == 0)
                        GUILayout.Label("No points selected", curve.centeredStyle);
                    else
                        GUILayout.Label($"{selectedPointCount} point{(selectedPointCount != 1 ? "s" : "")} selected", curve.centeredStyle);
                    var drawer = curve.UICurve.GetWindowDrawer();
                    EditorGUI.BeginChangeCheck();
                    drawer.DrawWindow(curve);
                    if (EditorGUI.EndChangeCheck())
                    {
                        curve.Recalculate();
                    }
                }
            }
            else
            {
                Debug.LogError("No ui curve!");
            }
            EditorGUIUtility.wideMode = isWideMode;
            EditorGUIUtility.labelWidth = initialLabelWidth;
        }

        void HandleKeys(Curve3D curve3d)
        {
            if (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace)
            {
                bool didDelete = curve3d.ActiveElement.Delete(curve3d.selectedPoints, curve3d);
                if (didDelete)
                {
                    curve3d.RequestMeshUpdate();
                    curve3d.UICurve.Initialize();
                }
                Event.current.Use();
            }
            if (Event.current.keyCode == KeyCode.A && Event.current.control)
            {
                curve3d.selectedPoints = curve3d.ActiveElement.SelectAll(curve3d);
                Event.current.Use();
            }
            SceneView.RepaintAll();
        }

        void EnsureValidEditMode()
        {
            var curve3d = (target as Curve3D);
            if (curve3d.editMode == Curve3DEditMode.Extrude && curve3d.type != MeshGenerationMode.Extrude)
                curve3d.editMode = Curve3DEditMode.PositionCurve;
            if (curve3d.editMode == Curve3DEditMode.Arc && curve3d.type != MeshGenerationMode.Cylinder && curve3d.type != MeshGenerationMode.HollowTube)
                curve3d.editMode = Curve3DEditMode.PositionCurve;
        }

            ClickHitData closestElementToCursor = null;
        List<IDraw> cachedDraws = new List<IDraw>();
        private void OnSceneGUI()
        {
            var curve3d = (target as Curve3D);
            curve3d.TryInitStyles();
            curve3d.TryInitialize();
            curve3d.CacheAverageSize();
            //curve3d.UpdateMaterials();
            EnsureValidEditMode();
            curve3d.BindDataToPositionCurve();
            curve3d.samplesPerSegment = Mathf.Max(1, curve3d.samplesPerSegment);
            var windowRect = new Rect(20, 40, 400, 0);
            bool didWindowEatMouse = false;
            if (curve3d.showPointSelectionWindow)
            {
                var actualWindowRect = GUILayout.Window(61732234, windowRect, WindowFunc, $"Editing {curve3d.editModeCategories.editmodeNameMap[curve3d.editMode]}");
                didWindowEatMouse = actualWindowRect.Contains(Event.current.mousePosition);
                MouseEater.EatMouseInput(actualWindowRect);
            }
            var rotationPoints = curve3d.rotationSampler.GetPoints(curve3d.positionCurve);
            if (curve3d.previousRotations.Count != rotationPoints.Count)
                curve3d.CopyRotations();
            Undo.RecordObject(curve3d, "curve");
            Undo.RecordObject(curve3d.Renderer, "curveRenderer");
            ClickHitData elementClickedDown = curve3d.elementClickedDown;
            CurveUIStatic.circleTexture = curve3d.settings.circleIcon;
            CurveUIStatic.plusTexture = curve3d.settings.plusButton;
            CurveUIStatic.squareTexture = curve3d.settings.squareIcon;
            CurveUIStatic.diamondTexture = curve3d.settings.diamondIcon;
            CurveUIStatic.defaultLineTexture = curve3d.settings.lineTex;
            CurveUIStatic.buttonSizeScalar = curve3d.settings.buttonSizeScalar;
            if (curve3d.UICurve == null)
            {
                curve3d.UICurve = new UICurve(null, curve3d);
                curve3d.UICurve.Initialize();
            }
            else if (curve3d.UICurve._curve == null)
            {
                curve3d.UICurve._curve = curve3d;
            }
            curve3d.UICurve.BakeBlobs();
            curve3d.UpdateMesh();
            var curveEditor = curve3d.UICurve;
            //var MousePos = new Vector2(480.9996f,312.9997f);//Event.current.mousePosition;
            var MousePos = Event.current.mousePosition;
            bool IsActiveElementSelected()
            {
                return curve3d.selectedPoints.Where(a => a == curve3d.elementClickedDown.owner.GUID).Count() > 0;
            }
            int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlID);
            if (elementClickedDown == null && !didWindowEatMouse && eventType == EventType.Layout)
            {
                Profiler.BeginSample("GetClosestElementToCursor");
                closestElementToCursor = GetClosestElementToCursor(curveEditor, MousePos);
                Profiler.EndSample();
            }
            if (!didWindowEatMouse && eventType == EventType.Layout)
            {
                cachedDraws.Clear();
                curveEditor.Draw(cachedDraws, closestElementToCursor);
            }
            void DrawLoop(bool imguiEvent)
            {
                cachedDraws.Sort((a, b) => (int)(Mathf.Sign(b.DistFromCamera() - a.DistFromCamera())));
                var selected = curve3d.selectedPoints;
                var currentEventType = Event.current.type;
                foreach (var draw in cachedDraws)
                    if (draw.DistFromCamera() > 0)
                    {
                        if (imguiEvent)
                        {
                            if (currentEventType == EventType.MouseDown)
                            {
                                if (draw.Creator() == closestElementToCursor.owner)
                                {
                                    Event.current.type = EventType.MouseDown;
                                }
                                else
                                {
                                    Event.current.type = EventType.Ignore;
                                }
                            }
                            var imgui = draw as IIMGUI;
                            if (imgui != null)
                                imgui.Event();
                        }
                        else
                        {
                            SelectionState selectionState = SelectionState.unselected;
                            var guid = draw.Creator().GUID;
                            if (selected.Count > 0 && guid == selected[0])
                                selectionState = SelectionState.primarySelected;
                            else if (selected.Contains(guid))
                                selectionState = SelectionState.secondarySelected;
                            if (closestElementToCursor != null && draw.Creator() == closestElementToCursor.owner)
                                draw.Draw(DrawMode.hovered, selectionState);
                            else if (elementClickedDown != null && draw.Creator() == elementClickedDown.owner)
                                draw.Draw(DrawMode.clicked, selectionState);
                            else
                                draw.Draw(DrawMode.normal, selectionState);
                        }
                    }
                if (imguiEvent && currentEventType == EventType.MouseDown)
                    Event.current.type = currentEventType;
            }
            void Draw() { DrawLoop(false); }
            void IMGUI() { DrawLoop(true); }
            //void IMGUI() { return; }
            //Regardless of event, you must call either Draw or IMGUI(), to make sure that imgui stuff gets all the events
            var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
            Profiler.BeginSample("Event Loop");
            switch (eventType)
            {
                case EventType.KeyDown:
                    HandleKeys(curve3d);
                    IMGUI();
                    break;
                case EventType.Repaint:
                    //var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
                    //curve3d.worldSpaceToClipSpace = sceneCam.previousViewProjectionMatrix;
                    //Debug.Log($"{sceneCam.pixelRect} repaint");
                    Draw();
                    break;
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        if (closestElementToCursor != null)
                        {
                            GUIUtility.hotControl = controlID;
                            curve3d.elementClickedDown = closestElementToCursor;
                            var clickPos = MousePos + curve3d.elementClickedDown.offset;
                            var commandToExecute = curve3d.elementClickedDown.owner.GetClickCommand();
                            //shift will behave like control if it's a split command, also just fixed a bug when shift-clicking a split point

                            bool isSplitAndShift = Event.current.shift && (commandToExecute is SplitCommand);
                            if (Event.current.control || isSplitAndShift)  /////CONTROL CLICK
                            {
                                curve3d.shiftControlState = ClickShiftControlState.control;
                                curve3d.ToggleSelectPoint(curve3d.elementClickedDown.owner.GUID);
                            }
                            else if (Event.current.shift) /////SHIFT CLICK
                            {
                                curve3d.shiftControlState = ClickShiftControlState.shift;
                                SelectableGUID previous = SelectableGUID.Null;
                                if (curve3d.selectedPoints.Count > 0)
                                    previous = curve3d.selectedPoints[0];
                                curve3d.selectedPoints = SelectableGUID.SelectBetween(curve3d.ActiveElement, previous, curve3d.elementClickedDown.owner.GUID, curve3d, curve3d.positionCurve);//for a extrude selection should use that curve instead of main
                            }
                            else
                            {
                                curve3d.shiftControlState = ClickShiftControlState.none;
                                if (curve3d.selectedPoints.Contains(curve3d.elementClickedDown.owner.GUID))
                                    curve3d.SelectAdditionalPoint(curve3d.elementClickedDown.owner.GUID);
                                else
                                    curve3d.SelectOnlyPoint(curve3d.elementClickedDown.owner.GUID);
                            }
                            if (IsActiveElementSelected())
                            {
                                IMGUI();
                                commandToExecute.ClickDown(clickPos, curve3d, curve3d.selectedPoints);
                                commandToExecute.ClickDrag(clickPos, curve3d, curve3d.elementClickedDown, curve3d.selectedPoints);
                            }
                            curve3d.RequestMeshUpdate();
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (elementClickedDown != null)
                    {
                        var clickPos = MousePos + elementClickedDown.offset;
                        elementClickedDown.hasBeenDragged = true;
                        var commandToExecute = elementClickedDown.owner.GetClickCommand();
                        if (IsActiveElementSelected())
                            commandToExecute.ClickDrag(clickPos, curve3d, elementClickedDown, curve3d.selectedPoints);
                        IMGUI();
                        Event.current.Use();
                        curve3d.RequestMeshUpdate();
                    }
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0)
                    {
                        if (elementClickedDown != null)
                        {
                            GUIUtility.hotControl = 0;
                            var commandToExecute = elementClickedDown.owner.GetClickCommand();
                            if (IsActiveElementSelected())
                                commandToExecute.ClickUp(MousePos, curve3d, curve3d.selectedPoints);
                            curve3d.RequestMeshUpdate();
                            if (!curve3d.elementClickedDown.hasBeenDragged && curve3d.shiftControlState == ClickShiftControlState.none)
                            {
                                curve3d.SelectOnlyPoint(curve3d.elementClickedDown.owner.GUID);
                            }
                            IMGUI();
                            curve3d.elementClickedDown = null;
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseMove:
                    IMGUI();
                    SceneView.lastActiveSceneView.Repaint();
                    //HandleUtility.Repaint();
                    break;
                default:
                    //Debug.Log($"{sceneCam.pixelRect} default");
                    IMGUI();
                    break;
            }
            Tools.hidden = closestElementToCursor != null && (GUIUtility.hotControl==controlID || GUIUtility.hotControl==0);
            curve3d.CopyRotations();
            Profiler.EndSample();
        }
        private const float smallDist = 5;
        private const float bigDist = 20;
        ClickHitData GetClosestElementToCursor(Composite root, Vector2 clickPosition)
        {
            ClickHitData GetFrom(List<ClickHitData> lst)
            {
                List<ClickHitData> clicks = new List<ClickHitData>(lst);
                Profiler.BeginSample("DistanceFromMouse");
                foreach (var i in clicks)
                    i.distanceFromClick = i.owner.DistanceFromMouse(clickPosition);
                Profiler.EndSample();
                clicks.Sort((a, b) => (int)Mathf.Sign(a.distanceFromCamera - b.distanceFromCamera));
                foreach (var i in clicks)
                {
                    float dist = i.distanceFromClick;
                    if (dist < smallDist)
                        return i;
                }
                clicks.Sort((a, b) => (int)Mathf.Sign(a.distanceFromClick - b.distanceFromClick));
                foreach (var i in clicks)
                {
                    float dist = i.distanceFromClick;
                    if (dist < bigDist)
                        return i;
                }
                return null;
            }
            List<ClickHitData> hits = new List<ClickHitData>();
            Profiler.BeginSample("Click");
            root.Click(clickPosition, hits);
            Profiler.EndSample();
            List<ClickHitData> highPriorityHits = new List<ClickHitData>();
            List<ClickHitData> lowPriorityHits = new List<ClickHitData>();
            foreach (var i in hits)
            {
                if (i.isLowPriority)
                    lowPriorityHits.Add(i);
                else
                    highPriorityHits.Add(i);
            }
            var high = GetFrom(highPriorityHits);
            if (high != null)
                return high;
            var low = GetFrom(lowPriorityHits);
            return low;
        }
    }
}
