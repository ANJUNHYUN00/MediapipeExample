using System.IO;
using NUnit.Framework;
using TriageTrace.Models;
using TriageTrace.Networking;
using UnityEngine;

namespace TriageTrace.Tests.EditMode
{
    public sealed class PoseProtocolEditModeTests
    {
        [TestCase(
            "pose_pointer_v2_tracking.json",
            PoseTrackingState.Tracking,
            true)]
        [TestCase(
            "pose_pointer_v2_partial.json",
            PoseTrackingState.Partial,
            false)]
        [TestCase(
            "pose_pointer_v2_lost.json",
            PoseTrackingState.Lost,
            false)]
        public void PoseFixturesParseToValidatedState(
            string fixtureName,
            PoseTrackingState expectedTracking,
            bool expectedPointing)
        {
            bool accepted = PoseMessageParser.TryParse(
                ReadFixture(fixtureName),
                out MessageKind kind,
                out PosePointerState state,
                out string error);

            Assert.That(accepted, Is.True, error);
            Assert.That(kind, Is.EqualTo(MessageKind.PosePointerV2));
            Assert.That(state, Is.Not.Null);
            Assert.That(state.Tracking, Is.EqualTo(expectedTracking));
            Assert.That(state.Pointing, Is.EqualTo(expectedPointing));
            Assert.That(state.Pointer != null, Is.EqualTo(expectedPointing));
        }

        [TestCase("hand_gesture_v1_rock.json")]
        [TestCase("hand_gesture_v1_extra_field.json")]
        public void GestureV1FixturesRouteSeparately(string fixtureName)
        {
            bool accepted = PoseMessageParser.TryParse(
                ReadFixture(fixtureName),
                out MessageKind kind,
                out PosePointerState state,
                out string error);

            Assert.That(accepted, Is.True, error);
            Assert.That(kind, Is.EqualTo(MessageKind.HandGestureV1));
            Assert.That(state, Is.Null);
        }

        [TestCase("hand_gesture_invalid_gesture.json")]
        [TestCase("hand_gesture_invalid_version.json")]
        public void InvalidGestureFixturesAreRejected(string fixtureName)
        {
            bool accepted = PoseMessageParser.TryParse(
                ReadFixture(fixtureName),
                out MessageKind kind,
                out PosePointerState state,
                out string error);

            Assert.That(accepted, Is.False);
            Assert.That(kind, Is.EqualTo(MessageKind.Rejected));
            Assert.That(state, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void UnknownAdditionalPoseFieldIsAllowed()
        {
            string json = ReadFixture("pose_pointer_v2_tracking.json")
                .TrimEnd();
            json = json.Substring(0, json.Length - 1)
                + ",\"diagnosticOnly\":\"allowed\"}";

            bool accepted = PoseMessageParser.TryParse(
                json,
                out MessageKind kind,
                out _,
                out string error);

            Assert.That(accepted, Is.True, error);
            Assert.That(kind, Is.EqualTo(MessageKind.PosePointerV2));
        }

        [Test]
        public void PointingFalseWithPointerIsRejected()
        {
            string json = ReadFixture("pose_pointer_v2_tracking.json")
                .Replace("\"pointing\": true", "\"pointing\": false");

            bool accepted = PoseMessageParser.TryParse(
                json,
                out _,
                out _,
                out string error);

            Assert.That(accepted, Is.False);
            Assert.That(error, Does.Contain("pointer=null"));
        }

        [Test]
        public void LatestQueueRejectsDuplicateAndOlderSequences()
        {
            PosePointerState sequence100 = ParsePose(
                "pose_pointer_v2_tracking.json");
            PosePointerState sequence101 = ParsePose(
                "pose_pointer_v2_lost.json");
            var queue = new LatestPoseStateQueue();

            Assert.That(queue.TryEnqueue(sequence100), Is.True);
            Assert.That(queue.TryEnqueue(sequence100), Is.False);
            Assert.That(queue.TryEnqueue(sequence101), Is.True);
            Assert.That(queue.TryEnqueue(sequence100), Is.False);
            Assert.That(queue.TryDequeue(out PosePointerState latest), Is.True);
            Assert.That(latest.Sequence, Is.EqualTo(101));
            Assert.That(queue.TryDequeue(out _), Is.False);
        }

        private static PosePointerState ParsePose(string fixtureName)
        {
            bool accepted = PoseMessageParser.TryParse(
                ReadFixture(fixtureName),
                out MessageKind kind,
                out PosePointerState state,
                out string error);
            Assert.That(accepted, Is.True, error);
            Assert.That(kind, Is.EqualTo(MessageKind.PosePointerV2));
            return state;
        }

        private static string ReadFixture(string fixtureName)
        {
            string path = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    "..",
                    "Mediapipe",
                    "tests",
                    "fixtures",
                    "messages",
                    fixtureName));
            return File.ReadAllText(path);
        }
    }
}
