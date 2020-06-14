using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class TransformBlob
    {
        private Transform _baseTransform;
        public DynamicMatrix4x4 _additionalTransform;
        public TransformBlob(Transform baseTransform, DynamicMatrix4x4 additionalTransform = null) {
            _baseTransform = baseTransform;
            _additionalTransform = additionalTransform;
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
            var retr = _baseTransform.TransformPoint(point);
            if (_additionalTransform!=null)
                retr = _additionalTransform.GetMatrix() * ToHomo(retr);
            return retr;
        }
        public Vector3 TransformDirection(Vector3 direction)
        {
            var retr = _baseTransform.TransformDirection(direction);
            if (_additionalTransform!=null)
                retr = _additionalTransform.GetMatrix() * ToHomoDirection(retr);
            return retr;
        }
        public Vector3 InverseTransformPoint(Vector3 point)
        {
            Vector3 retr = point;
            if (_additionalTransform != null)
                retr = _additionalTransform.GetMatrix().inverse * ToHomo(retr);
            return _baseTransform.InverseTransformPoint(retr);
        }
    }
    public interface IPointOnCurveProvider
    {
        PointOnCurve PointOnCurve { get; }
    }
    public class DynamicMatrix4x4
    {
        private IPointOnCurveProvider point;
        public DynamicMatrix4x4(IPointOnCurveProvider point)
        {
            this.point = point;
        }
        public Matrix4x4 GetMatrix()
        {
            var pointOnCurve = point.PointOnCurve;
            return Matrix4x4.Translate(pointOnCurve.position)*Matrix4x4.Rotate(Quaternion.LookRotation(pointOnCurve.tangent,pointOnCurve.reference));
        }
    }
}
