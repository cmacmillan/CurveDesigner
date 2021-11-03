using System.Collections.Generic;
using UnityEngine;

namespace ChaseMacMillan.CurveDesigner
{
    //A class which defines a chain of 3rd order bezier curves (4 control points per segment)
    [System.Serializable]
    public partial class BezierCurve : IActiveElement, ISerializationCallbackReceiver
    {
        [SerializeField]
        [HideInInspector]
        public List<PointGroup> PointGroups;
        public int NumControlPoints { get { return isClosedLoop ? PointGroups.Count * 3 : PointGroups.Count * 3 - 2; } }
        public int NumSegments { get { return isClosedLoop ? PointGroups.Count : PointGroups.Count - 1; } }

        public DimensionLockMode dimensionLockMode = DimensionLockMode.none;
        public CurveNormalGenerationMode normalGenerationMode = CurveNormalGenerationMode.MinimumDistance;
        public Curve3D owner;
        public bool isClosedLoop=false;
        public bool automaticTangents = false;
        public const float tangentSmoothingMin = .001f;
        [Range(tangentSmoothingMin,1)]
        public float automaticTangentSmoothing = .35f;
        [System.NonSerialized]//serializing the segments is slow
        [HideInInspector]
        public List<Segment> segments = null;
        private Vector3 GetTangent(int index)
        {
            return (this[index + 1] - this[index]);//.normalized;
        }

        public int NumSelectables(Curve3D curve) { return PointGroups.Count; }

        public ISelectable GetSelectable(int index, Curve3D curve)
        {
            return PointGroups[index];
        }

        public List<SelectableGUID> SelectAll(Curve3D curve)
        {
            List<SelectableGUID> retr = new List<SelectableGUID>();
            foreach (var i in PointGroups)
                retr.Add(i.GUID);
            return retr;
        }
        #region deleting
        private class PointGroupRun
        {
            public List<PointGroup> points = new List<PointGroup>();
            public int start;
            public int end;
            public float runLength;
            public bool IsWithinRange(ISamplerPoint point)
            {
                return point.SegmentIndex >= start && point.SegmentIndex < end;
            }
            public void CalculateLength(BezierCurve curve)
            {
                end = start + points.Count;
                runLength = 0;
                for (int i = start; i < end; i++)
                    runLength += curve.segments[i].length;
            }
        }
        private class ISamplerPointDeleteTracker
        {
            public ISamplerPointDeleteTracker(ISamplerPoint point, float fractionAlongSegment, int newSegmentIndex)
            {
                this.point = point;
                this.fractionAlongSegment = fractionAlongSegment;
                this.newSegmentIndex = newSegmentIndex;
            }
            public ISamplerPoint point;
            public float fractionAlongSegment;
            public int newSegmentIndex;
        }
        public bool Delete(List<SelectableGUID> guids, Curve3D curve)
        {
            DontDeleteAllTheGuids(guids);
            bool beforeIsClosedLoop = isClosedLoop;
            if (!isClosedLoop)
            {
                isClosedLoop = true;
                Recalculate();
            }
            bool IsDeleted(PointGroup point) { return guids.Contains(point.GUID); }
            int currIndex = 0;
            PointGroup curr = PointGroups[0];
            bool isEnd = false;
            void Next()
            {
                currIndex++;
                if (currIndex < PointGroups.Count)
                    curr = PointGroups[currIndex];
                else
                    isEnd = true;
            }
            PointGroupRun GetRun()
            {
                PointGroupRun retr = new PointGroupRun();
                retr.start = currIndex;
                do
                {
                    retr.points.Add(curr);
                    Next();
                } while (!isEnd && IsDeleted(curr));
                retr.CalculateLength(this);
                return retr;
            }
            ///////////
            bool hasLeftRun = IsDeleted(curr);
            PointGroupRun leftRun = null;
            if (hasLeftRun)
                leftRun = GetRun();
            List<PointGroupRun> runs = new List<PointGroupRun>();
            while (!isEnd)
                runs.Add(GetRun());
            var rightRun = runs.Last();
            bool hasRightRun = IsDeleted(rightRun.points.Last());
            if (hasRightRun)
            {
                runs.Remove(rightRun);
                rightRun.runLength -= segments.Last().length;//ending on an open point made the right run include the open curve length
            }
            //////////
            //Save all the old fractions along the run
            void GetSamplerPointRun(ISamplerPoint point, out int index, out float fractionAlongSegment)
            {
                for (index = 0; index < runs.Count; index++)
                {
                    var run = runs[index];
                    if (run.IsWithinRange(point))
                    {
                        float lengthUpToSegmentStart = run.start == 0 ? 0 : segments[run.start - 1].cummulativeLength;
                        float distanceFromStartOfRun = point.GetDistance(this) - lengthUpToSegmentStart;
                        fractionAlongSegment = distanceFromStartOfRun / run.runLength;
                        return;
                    }
                }

                ///// handle closed loop
                {
                    float closedLoopRunLength = segments.Last().length;
                    index = runs.Count - 1;
                    if (hasRightRun)
                    {
                        closedLoopRunLength += rightRun.runLength;
                        index++;
                    }
                    if (hasLeftRun)
                        closedLoopRunLength += leftRun.runLength;
                    bool isInLeftRun = false;
                    if (hasLeftRun)
                        isInLeftRun = leftRun.IsWithinRange(point);
                    float distanceFromStartOfRun;
                    if (isInLeftRun)
                    {
                        float pointDist = point.GetDistance(this);
                        distanceFromStartOfRun = pointDist + segments.Last().length + rightRun.runLength;
                    }
                    else
                    {
                        float lengthUpToSegmentStart = segments[rightRun.start - 1].cummulativeLength;
                        float pointDist = point.GetDistance(this);
                        distanceFromStartOfRun = pointDist - lengthUpToSegmentStart;
                    }
                    fractionAlongSegment = distanceFromStartOfRun / closedLoopRunLength;
                    return;
                }
            }
            List<ISamplerPointDeleteTracker> samplerList = new List<ISamplerPointDeleteTracker>();
            foreach (var sampler in curve.DistanceSamplers)
                foreach (var samplerPoint in sampler.AllPoints())
                {
                    GetSamplerPointRun(samplerPoint, out int runIndex, out float fractionAlongSegment);
                    samplerList.Add(new ISamplerPointDeleteTracker(samplerPoint, fractionAlongSegment, runIndex));
                }
            /////////////// ACTUALLY DO THE DELETE
            if (!DeleteGuids(guids, curve))
                return false;
            ///////////////
            foreach (var i in samplerList)
            {
                float lengthUpToSegment = i.newSegmentIndex == 0 ? 0 : segments[i.newSegmentIndex - 1].cummulativeLength;
                i.point.SetDistance(lengthUpToSegment + i.fractionAlongSegment * segments[i.newSegmentIndex].length, curve.positionCurve, false);
            }
            foreach (var i in curve.DistanceSamplers)
                i.Sort(curve.positionCurve);
            if (isClosedLoop != beforeIsClosedLoop)
            {
                isClosedLoop = beforeIsClosedLoop;
                Recalculate();
            }
            return true;
        }
        public void DontDeleteAllTheGuids(List<SelectableGUID> guids)
        {
            int numRemaining = 0;
            PointGroup firstDeleted = null;
            PointGroup lastDeleted = null;
            for (int i = 0; i < PointGroups.Count; i++)
            {
                var pointGroup = PointGroups[i];
                if (!guids.Contains(pointGroup.GUID))
                {
                    numRemaining++;
                }
                else
                {
                    if (firstDeleted == null)
                        firstDeleted = pointGroup;
                    else
                        lastDeleted = pointGroup;
                }
            }
            if (numRemaining == 0 || numRemaining == 1)//both prevent firstDeleted
                guids.RemoveAt(guids.IndexOf(firstDeleted.GUID));
            if (numRemaining == 0)//only 0 prevents lastdeleted also
                guids.RemoveAt(guids.IndexOf(lastDeleted.GUID));
        }
        public bool DeleteGuids(List<SelectableGUID> guids, Curve3D curve)
        {
            bool didChange = SelectableGUID.Delete(ref PointGroups, guids, curve);
            RegenSegmentIndicies();
            if (!didChange)
                return false;
            Recalculate();
            return true;
        }
        #endregion

        public float WrappedDistanceBetween(float distance1, float distance2)
        {
            float length = GetLength();
            bool isClosed = isClosedLoop;
            float simpleDistance = Mathf.Abs(distance1 - distance2);
            if (isClosed)
            {
                float wrappedUpper = Mathf.Abs((distance2 - length) - distance1);
                float wrappedLower = Mathf.Abs((distance1 - length) - distance2);
                return Mathf.Min(simpleDistance, wrappedLower, wrappedUpper);
            }
            else
            {
                return simpleDistance;
            }
        }

        public BezierCurve()
        {
            PointGroups = new List<PointGroup>();
        }
        public BezierCurve(BezierCurve curveToClone, bool createNewGuids)
        {
            PointGroups = new List<PointGroup>();
            foreach (var i in curveToClone.PointGroups)
            {
                PointGroups.Add(new PointGroup(i, curveToClone.owner, this, createNewGuids));
            }
            this.isClosedLoop = curveToClone.isClosedLoop;
            this.automaticTangents = curveToClone.automaticTangents;
            this.automaticTangentSmoothing = curveToClone.automaticTangentSmoothing;
            this.segments = new List<Segment>(curveToClone.segments.Count);
            foreach (var i in curveToClone.segments)
                segments.Add(new Segment(i));
            this.dimensionLockMode = curveToClone.dimensionLockMode;
            this.owner = curveToClone.owner;
            this.RegenSegmentIndicies();
        }

        #region curve manipulation
        public void Initialize()
        {
            var pointA = new PointGroup(owner.placeLockedPoints, owner, this);
            pointA.SetPositionLocal(PointGroupIndex.Position, Vector3.zero);
            pointA.SetPositionLocal(PointGroupIndex.LeftTangent, new Vector3(-1, 0, 0));
            pointA.SetPositionLocal(PointGroupIndex.RightTangent, new Vector3(1, 0, 0));
            PointGroups.Add(pointA);
            var pointB = new PointGroup(owner.placeLockedPoints, owner, this);
            pointB.SetPositionLocal(PointGroupIndex.Position, new Vector3(1, 1, 0));
            pointB.SetPositionLocal(PointGroupIndex.LeftTangent, new Vector3(0, 1, 0));
            pointB.SetPositionLocal(PointGroupIndex.RightTangent, new Vector3(2, 1, 0));
            PointGroups.Add(pointB);
            RegenSegmentIndicies();
        }

        private void RegenSegmentIndicies()
        {
            for (int i = 0; i < PointGroups.Count; i++)
                PointGroups[i].segmentIndex = i;
        }
        public SelectableGUID AppendPoint(bool isPrepend, bool lockPlacedPoint, Vector3 newPointPos)
        {
            PointGroup fromPoint = isPrepend ? PointGroups[0] : PointGroups.Last();
            PointGroupIndex outTangent;//out of the curve
            PointGroupIndex inTangent;//into the curve
            if (isPrepend)
            {
                outTangent = PointGroupIndex.LeftTangent;
                inTangent = PointGroupIndex.RightTangent;
            }
            else
            {
                outTangent = PointGroupIndex.RightTangent;
                inTangent = PointGroupIndex.LeftTangent;
            }
            fromPoint.SetPositionLocal(outTangent, fromPoint.GetPositionLocal(inTangent, true));

            PointGroup newPoint = new PointGroup(lockPlacedPoint, owner, this);
            if (isPrepend)
                PointGroups.Insert(0, newPoint);
            else
                PointGroups.Add(newPoint);
            newPoint.SetPositionLocal(PointGroupIndex.Position, newPointPos);
            Vector3 middlePoint = (newPointPos + fromPoint.GetPositionLocal(PointGroupIndex.Position)) / 2.0f;
            newPoint.SetPositionLocal(inTangent, middlePoint);
            newPoint.SetPositionLocal(outTangent, newPoint.GetPositionLocal(inTangent, true));
            RegenSegmentIndicies();
            return newPoint.GUID;
        }
        public SelectableGUID InsertSegmentAfterIndex(ISegmentTime splitPoint, bool lockPlacedPoint, SplitInsertionNeighborModification shouldModifyNeighbors)
        {
            var prePointGroup = PointGroups[splitPoint.SegmentIndex];
            var postPointGroup = PointGroups[(splitPoint.SegmentIndex + 1) % PointGroups.Count];
            PointGroup newPoint = new PointGroup(lockPlacedPoint, owner, this);
            var basePosition = this.GetSegmentPositionAtTime(splitPoint.SegmentIndex, splitPoint.Time);
            newPoint.SetPositionLocal(PointGroupIndex.Position, basePosition);
            Vector3 leftTangent;
            Vector3 rightTangent;
            Vector3 preLeftTangent;
            Vector3 postRightTangent;
            SolvePositionAtTimeTangents(GetVirtualIndex(splitPoint.SegmentIndex, 0), 4, splitPoint.Time, out leftTangent, out rightTangent, out preLeftTangent, out postRightTangent);

            void prePointModify()
            {
                prePointGroup.SetPositionLocal(PointGroupIndex.RightTangent, preLeftTangent);
            }
            void postPointModify()
            {
                postPointGroup.SetPositionLocal(PointGroupIndex.LeftTangent, postRightTangent);
            }
            switch (shouldModifyNeighbors)
            {
                case SplitInsertionNeighborModification.RetainCurveShape:
                    prePointGroup.SetPointLocked(false);
                    postPointGroup.SetPointLocked(false);
                    prePointModify();
                    postPointModify();
                    break;
                default:
                    break;
            }

            //use the bigger tangent, this only matters if the point is locked
            if ((leftTangent - newPoint.GetPositionLocal(PointGroupIndex.Position)).magnitude < (rightTangent - newPoint.GetPositionLocal(PointGroupIndex.Position)).magnitude)
            {
                newPoint.SetPositionLocal(PointGroupIndex.LeftTangent, leftTangent);
                newPoint.SetPositionLocal(PointGroupIndex.RightTangent, rightTangent);
            }
            else
            {
                newPoint.SetPositionLocal(PointGroupIndex.RightTangent, rightTangent);
                newPoint.SetPositionLocal(PointGroupIndex.LeftTangent, leftTangent);
            }

            PointGroups.Insert(splitPoint.SegmentIndex + 1, newPoint);
            RegenSegmentIndicies();
            return newPoint.GUID;
        }

        public void AddDefaultSegment()
        {
            var finalPointGroup = PointGroups[PointGroups.Count - 1];
            var finalPointPos = finalPointGroup.GetPositionLocal(PointGroupIndex.Position);
            finalPointGroup.SetPositionLocal(PointGroupIndex.RightTangent, finalPointPos + new Vector3(1, 0, 0));
            var pointB = new PointGroup(owner.placeLockedPoints, owner, this);
            pointB.SetPositionLocal(PointGroupIndex.Position, finalPointPos + new Vector3(1, 1, 0));
            pointB.SetPositionLocal(PointGroupIndex.LeftTangent, finalPointPos + new Vector3(0, 1, 0));
            PointGroups.Add(pointB);
            RegenSegmentIndicies();
            Recalculate();
        }
        #endregion

        #region curve calculations
        //.private const float samplesPerUnit = 100.0f;
        private const int MaxSamples = 500;
        private const int samplesPerSegment = 10;
        private float GetAutoCurveDensity(float curveLength)
        {
            return Mathf.Max(curveLength / MaxSamples, curveLength / (samplesPerSegment * NumSegments));
        }

        public float GetDistanceAtSegmentIndexAndTime(int segmentIndex, float time)
        {
            if (segmentIndex == segments.Count && time == 0.0f)
                return segments[segments.Count - 1].GetDistanceAtTime(1.0f);
            var segmentLen = segments[segmentIndex].GetDistanceAtTime(time);
            if (segmentIndex > 0)
                return segments[segmentIndex - 1].cummulativeLength + segmentLen;
            return segmentLen;
        }

        public PointOnCurve GetPointAtDistance(float distance, bool needsTangent = true)
        {
            float length = GetLength();
            if (isClosedLoop && (distance<0 || distance>length))
                distance = Utils.ModFloat(distance, length);
            else
                distance = Mathf.Clamp(distance, 0, length);
            float remainingDistance = distance;
            for (int segmentIndex = 0; segmentIndex < NumSegments; segmentIndex++)
            {
                if (remainingDistance < segments[segmentIndex].length)
                {
                    float time = segments[segmentIndex].GetTimeAtLength(remainingDistance, out PointOnCurve lowerPoint, out Vector3 lowerReference);
                    Vector3 position = GetSegmentPositionAtTime(segmentIndex, time);
                    Vector3 tangent = Vector3.zero;
                    if (needsTangent)
                        tangent = GetSegmentTangentAtTime(segmentIndex, time);
                    var retr = new PointOnCurve(time, remainingDistance, position, segmentIndex, tangent);
                    retr.distanceFromStartOfCurve = retr.distanceFromStartOfSegment + (segmentIndex - 1 >= 0 ? segments[segmentIndex - 1].cummulativeLength : 0);
                    retr.CalculateReference(lowerPoint, lowerReference, this);
                    return retr;
                }
                remainingDistance -= segments[segmentIndex].length;
            }
            {
                int finalSegmentIndex = NumSegments - 1;
                float time = 1.0f;
                Vector3 position = GetSegmentPositionAtTime(finalSegmentIndex, time);
                Vector3 tangent = Vector3.zero;
                if (needsTangent)
                    tangent = GetSegmentTangentAtTime(finalSegmentIndex, time);
                var retr = new PointOnCurve(time, segments[finalSegmentIndex].length, position, finalSegmentIndex, tangent);
                retr.distanceFromStartOfCurve = retr.distanceFromStartOfSegment + (finalSegmentIndex - 1 >= 0 ? segments[finalSegmentIndex - 1].cummulativeLength : 0);
                var finalSegmentSamples = segments[finalSegmentIndex].samples;
                retr.reference = finalSegmentSamples[finalSegmentSamples.Count - 1].reference;
                return retr;
            }
        }

        public void SolvePositionAtTimeTangents(int startIndex, int length, float time, out Vector3 leftTangent, out Vector3 rightTangent, out Vector3 preLeftTangent, out Vector3 postRightTangent)
        {
            leftTangent = SolvePositionAtTime(startIndex, length - 1, time);
            rightTangent = SolvePositionAtTime(startIndex + 1, length - 1, time);

            preLeftTangent = SolvePositionAtTime(startIndex, length - 2, time);
            postRightTangent = SolvePositionAtTime(startIndex + 2, length - 2, time);
        }

        public Vector3 GetSegmentPositionAtTime(int segmentIndex, float time)
        {
            return SolvePositionAtTime(GetVirtualIndex(segmentIndex, 0), 4, time);
        }
        public Vector3 GetSegmentTangentAtTime(int segmentIndex, float time)
        {
            return SolveTangentAtTime(GetVirtualIndex(segmentIndex, 0), 3, time).normalized;
        }
        private Vector3 SolveTangentAtTime(int startIndex, int length, float time)
        {
            if (length == 2)
                return Vector3.Lerp(GetTangent(startIndex), GetTangent(startIndex + 1), time);
            Vector3 firstHalf = SolveTangentAtTime(startIndex, length - 1, time);
            Vector3 secondHalf = SolveTangentAtTime(startIndex + 1, length - 1, time);
            return Vector3.Lerp(firstHalf, secondHalf, time);
        }

        private Vector3 SolvePositionAtTime(int startIndex, int length, float time)
        {
            if (length == 2)
                return Vector3.Lerp(this[startIndex], this[startIndex + 1], time);
            Vector3 firstHalf = SolvePositionAtTime(startIndex, length - 1, time);
            Vector3 secondHalf = SolvePositionAtTime(startIndex + 1, length - 1, time);
            return Vector3.Lerp(firstHalf, secondHalf, time);
        }
        #endregion

        #region point locking
        public void SetPointLockState(int segmentIndex, int pointIndex, bool state)
        {
            SetPointLockState(GetVirtualIndex(segmentIndex, pointIndex), state);
        }
        public bool GetPointLockState(int segmentIndex, int pointIndex)
        {
            return GetPointLockState(GetVirtualIndex(segmentIndex, pointIndex));
        }
        public void SetPointLockState(int index, bool state)
        {
            PointGroups[GetPointGroupIndex(index)].SetPointLocked(state);
        }
        public bool GetPointLockState(int index)
        {
            return PointGroups[GetPointGroupIndex(index)].GetIsPointLocked();
        }
        #endregion


        #region length calculation
        public float GetLength()
        {
            return segments[NumSegments - 1].cummulativeLength;
        }
        public Vector3 GetDefaultReferenceVector(Vector3 tangent)
        {
            Vector3 reference;
            switch (dimensionLockMode)
            {
                case DimensionLockMode.x:
                    reference = Vector3.right;
                    break;
                case DimensionLockMode.y:
                    reference = Vector3.up;
                    break;
                case DimensionLockMode.z:
                    reference = Vector3.forward;
                    break;
                default:
                    switch (normalGenerationMode)
                    {
                        case CurveNormalGenerationMode.BiasTowardsForward:
                            reference = Vector3.forward;
                            break;
                        case CurveNormalGenerationMode.BiasTowardsRight:
                            reference = Vector3.right;
                            break;
                        case CurveNormalGenerationMode.BiasTowardsUp:
                        case CurveNormalGenerationMode.MinimumDistance:
                        default:
                            reference = Vector3.up;
                            break;
                    }
                    break;
            }
            var retr = Vector3.ProjectOnPlane(reference, tangent).normalized;
            return retr;
        }
        /// <summary>
        /// must call after modifying points
        /// </summary>
        public PointOnCurve Recalculate(PointOnCurve referenceHint = null, HashSet<int> recalculateOnlyIndicies = null)
        {
            if (automaticTangents)
            {
                int startIndex =0;
                int endIndex=PointGroups.Count;
                if (!isClosedLoop)
                {
                    startIndex = 1;
                    endIndex = PointGroups.Count - 1;
                    if (recalculateOnlyIndicies==null || recalculateOnlyIndicies.Contains(0))
                    {
                        PointGroups[0].SetPointLocked(true);//automatic tangents should lock all tangents
                        Vector3 firstPointStartPos = PointGroups[0].GetPositionLocal(PointGroupIndex.Position);
                        Vector3 firstPointVect = PointGroups[1].GetPositionLocal(PointGroupIndex.Position) - firstPointStartPos;
                        PointGroups[0].SetPositionLocal(PointGroupIndex.RightTangent, firstPointStartPos + firstPointVect * automaticTangentSmoothing);
                    }

                    if (recalculateOnlyIndicies==null || recalculateOnlyIndicies.Contains(PointGroups.Count-2))
                    {
                        PointGroups[PointGroups.Count - 1].SetPointLocked(true);
                        Vector3 lastPointStartPos = PointGroups[PointGroups.Count - 1].GetPositionLocal(PointGroupIndex.Position);
                        Vector3 lastPointVect = PointGroups[PointGroups.Count - 2].GetPositionLocal(PointGroupIndex.Position) - lastPointStartPos;
                        PointGroups[PointGroups.Count - 1].SetPositionLocal(PointGroupIndex.LeftTangent, lastPointStartPos + lastPointVect * automaticTangentSmoothing);
                    }
                }
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (recalculateOnlyIndicies == null || recalculateOnlyIndicies.Contains(i))
                    {
                        int prev = Utils.ModInt(i - 1, PointGroups.Count);
                        int next = Utils.ModInt(i + 1, PointGroups.Count);
                        Vector3 prevPos = PointGroups[prev].GetPositionLocal(PointGroupIndex.Position);
                        Vector3 thisPos = PointGroups[i].GetPositionLocal(PointGroupIndex.Position);
                        Vector3 nextPos = PointGroups[next].GetPositionLocal(PointGroupIndex.Position);
                        Vector3 reflectedPrevOffset = thisPos - prevPos;
                        Vector3 nextOffset = nextPos - thisPos;
                        Vector3 avgDirection = (reflectedPrevOffset.normalized + nextOffset.normalized).normalized;
                        float minLength = Mathf.Sqrt(Mathf.Min(reflectedPrevOffset.sqrMagnitude, nextOffset.sqrMagnitude));
                        PointGroups[i].SetPositionLocal(PointGroupIndex.RightTangent, thisPos + avgDirection * minLength * automaticTangentSmoothing);
                    }
                }
            }
            if (recalculateOnlyIndicies == null || segments == null)
            {
                if (segments == null)
                    segments = new List<Segment>();
                else
                    segments.Clear();
                for (int i = 0; i < NumSegments; i++)
                    segments.Add(new Segment(this, i, i == NumSegments - 1));
            }
            else
            {
                foreach (int recalculateOnlyIndex in recalculateOnlyIndicies)
                {
                    segments[recalculateOnlyIndex].Recalculate(this, recalculateOnlyIndex, recalculateOnlyIndex == NumSegments - 1);
                }
            }
            CalculateCummulativeLengths();
            ///Calculate reference vectors
            List<PointOnCurve> points = GetSamplePoints();
            {
                Vector3 referenceVector = GetDefaultReferenceVector(points[0].tangent);
                if (referenceHint != null)
                {
                    var rotation = Quaternion.FromToRotation(referenceHint.tangent, points[0].tangent);
                    if (Vector3.Dot(rotation * referenceHint.reference, referenceVector) < 0)
                        referenceVector = -referenceVector;
                }
                referenceVector = referenceVector.normalized;
                points[0].reference = referenceVector;
                for (int i = 1; i < points.Count; i++)
                {
                    var point = points[i];
                    point.CalculateReference(points[i - 1], referenceVector, this);
                    referenceVector = point.reference.normalized;
                    points[i].reference = referenceVector;
                }
            }
            if (isClosedLoop)
            {
                //angle difference between the final reference vector, and the first reference vector projected backwards
                Vector3 finalReferenceVector = points[points.Count - 1].reference;
                var point = points[points.Count - 1];
                point.CalculateReference(points[0], points[0].reference, this);
                Vector3 firstReferenceVectorProjectedBackwards = point.reference;
                float angleDifference = Vector3.SignedAngle(finalReferenceVector, firstReferenceVectorProjectedBackwards, points[points.Count - 1].tangent);
                for (int i = 1; i < points.Count; i++)
                    points[i].reference = Quaternion.AngleAxis((i / (float)(points.Count - 1)) * angleDifference, points[i].tangent) * points[i].reference;
            }
            return points[0];
        }
        public List<PointOnCurve> GetPointsWithSpacing(float spacing)
        {
            var retr = new List<PointOnCurve>();
            float length = GetLength();
            int numInLength = Mathf.CeilToInt(length / spacing);
            int sampleReduction = isClosedLoop ? 1 : 0;
            for (int i = 0; i <= numInLength - sampleReduction; i++)
                retr.Add(GetPointAtDistance(i * length / numInLength));
            return retr;
        }

        public List<PointOnCurve> GetSamplePoints()
        {
            List<PointOnCurve> retr = new List<PointOnCurve>();
            foreach (var i in segments)
                foreach (var j in i.samples)
                    retr.Add(j);
            return retr;
        }
        private void CalculateCummulativeLengths()
        {
            float cummulativeLength = 0;
            foreach (var i in segments)
            {
                foreach (var j in i.samples)
                    j.distanceFromStartOfCurve = j.distanceFromStartOfSegment + cummulativeLength;//we add the cummulative length not including the current segment
                cummulativeLength += i.length;
                i.cummulativeLength = cummulativeLength;
            }
        }
        #endregion

        #region point manipulation
        public Vector3 this[int virtualIndex]
        {
            get
            {
                return GetPointGroupByIndex(virtualIndex).GetPositionLocal(GetPointTypeByIndex(virtualIndex));
            }
            set
            {
                GetPointGroupByIndex(virtualIndex).SetPositionLocal(GetPointTypeByIndex(virtualIndex), value);
            }
        }
        public Vector3 this[int segmentVirtualIndex, int pointVirtualIndex]
        {
            get
            {
                int index = GetVirtualIndex(segmentVirtualIndex, pointVirtualIndex);
                return this[index];
            }
            set
            {
                int index = GetVirtualIndex(segmentVirtualIndex, pointVirtualIndex);
                this[index] = value;
            }
        }
        #endregion

        public PointGroupIndex GetPointTypeByIndex(int virtualIndex)
        {
            var length = PointGroups.Count * 3;
            if (virtualIndex == length)
                return PointGroupIndex.Position;
            if (virtualIndex == length - 1)
                return PointGroupIndex.LeftTangent;
            if (virtualIndex == length - 2)
                return PointGroupIndex.RightTangent;
            int offsetIndex = virtualIndex - GetParentVirtualIndex(virtualIndex);
            return (PointGroupIndex)offsetIndex;
        }
        public PointGroupIndex GetOtherTangentIndex(PointGroupIndex index)
        {
            switch (index)
            {
                case PointGroupIndex.LeftTangent:
                    return PointGroupIndex.RightTangent;
                case PointGroupIndex.RightTangent:
                    return PointGroupIndex.LeftTangent;
                case PointGroupIndex.Position:
                    return PointGroupIndex.Position;
                default:
                    throw new System.InvalidOperationException();
            }
        }
        public PointGroup GetPointGroupByIndex(int virtualIndex)
        {
            return PointGroups[GetPointGroupIndex(virtualIndex)];
        }

        public static int GetVirtualIndexByType(int parentPointGroupIndex, PointGroupIndex type)
        {
            int retrVal = parentPointGroupIndex * 3;
            if (type == PointGroupIndex.LeftTangent)
            {
                return retrVal - 1;
            }
            else if (type == PointGroupIndex.RightTangent)
            {
                return retrVal + 1;
            }
            return retrVal;
        }

        public int GetVirtualIndex(int segmentIndex, int pointIndex) { return segmentIndex * 3 + pointIndex; }
        public int GetParentVirtualIndex(int childVirtualIndex) { return GetPointGroupIndex(childVirtualIndex) * 3; }
        public int GetPointGroupIndex(int childIndex)
        {
            return ((childIndex + 1) / 3) % PointGroups.Count;
        }

        public string GetPointName()
        {
            return "position";
        }

        public void OnPositionEdited()
        {
            Recalculate();
        }

        public void OnBeforeSerialize() { /* Do Nothing*/ }

        public void OnAfterDeserialize()
        {
            foreach (var i in PointGroups)
                i.owner = this;
        }
    }
}
