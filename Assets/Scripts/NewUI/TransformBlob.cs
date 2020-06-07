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
        private Matrix4x4? _additionalTransform;
        public TransformBlob(Transform baseTransform, Matrix4x4? additionalTransform = null) {
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
            if (_additionalTransform.HasValue)
                retr = _additionalTransform.Value * ToHomo(retr);
            return retr;
        }
    }

}
