using TriageTrace.Models;

namespace TriageTrace.Networking
{
    public sealed class LatestPoseStateQueue
    {
        private readonly object _gate = new object();
        private PosePointerState _latest;
        private long _highestSequence = -1;

        public bool TryEnqueue(PosePointerState state)
        {
            if (state == null)
            {
                return false;
            }

            lock (_gate)
            {
                if (state.Sequence <= _highestSequence)
                {
                    return false;
                }

                _highestSequence = state.Sequence;
                _latest = state;
                return true;
            }
        }

        public bool TryDequeue(out PosePointerState state)
        {
            lock (_gate)
            {
                state = _latest;
                _latest = null;
                return state != null;
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                _latest = null;
                _highestSequence = -1;
            }
        }
    }
}
