using Newtonsoft.Json;

namespace TriageTrace.Models
{
    public enum MessageKind
    {
        Rejected,
        HandGestureV1,
        PosePointerV2
    }

    public enum PoseTrackingState
    {
        Tracking,
        Partial,
        Lost
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class PointerDto
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class JointDto
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class RightArmJointsDto
    {
        [JsonProperty("rightShoulder")]
        public JointDto RightShoulder { get; set; }

        [JsonProperty("rightElbow")]
        public JointDto RightElbow { get; set; }

        [JsonProperty("rightWrist")]
        public JointDto RightWrist { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class RightArmVisibilityDto
    {
        [JsonProperty("rightShoulder")]
        public double RightShoulder { get; set; }

        [JsonProperty("rightElbow")]
        public double RightElbow { get; set; }

        [JsonProperty("rightWrist")]
        public double RightWrist { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class PosePointerMessageV2Dto
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("sequence")]
        public long Sequence { get; set; }

        [JsonProperty("tracking")]
        public string Tracking { get; set; }

        [JsonProperty("pointing")]
        public bool Pointing { get; set; }

        [JsonProperty("pointer")]
        public PointerDto Pointer { get; set; }

        [JsonProperty("joints")]
        public RightArmJointsDto Joints { get; set; }

        [JsonProperty("visibility")]
        public RightArmVisibilityDto Visibility { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class FingerStatesDto
    {
        [JsonProperty("thumb")]
        public bool Thumb { get; set; }

        [JsonProperty("index")]
        public bool Index { get; set; }

        [JsonProperty("middle")]
        public bool Middle { get; set; }

        [JsonProperty("ring")]
        public bool Ring { get; set; }

        [JsonProperty("pinky")]
        public bool Pinky { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class HandGestureMessageV1Dto
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("sequence")]
        public long Sequence { get; set; }

        [JsonProperty("handDetected")]
        public bool HandDetected { get; set; }

        [JsonProperty("handedness")]
        public string Handedness { get; set; }

        [JsonProperty("gesture")]
        public string Gesture { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("fingerStates")]
        public FingerStatesDto FingerStates { get; set; }
    }

    public sealed class PosePointerState
    {
        public PosePointerState(
            long timestamp,
            long sequence,
            PoseTrackingState tracking,
            bool pointing,
            PointerDto pointer,
            RightArmJointsDto joints,
            RightArmVisibilityDto visibility)
        {
            Timestamp = timestamp;
            Sequence = sequence;
            Tracking = tracking;
            Pointing = pointing;
            Pointer = pointer;
            Joints = joints;
            Visibility = visibility;
        }

        public long Timestamp { get; }
        public long Sequence { get; }
        public PoseTrackingState Tracking { get; }
        public bool Pointing { get; }
        public PointerDto Pointer { get; }
        public RightArmJointsDto Joints { get; }
        public RightArmVisibilityDto Visibility { get; }
    }
}
