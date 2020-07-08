using Assets.NewUI;
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

    public override void OnInspectorGUI()
    {
        var curve3d = (target as Curve3D);

        float width = Screen.width - 18; // -10 is effect_bg padding, -8 is inspector padding
        EditorGUIUtility.labelWidth = 0;
        EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth - 4;

        EditorGUILayout.BeginVertical();
        GUIStyle effectBgStyle = "ShurikenEffectBg";
        GUIStyle shurikenModuleBg = "ShurikenModuleBg";
        GUIStyle mixedToggleStyle = "ShurikenToggleMixed";
        GUIStyle initialHeaderStyle = "ShurikenEmitterTitle";
        GUIStyle nonInitialHeaderStyle = "ShurikenModuleTitle";
        bool isDisabled = false;

        GUILayout.BeginVertical(effectBgStyle);
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
                    headerStyle = initialHeaderStyle;
                } else
                {
                    headerHeight = 15;
                    headerStyle = nonInitialHeaderStyle;
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
                            GUI.Label(moduleSize, GUIContent.none, shurikenModuleBg);
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


        //GUILayout.Label("asdf"); 
        //base.OnInspectorGUI();
    }

    private void WindowFunc(int id)
    {
        var curve = (target as Curve3D);
        var editModes = curve.editModeCategories;
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        int skipCount = 0;
        if (curve.type != CurveType.DoubleBezier)
            skipCount++;
        for (int i = 0; i < editModes.editModes.Length; i++)
        {
            EditMode currMode = editModes.editModes[i];
            if (curve.type != CurveType.DoubleBezier && currMode == EditMode.DoubleBezier)
                continue;
            string currName = editModes.editmodeNameMap[currMode];
            string style;
            if (i == 0)
                style = "ButtonLeft";
            else if (i == editModes.editModes.Length - 1 - skipCount)
                style = "ButtonRight";
            else
                style = "ButtonMid";
            if (GUILayout.Toggle(curve.editMode == currMode, EditorGUIUtility.TrTextContent(currName), style))
                curve.editMode = currMode;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Label("asdf");
        GUILayout.Label("asdf");
        GUILayout.Label("asdf");
        if (GUILayout.Button("Clear"))
        {
            curve.positionCurve = new BezierCurve();
            curve.positionCurve.owner = curve;
            curve.positionCurve.Initialize();
            curve.positionCurve.isCurveOutOfDate = true;
            curve.sizeDistanceSampler = new FloatLinearDistanceSampler("Size");
            curve.rotationDistanceSampler = new FloatLinearDistanceSampler("Rotation (degrees)");
            curve.doubleBezierSampler = new DoubleBezierSampler();
            curve.UICurve = new UICurve(null, curve);
            curve.UICurve.Initialize();
            Debug.Log("cleared");
        }
        GUILayout.EndVertical();
        if (curve.UICurve != null)
        {
            var drawer = curve.UICurve.GetWindowDrawer();
            drawer.DrawWindow(curve);
        }
    }

    private void DrawSelectedUI()
    {
        //so basically we need to handle editing an individual point, and groups of points
        //each UI curve should describe how to handle it, when passed an array of ints 
    }

    private void OnSceneGUI()
    {
        var curve3d = (target as Curve3D);
        GUILayout.Window(61732234,new Rect(20, 20, 0, 0),WindowFunc,$"Editing {curve3d.editModeCategories.editmodeNameMap[curve3d.editMode]}");
        /*
        curve3d.positionCurve.Recalculate();
        foreach (var i in curve3d.doubleBezierSampler.secondaryCurves)
        {
            i.secondaryCurve.owner = curve3d;//gotta be careful that I'm not referencing stuff in owner that I shouldn't be
            i.secondaryCurve.Recalculate();
        }
        curve3d.CacheAverageSize();
        var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;//Consider replacing with Camera.current?
        if (curve3d.commandBuffer == null)
        {
            curve3d.commandBuffer = new CommandBuffer();
            sceneCam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, curve3d.commandBuffer);
        }
        var commandBuffer = curve3d.commandBuffer;
        commandBuffer.Clear();
        if (curve3d.testMesh != null && curve3d.testMat != null)
        {
            var second = curve3d.doubleBezierSampler.secondaryCurves[0];
            float farthestPoint = 0;
            foreach (var i in second.secondaryCurve.PointGroups)
            {
                void doit(PGIndex index){
                    float d = i.GetWorldPositionByIndex(index, DimensionLockMode.z).magnitude;
                    farthestPoint = Mathf.Max(farthestPoint, d);
                }
                doit(PGIndex.LeftTangent);
                doit(PGIndex.Position);
                doit(PGIndex.RightTangent);
            }
            curve3d.testMat.SetFloat("_Scale",farthestPoint);
            var dist = second.GetDistance(curve3d.positionCurve);
            var point = curve3d.positionCurve.GetPointAtDistance(dist);
            commandBuffer.DrawMesh(curve3d.testMesh, Matrix4x4.Translate(point.position)*Matrix4x4.Rotate(Quaternion.LookRotation(point.tangent,point.reference))*Matrix4x4.Scale(Vector3.one*farthestPoint*2), curve3d.testMat);
        }
        */

        /*
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(20, 20, 210, 60));
        var rect = EditorGUILayout.BeginVertical();
        GUI.color = Color.yellow;
        GUI.Box(rect, GUIContent.none);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Edit Curve");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.color = Color.red;
        if (GUILayout.Button("Clear"))
        {
            Debug.Log("cleared");
            curve3d.selectedPointsIndex.Clear();
            curve3d.hotPointIndex = -1;
            curve3d.positionCurve.Initialize();
            curve3d.positionCurve.isCurveOutOfDate = true;
            curve3d.sizeDistanceSampler = new FloatLinearDistanceSampler();
            curve3d.rotationDistanceSampler = new FloatLinearDistanceSampler();
            curve3d.UICurve.Initialize();
        }
        if (GUILayout.Button("Lock"))
        {
            foreach (var i in curve3d.positionCurve.PointGroups)
            {
                i.SetPointLocked(true);
            }
        }
        if (GUILayout.Button("Export to Obj"))
        {
            ObjMeshExporter.DoExport(curve3d.gameObject,false);
        }
        //GUI.color = Color.white;
        //EditorGUI.CurveField(new Rect(0, 0, 20, 20),curve.curveSizeAnimationCurve);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        Handles.EndGUI();
        */

        ///
        curve3d.positionCurve.owner = curve3d;
        curve3d.positionCurve.isClosedLoop = curve3d.isClosedLoop;
        curve3d.positionCurve.dimensionLockMode = curve3d.lockToPositionZero;
        curve3d.positionCurve.Recalculate();
        var secondaryCurves = curve3d.doubleBezierSampler.secondaryCurves;
        if (secondaryCurves.Count > 0)
        {
            foreach (var curr in secondaryCurves)
                curr.secondaryCurve.owner = curve3d;//gotta be careful that I'm not referencing stuff in owner that I shouldn't be
            var referenceHint = secondaryCurves[0].secondaryCurve.Recalculate();
            for (int i = 1; i < secondaryCurves.Count; i++)
                referenceHint = secondaryCurves[i].secondaryCurve.Recalculate(referenceHint);
        }
        curve3d.CacheAverageSize();
        var rotationPoints = curve3d.rotationDistanceSampler.GetPoints(curve3d);
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
        UpdateMesh(curve3d);
        var curveEditor = curve3d.UICurve;
        var MousePos = Event.current.mousePosition;
        int controlID = GUIUtility.GetControlID(_CurveHint, FocusType.Passive);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.KeyDown:
                if (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace)
                {
                    bool didDelete = false;
                    foreach (var i in curve3d.Deleteables)
                        didDelete |= i.Delete(curve3d.selectedPoints,curve3d);
                    if (didDelete)
                    {
                        curve3d.RequestMeshUpdate();
                        curve3d.UICurve.Initialize();
                    }
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.A && Event.current.control)
                {
                    Debug.Log("Select All!");
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
                            curve3d.editMode = EditMode.PositionCurve;
                            break;
                    }
                    Event.current.Use();
                }
                break;
            case EventType.Repaint:
                Draw(curveEditor, MousePos, elementClickedDown,curve3d.selectedPoints);
                break;
            case EventType.MouseDown:
                if (Event.current.button == 0)
                {
                    var clicked = GetClosestElementToCursor(curveEditor, MousePos);
                    if (clicked != null)
                    {
                        GUIUtility.hotControl = controlID;
                        curve3d.elementClickedDown = clicked;
                        var clickPos = MousePos + clicked.offset;
                        var commandToExecute = clicked.owner.GetClickCommand();
                        commandToExecute.ClickDown(clickPos);
                        if (Event.current.control)
                        {
                            curve3d.ToggleSelectPoint(clicked.owner.Guid);
                        }
                        else
                        {
                            curve3d.DeselectAllPoints();
                            curve3d.ToggleSelectPoint(clicked.owner.Guid);
                        }
                        commandToExecute.ClickDrag(clickPos, curve3d, clicked);
                        curve3d.RequestMeshUpdate();
                        Event.current.Use();
                    }
                }
                break;
            case EventType.MouseDrag:
                if (elementClickedDown != null)
                {
                    var clickPos = MousePos + elementClickedDown.offset;
                    var commandToExecute = elementClickedDown.owner.GetClickCommand();
                    commandToExecute.ClickDrag(clickPos,curve3d,elementClickedDown);
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
                        commandToExecute.ClickUp(MousePos);
                        curve3d.RequestMeshUpdate();
                        curve3d.elementClickedDown = null;
                        Event.current.Use();
                    }
                }
                break;
            case EventType.MouseMove:
                HandleUtility.Repaint();
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
                    if (MeshGenerator.hasUVs)
                        curve.displayMesh.SetUVs(0, MeshGenerator.uvs);
                    curve.displayMesh.RecalculateNormals();
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
    private const float SmallClickRadius = 5;
    private const float LargeClickRadius = 20;
    ClickHitData GetClosestElementToCursor(IComposite root,Vector2 clickPosition)
    {
        ClickHitData GetHit(List<ClickHitData> hits)
        {
            var veryClosehits = hits.Where(a => a.distanceFromClick < SmallClickRadius).ToList();
            if (veryClosehits.Count > 0)
            {
                veryClosehits.Sort((a, b) => (int)Mathf.Sign(a.distanceFromCamera - b.distanceFromCamera));
                return veryClosehits[0];
            }
            else
            {
                var somewhatCloseHits = hits.Where(a => a.distanceFromClick < LargeClickRadius).ToList();
                if (somewhatCloseHits.Count > 0)
                {
                    somewhatCloseHits.Sort((a, b) => (int)Mathf.Sign(a.distanceFromClick - b.distanceFromClick));
                    return somewhatCloseHits[0];
                }
                return null;
            }
        }
        List<ClickHitData> hitsList = new List<ClickHitData>();
        root.Click(clickPosition, hitsList);

        var highPriorityHits = hitsList.Where(a=>!a.isLowPriority).ToList();
        var highPriorityItem = GetHit(highPriorityHits);
        if (highPriorityItem != null)
            return highPriorityItem;

        var lowPriorityHits = hitsList.Where(a=>a.isLowPriority).ToList();
        var lowPriorityItem = GetHit(lowPriorityHits);
        if (lowPriorityItem != null)
            return lowPriorityItem;

        return null;
    }

    void Draw(IComposite root,Vector2 mousePos,ClickHitData currentlyHeldDown,List<SelectableGUID> selected)
    {
        ClickHitData closestElementToCursor = null;
        if (currentlyHeldDown==null)
            closestElementToCursor = GetClosestElementToCursor(root,mousePos);
        List<IDraw> draws = new List<IDraw>();
        root.Draw(draws,closestElementToCursor);
        draws.Sort((a, b) => (int)(Mathf.Sign(b.DistFromCamera() - a.DistFromCamera())));
        foreach (var draw in draws)
        {
            SelectionState selectionState = SelectionState.unselected;
            var guid = draw.Creator().GUID;
            if (selected.Count>0 && guid==selected[0])
                selectionState = SelectionState.primarySelected;
            else if (selected.Contains(guid))
                selectionState = SelectionState.secondarySelected;
            if (draw.DistFromCamera() > 0)
            {
                if (closestElementToCursor != null && draw.Creator() == closestElementToCursor.owner)
                    draw.Draw(DrawMode.hovered,selectionState);
                else if (currentlyHeldDown != null && draw.Creator() == currentlyHeldDown.owner)
                    draw.Draw(DrawMode.clicked,selectionState);
                else
                    draw.Draw(DrawMode.normal,selectionState);
            }
        }
    }
}
/*
//we could dick around trying to draw our own lines/icons so that we can sort using the depth buffer, but I say we do that laterS
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
