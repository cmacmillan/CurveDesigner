using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Curve3D : MonoBehaviour
{
    public BeizerCurve curve;
    [Range(.01f,5)]
    public float sampleRate = 1.0f;
    void Start()
    {
    }

    void Update()
    {
        
    }
}
