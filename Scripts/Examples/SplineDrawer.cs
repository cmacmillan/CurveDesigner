using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner.Examples
{
    [RequireComponent(typeof(Camera))]
    public class SplineDrawer : MonoBehaviour
    {
        public float distanceBetweenPoints = 1;
        private Curve3D currentCurve;
        private Camera cam;
        private void Start()
        {
            cam = GetComponent<Camera>();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition),out var hit))
                {
                    var obj = new GameObject();
                    obj.transform.position = hit.transform.position;
                    currentCurve = obj.AddComponent<Curve3D>();
                    currentCurve.Initialize(false);
                    currentCurve.type = MeshGenerationMode.Cylinder;
                    currentCurve.arcOfTubeSampler.constValue = 360.0f;
                    currentCurve.positionCurve.automaticTangents = true;
                    currentCurve.positionCurve.PointGroups[0].SetPositionLocal(PointGroupIndex.Position, hit.point);
                }
            }
            if (currentCurve != null)
            {
                if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition),out var hit))
                {
                    var points = currentCurve.positionCurve.PointGroups;
                    var previous = points[points.Count - 2];
                    var curr = points[points.Count - 1];
                    var localPosition = currentCurve.gameObject.transform.InverseTransformPoint(hit.point);
                    curr.SetPositionLocal(PointGroupIndex.Position,localPosition);
                    if (Vector3.Distance(localPosition,previous.GetPositionLocal(PointGroupIndex.Position))>distanceBetweenPoints)
                    {
                        currentCurve.positionCurve.AppendPoint(false, false, localPosition);
                    }
                }
                currentCurve.Recalculate();
                currentCurve.RequestMeshUpdate();
                if (Input.GetMouseButtonUp(0))
                {
                    currentCurve = null;
                }
            }
        }
    }
}
