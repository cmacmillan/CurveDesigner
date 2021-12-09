using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    //This script allows you to snap an object to the surface of a Curve3D
    [ExecuteAlways]
    public class ObjectOnCurve : MonoBehaviour
    {
        [Range(0, 1)]
        public float crosswisePosition;
        public float lengthwisePosition;
        public bool attachedToFront = true;
        [HideInInspector]
        public Curve3D curve;
        void Update()
        {
            if (curve != null && curve.type != MeshGenerationMode.Mesh && curve.type != MeshGenerationMode.NoMesh)
            {
                transform.position = curve.GetPointOnSurface(lengthwisePosition, crosswisePosition, out Vector3 normal, out Vector3 tangent, attachedToFront);
                transform.rotation = Quaternion.LookRotation(tangent, normal);
            }
        }
    }
}
