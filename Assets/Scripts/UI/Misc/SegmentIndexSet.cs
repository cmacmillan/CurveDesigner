using System.Collections;
using System.Collections.Generic;

namespace ChaseMacMillan.CurveDesigner
{
    public class SegmentIndexSet : IEnumerable<int>
    {
        public SegmentIndexSet(BezierCurve curve)
        {
            this.curve = curve;
        }
        private BezierCurve curve;
        private HashSet<int> set = new HashSet<int>();
        public void Add(int segmentIndex)
        {
            if (curve.isClosedLoop)
            {
                int lower = SelectableGUID.mod(segmentIndex - 1, curve.NumSegments);
                int upper = SelectableGUID.mod(segmentIndex, curve.NumSegments);
                set.Add(lower);
                set.Add(upper);
            }
            else
            {
                int lower = segmentIndex - 1;
                int upper = segmentIndex;
                if (lower >= 0)
                    set.Add(lower);
                if (upper < curve.NumSegments)
                    set.Add(upper);
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            return set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return set.GetEnumerator();
        }
    }
}
