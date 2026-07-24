using System;
using System.Collections;
using NUnit.Framework;
using TriageTrace.Models;
using TriageTrace.Networking;
using TriageTrace.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TriageTrace.Tests.PlayMode
{
    public sealed class PosePresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator PointerIsVisibleOnlyWhileConnectedAndFresh()
        {
            PosePointerState tracking = CreateTrackingState();
            var presenter = new PoseDebugPresenterState();
            presenter.SetConnected(true);
            presenter.Apply(tracking, 10.0f);

            Assert.That(presenter.IsPointerVisible(10.1f, 0.5f), Is.True);
            Assert.That(presenter.IsPointerVisible(10.6f, 0.5f), Is.False);

            presenter.SetConnected(false);
            Assert.That(presenter.IsPointerVisible(10.2f, 0.5f), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PartialStateNeverShowsPointer()
        {
            var presenter = new PoseDebugPresenterState();
            presenter.SetConnected(true);
            presenter.Apply(
                new PosePointerState(
                    1,
                    1,
                    PoseTrackingState.Partial,
                    false,
                    null,
                    new RightArmJointsDto(),
                    new RightArmVisibilityDto()),
                1.0f);

            Assert.That(presenter.IsPointerVisible(1.1f, 0.5f), Is.False);
            yield return null;
        }

        [Test]
        public void SafetyNoticeExplicitlyMarksSimulation()
        {
            Assert.That(
                PoseReceiverBehaviour.SafetyNotice,
                Does.Contain("Simulation Only"));
            Assert.That(
                PoseReceiverBehaviour.SafetyNotice,
                Does.Contain("의료 판단용이 아님"));
        }

        [UnityTest]
        public IEnumerator PythonPublisherFeedsUnityReceiverWhenEnabled()
        {
            string uri = Environment.GetEnvironmentVariable(
                "TRIAGE_TRACE_INTEGRATION_URI");
            if (string.IsNullOrWhiteSpace(uri))
            {
                Assert.Ignore(
                    "Set TRIAGE_TRACE_INTEGRATION_URI to run the live bridge test.");
            }

            var queue = new LatestPoseStateQueue();
            var client = new PoseWebSocketClient(
                uri,
                queue,
                reconnectDelaySeconds: 0.1);
            client.Start();

            float deadline = Time.realtimeSinceStartup + 8.0f;
            PosePointerState received = null;
            while (received == null && Time.realtimeSinceStartup < deadline)
            {
                queue.TryDequeue(out received);
                yield return null;
            }

            var stopTask = client.StopAsync();
            while (!stopTask.IsCompleted)
            {
                yield return null;
            }
            client.Dispose();

            Assert.That(received, Is.Not.Null);
            Assert.That(received.Sequence, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                received.Tracking,
                Is.EqualTo(PoseTrackingState.Tracking)
                    .Or.EqualTo(PoseTrackingState.Partial)
                    .Or.EqualTo(PoseTrackingState.Lost));
        }

        private static PosePointerState CreateTrackingState()
        {
            return new PosePointerState(
                1,
                1,
                PoseTrackingState.Tracking,
                true,
                new PointerDto { X = 0.7, Y = 0.3 },
                new RightArmJointsDto
                {
                    RightShoulder = new JointDto(),
                    RightElbow = new JointDto(),
                    RightWrist = new JointDto()
                },
                new RightArmVisibilityDto
                {
                    RightShoulder = 0.9,
                    RightElbow = 0.9,
                    RightWrist = 0.9
                });
        }
    }
}
