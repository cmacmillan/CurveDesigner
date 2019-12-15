using UnityEngine;

public class SampleFragment
{
    public Vector3 position;
    public int segmentIndex;
    /// <summary>
    /// 
    /// </summary>
    public float time;
    /// <summary>
    /// The distance along the curve in real units
    /// </summary>
    public float distanceAlongCurve;
    public SampleFragment(Vector3 position, int segmentIndex, float time, float distanceAlongSegment)
    {
        this.position = position;
        this.segmentIndex = segmentIndex;
        this.time = time;
        this.distanceAlongCurve = distanceAlongSegment;
    }
}
