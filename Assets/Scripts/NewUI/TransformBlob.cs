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
        public Vector3 TransformPoint(Vector3 point)
        {
            var retr = _baseTransform.TransformPoint(point);
            if (_additionalTransform!=null)
                retr = _additionalTransform.GetMatrix() * ToHomo(retr);
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
    public class DynamicMatrix4x4
    {
        private IPositionProvider centerPosition;
        public DynamicMatrix4x4(IPositionProvider centerPosition)
        {
            this.centerPosition = centerPosition;
        }
        public Matrix4x4 GetMatrix()
        {
            return Matrix4x4.Translate(centerPosition.Position);
        }
    }
}
