﻿using Assets.NewUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;


[CustomEditor(typeof(Curve3D))]
public class Curve3DInspector : Editor
{
    private static readonly int _CurveHint = "NewGUI.CURVE".GetHashCode();

    MonoScript script;
    private void OnEnable()
    {
        script = MonoScript.FromMonoBehaviour((Curve3D)target);
    }

    public override void OnInspectorGUI()
    {
        var curve3d = (target as Curve3D);

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
                } else
                {
                    headerHeight = 15;
                    headerStyle = curve3d.nonInitialHeaderStyle;
                }
                Rect headerRect = GUILayoutUtility.GetRect(width, headerHeight);
                int iconSize = 21;
                Rect iconRect = new Rect(headerRect.x + 4, headerRect.y + 2, iconSize, iconSize);
                if (curr.isExpanded)
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
                if (isInitial && curve3d.settings.uiIcon!=null)
                {
                    GUI.DrawTexture(iconRect, curve3d.settings.uiIcon, ScaleMode.StretchToFill, true);
                }
                GUIContent headerLabel = new GUIContent();
                headerLabel.text = curr.GetName(curve3d);
                curr.isExpanded = GUI.Toggle(headerRect, curr.isExpanded, headerLabel, headerStyle);
                GUILayout.Space(1);
            }
            GUILayout.Space(-1);
        }
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();

        Undo.RecordObject(curve3d, "curve");

        int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.KeyDown:
                HandleKeys(curve3d);
                break;
        }
    }

    private void WindowFunc(int id)
    {
        var curve = (target as Curve3D);
        var editModes = curve.editModeCategories;
        if (curve.UICurve != null)
        {
            int pointCount = curve.ActiveElement.NumSelectables(curve);
            if (pointCount == 0)
            {
                GUILayout.Label($"Click on the curve in the scene view to place a {curve.ActiveElement.GetPointName()} control point", curve.CenteredStyle);
            }
            else
            {
                int selectedPointCount = 0;
                int numPoints = curve.ActiveElement.NumSelectables(curve);
                for (int i = 0; i < numPoints; i++)
                    if (curve.selectedPoints.Contains(curve.ActiveElement.GetSelectable(i, curve).GUID))
                        selectedPointCount++;
                if (selectedPointCount == 0)
                    GUILayout.Label("No points selected", curve.CenteredStyle);
                else
                    GUILayout.Label($"{selectedPointCount} point{(selectedPointCount != 1 ? "s" : "")} selected", curve.CenteredStyle);
                var drawer = curve.UICurve.GetWindowDrawer();
                drawer.DrawWindow(curve);
            }
        }
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
        if (Event.current.keyCode == KeyCode.Tab)
        {
            switch (curve3d.editMode)
            {
                case EditMode.PositionCurve:
                    curve3d.editMode = EditMode.Rotation;
                    break;
                case EditMode.Rotation:
                    curve3d.editMode = EditMode.Size;
                    break;
                case EditMode.Size:
                    curve3d.editMode = EditMode.Color;
                    break;
                case EditMode.Color:
                    curve3d.editMode = EditMode.PositionCurve;
                    break;
            }
            Event.current.Use();
        }
        SceneView.RepaintAll();
    }

    private void OnSceneGUI()
    {
        var curve3d = (target as Curve3D);
        if (curve3d.editMode == EditMode.DoubleBezier && curve3d.type != CurveType.DoubleBezier)
            curve3d.editMode = EditMode.PositionCurve;
        var windowRect = new Rect(20, 40, 0, 0);
        if (curve3d.showPointSelectionWindow)
        {
            MouseEater.EatMouseInput(GUILayout.Window(61732234, windowRect, WindowFunc, $"Editing {curve3d.editModeCategories.editmodeNameMap[curve3d.editMode]}"));
        }
        //Handles.DrawAAConvexPolygon(new Vector3[4] { new Vector3(0,1,0), new Vector3(1,0,0),new Vector3(-1,0,0),new Vector3(0,0,1)});
        /*
        if (curve3d.graphicsMesh!=null && curve3d.graphicsMaterial!=null)
        {
            Graphics.DrawMesh(curve3d.graphicsMesh,Matrix4x4.identity,curve3d.graphicsMaterial,1<<5);
        }
        */

        curve3d.positionCurve.owner = curve3d;
        curve3d.positionCurve.isClosedLoop = curve3d.isClosedLoop;
        curve3d.positionCurve.dimensionLockMode = curve3d.lockToPositionZero;
        curve3d.CacheAverageSize();
        var rotationPoints = curve3d.rotationSampler.GetPoints(curve3d.positionCurve);
        if (curve3d.previousRotations.Count != rotationPoints.Count)
            curve3d.CopyRotations();
        Undo.RecordObject(curve3d, "curve");
        ClickHitData elementClickedDown = curve3d.elementClickedDown;
        Curve3DSettings.circleTexture = curve3d.settings.circleIcon;
        Curve3DSettings.squareTexture = curve3d.settings.squareIcon;
        Curve3DSettings.diamondTexture = curve3d.settings.diamondIcon;
        Curve3DSettings.defaultLineTexture = curve3d.settings.lineTex;
        if (curve3d.UICurve == null)
        {
            curve3d.UICurve=new UICurve(null,curve3d);
            curve3d.UICurve.Initialize();
        } 
        else if (curve3d.UICurve._curve==null)
        {
            curve3d.UICurve._curve = curve3d;
        }
        curve3d.UICurve.BakeBlobs();
        UpdateMesh(curve3d);
        var curveEditor = curve3d.UICurve;
        var MousePos = Event.current.mousePosition;
        bool IsActiveElementSelected()
        {
            return curve3d.selectedPoints.Where(a => a == curve3d.elementClickedDown.owner.GUID).Count() > 0;
        }
        int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
        var eventType = Event.current.GetTypeForControl(controlID);
        ClickHitData closestElementToCursor = null;
        if (elementClickedDown == null)
            closestElementToCursor = GetClosestElementToCursor(curveEditor, MousePos, EventType.Repaint);
        void DrawLoop(bool imguiEvent)
        {
            List<IDraw> draws = new List<IDraw>();
            curveEditor.Draw(draws, closestElementToCursor);
            draws.Sort((a, b) => (int)(Mathf.Sign(b.DistFromCamera() - a.DistFromCamera())));
            var selected = curve3d.selectedPoints;
            var currentEventType = Event.current.type;
            foreach (var draw in draws)
                if (draw.DistFromCamera() > 0)
                {
                    if (imguiEvent)
                    {
                        if (currentEventType== EventType.MouseDown)
                        {
                            if (draw.Creator() == closestElementToCursor.owner)
                                Event.current.type = EventType.MouseDown;
                            else
                                Event.current.type = EventType.Ignore;
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
            Event.current.type = currentEventType;
        }
        void Draw() { DrawLoop(false); }
        void IMGUI() { DrawLoop(true); }
        //Regardless of event, you must call either Draw or IMGUI(), to make sure that imgui stuff gets all the events
        switch (eventType)
        {
            case EventType.KeyDown:
                HandleKeys(curve3d);
                IMGUI();
                break;
            case EventType.Repaint:
                Draw();
                break;
            case EventType.MouseDown:
                if (Event.current.button == 0)
                {
                    var clicked = GetClosestElementToCursor(curveEditor, MousePos, EventType.MouseDown);
                    if (clicked != null)
                    {
                        GUIUtility.hotControl = controlID;
                        curve3d.elementClickedDown = clicked;
                        var clickPos = MousePos + curve3d.elementClickedDown.offset;
                        var commandToExecute = curve3d.elementClickedDown.owner.GetClickCommand();
                        //shift will behave like control if it's a split command, also just fixed a bug when shift-clicking a split point
                        bool isSplitAndShift = Event.current.shift && (commandToExecute is SplitCommand);
                        if (Event.current.control || isSplitAndShift)  /////CONTROL CLICK
                        {
                            curve3d.shiftControlState = Curve3D.ClickShiftControlState.control;
                            curve3d.ToggleSelectPoint(curve3d.elementClickedDown.owner.GUID);
                        }
                        else if (Event.current.shift) /////SHIFT CLICK
                        {
                            curve3d.shiftControlState = Curve3D.ClickShiftControlState.shift;
                            SelectableGUID previous = SelectableGUID.Null;
                            if (curve3d.selectedPoints.Count > 0)
                                previous = curve3d.selectedPoints[0];
                            curve3d.selectedPoints = SelectableGUID.SelectBetween(curve3d.ActiveElement, previous, curve3d.elementClickedDown.owner.GUID,curve3d,curve3d.positionCurve);//for a double bezier selection should use that curve instead of main
                        }
                        else
                        {
                            curve3d.shiftControlState = Curve3D.ClickShiftControlState.none;
                            if (curve3d.selectedPoints.Contains(curve3d.elementClickedDown.owner.GUID))
                                curve3d.SelectAdditionalPoint(curve3d.elementClickedDown.owner.GUID);
                            else 
                                curve3d.SelectOnlyPoint(curve3d.elementClickedDown.owner.GUID);
                        }
                        if (IsActiveElementSelected())
                        {
                            IMGUI();
                            commandToExecute.ClickDown(clickPos,curve3d,curve3d.selectedPoints);
                            commandToExecute.ClickDrag(clickPos, curve3d, curve3d.elementClickedDown,curve3d.selectedPoints);
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
                        commandToExecute.ClickDrag(clickPos,curve3d,elementClickedDown,curve3d.selectedPoints);
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
                            commandToExecute.ClickUp(MousePos,curve3d,curve3d.selectedPoints);
                        curve3d.RequestMeshUpdate();
                        if (!curve3d.elementClickedDown.hasBeenDragged && curve3d.shiftControlState==Curve3D.ClickShiftControlState.none)
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
                HandleUtility.Repaint();
                break;
            default:
                IMGUI();
                break;
        }
        curve3d.CopyRotations();
    }
    private void UpdateMesh(Curve3D curve)
    {
        if (!MeshGenerator.IsBuzy)
        {
            if (curve.lastMeshUpdateEndTime != MeshGenerator.lastUpdateTime)
            {
                if (MeshGenerator.didMeshGenerationSucceed)
                {
                    if (curve.displayMesh == null)
                    {
                        curve.displayMesh = new Mesh();
                        curve.filter.mesh = curve.displayMesh;
                    }
                    else
                    {
                        curve.displayMesh.Clear();
                    }
                    curve.displayMesh.SetVertices(MeshGenerator.vertices);
                    curve.displayMesh.SetTriangles(MeshGenerator.triangles, 0);
                    curve.displayMesh.SetUVs(0, MeshGenerator.uvs);
                    curve.displayMesh.SetColors(MeshGenerator.colors);
                    curve.displayMesh.RecalculateNormals();
                    curve.collider = curve.GetComponent<MeshCollider>();
                    if (curve.collider != null)
                    {
                        curve.collider.sharedMesh = curve.displayMesh;
                    }
                }
                curve.lastMeshUpdateEndTime = MeshGenerator.lastUpdateTime;
            }
            if (curve.lastMeshUpdateStartTime != MeshGenerator.lastUpdateTime)
            {
                MeshGenerator.StartGenerating(curve);
            }
        }
        if (curve.HaveCurveSettingsChanged())
        {

            curve.RequestMeshUpdate();
        }
    }
    private const float maxDistance = 5;
    ClickHitData GetClosestElementToCursor(IComposite root,Vector2 clickPosition,EventType eventType)
    {
        ClickHitData GetFrom(IEnumerable<ClickHitData> lst)
        {
            var clicks = lst.OrderBy(a => a.distanceFromCamera);
            foreach (var i in clicks)
                if (i.owner.DistanceFromMouse(clickPosition) < maxDistance)
                    return i;
            return null;
        }
        List<ClickHitData> hits = new List<ClickHitData>();
        root.Click(clickPosition, hits, eventType);
        var highPriority = hits.Where(a => !a.isLowPriority);
        var high = GetFrom(highPriority);
        if (high != null)
            return high;
        var lowPriority = hits.Where(a => a.isLowPriority);
        var low = GetFrom(lowPriority);
        return low;
    }
}
/*
//we could dick around trying to draw our own lines/icons so that we can sort using the depth buffer, but I say we do that later
GL.PushMatrix();
curve3d.testmat.SetPass(0);
GL.LoadOrtho();
GL.Begin(GL.TRIANGLES);
GL.Color(Color.green);
GL.Vertex3(0, 0, -10f);
GL.Vertex3(1, 1, -10f);
GL.Vertex3(0, 1, -10f);
GL.Color(Color.blue);
GL.Vertex3(0, 0, -5f);
GL.Vertex3(1, 1, -5f);
GL.Vertex3(0, 1, -5f);
GL.End();
GL.PopMatrix();
*/
