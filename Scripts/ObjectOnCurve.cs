using UnityEngine;
namespace ChaseMacMillan.CurveDesigner
{
    //This script allows you to snap an object to the surface of a Curve3D
    [ExecuteAlways]
    public class ObjectOnCurve : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Range(0, 1)]
        public float crosswisePosition;
        [HideInInspector]
        public float old_crosswisePosition;

        public float lengthwisePosition;
        [HideInInspector]
        public float old_lengthwisePosition;

        [HideInInspector]
        public Vector3 old_localPosition;
        [HideInInspector]
        public Transform old_parent;
        [HideInInspector]
        public Quaternion old_rotation;

        public bool attachedToFront = true;
        [HideInInspector]
        public bool old_attachedToFront;

        public bool updateOnlyWhenModified = true;//Set to false if the curve needs to change shape at runtime in a way that would affect the position/rotation of this ObjectOnCurve. Setting this to true can be expensive

        private bool wantsUpdatePosition = false;

        public Curve3D curve;
        [HideInInspector]
        public Curve3D old_curve=null;
        private void Start()
        {
            Bind();
            UpdatePositionAndRotation();
        }
        public void Update()
        {
            bool curveChanged = CheckCurveChanged();
            if (updateOnlyWhenModified)
            {
                if (wantsUpdatePosition || 
                    curveChanged || 
                    old_attachedToFront != attachedToFront || 
                    old_crosswisePosition != crosswisePosition || 
                    old_lengthwisePosition != lengthwisePosition || 
                    transform.parent!=old_parent || 
                    transform.localPosition!=old_localPosition ||
                    transform.rotation != old_rotation)
                {
                    UpdatePositionAndRotation();
                }
            }
            else
            {
                UpdatePositionAndRotation();
            }
        }
        void OnDestroy()
        {
            CheckCurveChanged();
            if (curve != null)
            {
                curve.objectsOnThisCurve.Remove(this);
            }
        }
        void UpdatePositionAndRotation()
        {
            if (curve != null && curve.type != MeshGenerationMode.Mesh && curve.type != MeshGenerationMode.NoMesh)
            {
                transform.position = curve.GetPointOnSurface(lengthwisePosition, crosswisePosition, out Vector3 normal, out Vector3 tangent, attachedToFront);
                transform.rotation = Quaternion.LookRotation(tangent, normal);
            }
            wantsUpdatePosition = false;
            old_attachedToFront = attachedToFront;
            old_crosswisePosition = crosswisePosition;
            old_lengthwisePosition = lengthwisePosition;
            old_localPosition = transform.localPosition;
            old_parent = transform.parent;
            old_rotation = transform.rotation;
        }
        bool CheckCurveChanged()
        {
            if (curve != old_curve)
            {
                Bind();
                old_curve = curve;
                return true;
            }
            return false;
        }
        private void Bind()
        {
            if (old_curve != null)
                old_curve.objectsOnThisCurve.Remove(this);
            if (curve != null && !curve.objectsOnThisCurve.Contains(this))
                curve.objectsOnThisCurve.Add(this);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            wantsUpdatePosition = true;
        }
    }
}
