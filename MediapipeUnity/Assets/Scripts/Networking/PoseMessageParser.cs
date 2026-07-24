using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TriageTrace.Models;

namespace TriageTrace.Networking
{
    public static class PoseMessageParser
    {
        private const double PartialVisibilityThreshold = 0.5;

        private static readonly HashSet<string> GestureValues =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "ROCK",
                "SCISSORS",
                "PAPER",
                "UNKNOWN",
                "NO_HAND"
            };

        public static bool TryParse(
            string json,
            out MessageKind kind,
            out PosePointerState poseState,
            out string error)
        {
            kind = MessageKind.Rejected;
            poseState = null;
            error = string.Empty;

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (Exception exception) when (
                exception is JsonException || exception is ArgumentException)
            {
                error = $"Invalid JSON: {exception.Message}";
                return false;
            }

            if (!TryString(root["type"], out string type) ||
                !TryInteger(root["version"], out int version))
            {
                error = "Message requires string type and integer version.";
                return false;
            }

            if (type == "hand_gesture" && version == 1)
            {
                if (!ValidateHandGestureV1(root, out error))
                {
                    return false;
                }

                kind = MessageKind.HandGestureV1;
                return true;
            }

            if (type != "pose_pointer" || version != 2)
            {
                error = $"Unsupported message type/version: {type}/{version}.";
                return false;
            }

            if (!TryParsePoseV2(root, out poseState, out error))
            {
                return false;
            }

            kind = MessageKind.PosePointerV2;
            return true;
        }

        private static bool TryParsePoseV2(
            JObject root,
            out PosePointerState state,
            out string error)
        {
            state = null;
            error = string.Empty;

            if (!HasProperties(
                    root,
                    "timestamp",
                    "sequence",
                    "tracking",
                    "pointing",
                    "pointer",
                    "joints",
                    "visibility"))
            {
                error = "Pose v2 message is missing a required field.";
                return false;
            }

            if (!TryNonNegativeLong(root["timestamp"], out long timestamp) ||
                !TryNonNegativeLong(root["sequence"], out long sequence))
            {
                error = "timestamp and sequence must be non-negative integers.";
                return false;
            }

            if (!TryString(root["tracking"], out string trackingText) ||
                !TryTracking(trackingText, out PoseTrackingState tracking))
            {
                error = "Unknown tracking state.";
                return false;
            }

            if (root["pointing"].Type != JTokenType.Boolean)
            {
                error = "pointing must be a boolean.";
                return false;
            }

            bool pointing = root["pointing"].Value<bool>();
            if (!ValidatePointer(root["pointer"], out bool pointerIsNull, out error))
            {
                return false;
            }

            if (!(root["joints"] is JObject joints) ||
                !HasProperties(
                    joints,
                    "rightShoulder",
                    "rightElbow",
                    "rightWrist"))
            {
                error = "joints must contain all right-arm keys.";
                return false;
            }

            if (!ValidateJoint(
                    joints["rightShoulder"],
                    out bool shoulderIsNull,
                    out error) ||
                !ValidateJoint(
                    joints["rightElbow"],
                    out bool elbowIsNull,
                    out error) ||
                !ValidateJoint(
                    joints["rightWrist"],
                    out bool wristIsNull,
                    out error))
            {
                return false;
            }

            if (!(root["visibility"] is JObject visibility) ||
                !HasProperties(
                    visibility,
                    "rightShoulder",
                    "rightElbow",
                    "rightWrist") ||
                !TryVisibility(
                    visibility["rightShoulder"],
                    out double shoulderVisibility) ||
                !TryVisibility(
                    visibility["rightElbow"],
                    out double elbowVisibility) ||
                !TryVisibility(
                    visibility["rightWrist"],
                    out double wristVisibility))
            {
                error = "visibility requires three values between 0 and 1.";
                return false;
            }

            if ((shoulderIsNull && shoulderVisibility != 0.0) ||
                (elbowIsNull && elbowVisibility != 0.0) ||
                (wristIsNull && wristVisibility != 0.0))
            {
                error = "A missing joint must have zero visibility.";
                return false;
            }

            int missingCount =
                (shoulderIsNull ? 1 : 0) +
                (elbowIsNull ? 1 : 0) +
                (wristIsNull ? 1 : 0);
            bool hasLowVisibility =
                shoulderVisibility < PartialVisibilityThreshold ||
                elbowVisibility < PartialVisibilityThreshold ||
                wristVisibility < PartialVisibilityThreshold;

            if (tracking == PoseTrackingState.Tracking && missingCount != 0)
            {
                error = "TRACKING requires all three joints.";
                return false;
            }

            if (tracking == PoseTrackingState.Partial &&
                missingCount == 0 &&
                !hasLowVisibility)
            {
                error = "PARTIAL requires a missing or low-visibility joint.";
                return false;
            }

            if (tracking == PoseTrackingState.Lost &&
                (missingCount != 3 ||
                 shoulderVisibility != 0.0 ||
                 elbowVisibility != 0.0 ||
                 wristVisibility != 0.0))
            {
                error = "LOST requires null joints and zero visibility.";
                return false;
            }

            if (pointing)
            {
                if (tracking != PoseTrackingState.Tracking || pointerIsNull)
                {
                    error = "pointing=true requires TRACKING and a pointer.";
                    return false;
                }
            }
            else if (!pointerIsNull)
            {
                error = "pointing=false requires pointer=null.";
                return false;
            }

            PosePointerMessageV2Dto dto;
            try
            {
                dto = root.ToObject<PosePointerMessageV2Dto>();
            }
            catch (JsonException exception)
            {
                error = $"Pose DTO conversion failed: {exception.Message}";
                return false;
            }

            if (dto == null || dto.Joints == null || dto.Visibility == null)
            {
                error = "Pose DTO conversion returned incomplete data.";
                return false;
            }

            state = new PosePointerState(
                timestamp,
                sequence,
                tracking,
                pointing,
                dto.Pointer,
                dto.Joints,
                dto.Visibility);
            return true;
        }

        private static bool ValidateHandGestureV1(
            JObject root,
            out string error)
        {
            error = string.Empty;
            if (!HasProperties(
                    root,
                    "timestamp",
                    "sequence",
                    "handDetected",
                    "handedness",
                    "gesture",
                    "confidence",
                    "fingerStates"))
            {
                error = "Gesture v1 message is missing a required field.";
                return false;
            }

            if (!TryNonNegativeLong(root["timestamp"], out _) ||
                !TryNonNegativeLong(root["sequence"], out _) ||
                root["handDetected"].Type != JTokenType.Boolean ||
                !TryString(root["handedness"], out _) ||
                !TryString(root["gesture"], out string gesture) ||
                !GestureValues.Contains(gesture) ||
                !TryFiniteNumber(root["confidence"], out double confidence) ||
                confidence < 0.0 ||
                confidence > 1.0)
            {
                error = "Gesture v1 field value is invalid.";
                return false;
            }

            bool handDetected = root["handDetected"].Value<bool>();
            if ((!handDetected && gesture != "NO_HAND") ||
                (handDetected && gesture == "NO_HAND"))
            {
                error = "Gesture v1 handDetected and gesture are inconsistent.";
                return false;
            }

            if (!(root["fingerStates"] is JObject fingers) ||
                !HasProperties(
                    fingers,
                    "thumb",
                    "index",
                    "middle",
                    "ring",
                    "pinky"))
            {
                error = "Gesture v1 fingerStates is incomplete.";
                return false;
            }

            foreach (string name in new[]
                     {
                         "thumb",
                         "index",
                         "middle",
                         "ring",
                         "pinky"
                     })
            {
                if (fingers[name].Type != JTokenType.Boolean)
                {
                    error = "Gesture v1 finger state must be boolean.";
                    return false;
                }
            }

            return true;
        }

        private static bool ValidatePointer(
            JToken token,
            out bool isNull,
            out string error)
        {
            isNull = token == null || token.Type == JTokenType.Null;
            error = string.Empty;
            if (isNull)
            {
                return true;
            }

            if (!(token is JObject pointer) ||
                !HasProperties(pointer, "x", "y") ||
                !TryFiniteNumber(pointer["x"], out double x) ||
                !TryFiniteNumber(pointer["y"], out double y) ||
                x < 0.0 ||
                x > 1.0 ||
                y < 0.0 ||
                y > 1.0)
            {
                error = "pointer must contain normalized finite x and y.";
                return false;
            }

            return true;
        }

        private static bool ValidateJoint(
            JToken token,
            out bool isNull,
            out string error)
        {
            isNull = token == null || token.Type == JTokenType.Null;
            error = string.Empty;
            if (isNull)
            {
                return true;
            }

            if (!(token is JObject joint) ||
                !HasProperties(joint, "x", "y", "z") ||
                !TryFiniteNumber(joint["x"], out _) ||
                !TryFiniteNumber(joint["y"], out _) ||
                !TryFiniteNumber(joint["z"], out _))
            {
                error = "joint must contain finite x, y and z.";
                return false;
            }

            return true;
        }

        private static bool TryTracking(
            string value,
            out PoseTrackingState tracking)
        {
            switch (value)
            {
                case "TRACKING":
                    tracking = PoseTrackingState.Tracking;
                    return true;
                case "PARTIAL":
                    tracking = PoseTrackingState.Partial;
                    return true;
                case "LOST":
                    tracking = PoseTrackingState.Lost;
                    return true;
                default:
                    tracking = default;
                    return false;
            }
        }

        private static bool HasProperties(JObject value, params string[] names)
        {
            foreach (string name in names)
            {
                if (value.Property(name) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryString(JToken token, out string value)
        {
            value = null;
            if (token == null || token.Type != JTokenType.String)
            {
                return false;
            }

            value = token.Value<string>();
            return value != null;
        }

        private static bool TryInteger(JToken token, out int value)
        {
            value = 0;
            if (token == null || token.Type != JTokenType.Integer)
            {
                return false;
            }

            try
            {
                value = token.Value<int>();
                return true;
            }
            catch (Exception exception) when (
                exception is OverflowException ||
                exception is FormatException)
            {
                return false;
            }
        }

        private static bool TryNonNegativeLong(JToken token, out long value)
        {
            value = 0;
            if (token == null || token.Type != JTokenType.Integer)
            {
                return false;
            }

            try
            {
                value = token.Value<long>();
                return value >= 0;
            }
            catch (Exception exception) when (
                exception is OverflowException ||
                exception is FormatException)
            {
                return false;
            }
        }

        private static bool TryVisibility(JToken token, out double value)
        {
            return TryFiniteNumber(token, out value) &&
                   value >= 0.0 &&
                   value <= 1.0;
        }

        private static bool TryFiniteNumber(JToken token, out double value)
        {
            value = 0.0;
            if (token == null ||
                (token.Type != JTokenType.Integer &&
                 token.Type != JTokenType.Float))
            {
                return false;
            }

            try
            {
                value = token.Value<double>();
                return !double.IsNaN(value) && !double.IsInfinity(value);
            }
            catch (Exception exception) when (
                exception is OverflowException ||
                exception is FormatException)
            {
                return false;
            }
        }
    }
}
