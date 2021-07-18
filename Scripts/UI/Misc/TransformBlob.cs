using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    public class TransformBlob
    {
        private Transform _baseTransform;
        public DynamicMatrix4x4 _additionalTransform;
        public Matrix4x4? _additionalMatrix=null;
        public Matrix4x4? _additionalMatrixInverse=null;
        public Curve3D curve;
        public void Bake()
        {
            if (_additionalTransform != null)
            {
                _additionalMatrix = _additionalTransform.GetMatrix(curve);
                _additionalMatrixInverse = _additionalMatrix.Value.inverse;
            }
        }
        /// <summary>
        /// Should only really be instantiated by a curve, and referenced by everyone else
        /// </summary>
        public TransformBlob(Transform baseTransform, DynamicMatrix4x4 additionalTransform = null,Curve3D curve= null) {
            _baseTransform = baseTransform;
            _additionalTransform = additionalTransform;
            this.curve = curve;
        }
        Vector4 ToHomo(Vector3 vect)
        {
            return new Vector4(vect.x,vect.y,vect.z,1);
        }
        Vector4 ToHomoDirection(Vector3 vect)
        {
            return new Vector4(vect.x,vect.y,vect.z,0);
        }
        public Vector3 TransformPoint(Vector3 point)
        {
            Vector3 retr=point;
            if (_additionalTransform!=null)
                retr = _additionalMatrix.Value * ToHomo(retr);
            retr = _baseTransform.TransformPoint(retr);
            return retr;
        }
        public Vector3 TransformDirection(Vector3 direction)
        {
            Vector3 retr = direction;
            if (_additionalTransform!=null)
                retr = _additionalMatrix.Value * ToHomoDirection(retr);
            retr = _baseTransform.TransformDirection(retr);
            return retr;
        }
        public Vector3 InverseTransformPoint(Vector3 point)
        {
            Vector3 retr = point;
            retr = _baseTransform.InverseTransformPoint(retr);
            if (_additionalTransform != null)
                retr = _additionalMatrixInverse.Value * ToHomo(retr);
            return retr;
        }
    }
    public class DynamicMatrix4x4
    {
        private IPointOnCurveProvider point;
        public DynamicMatrix4x4(IPointOnCurveProvider point)
        {
            this.point = point;
        }
        public Matrix4x4 GetMatrix(Curve3D curve)
        {
            var pointOnCurve = point.PointOnCurve;
            float degrees = 0; 
            if (curve!=null)
            {
                degrees = curve.rotationSampler.GetValueAtDistance(pointOnCurve.distanceFromStartOfCurve,curve.positionCurve);
            }
            return Matrix4x4.Translate(pointOnCurve.position) * Matrix4x4.Rotate(Quaternion.AngleAxis(degrees,pointOnCurve.tangent)*Quaternion.LookRotation(pointOnCurve.tangent, pointOnCurve.reference));
        }
    }
}
