using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public interface IDistanceSampler<T> where T : struct
    {
        T GetValueAtDistance(float distance,bool isClosedLoop,float curveLength,BezierCurve curve);
    }
}
