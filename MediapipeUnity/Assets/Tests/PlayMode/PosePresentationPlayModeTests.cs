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

        [UnityTest]
        public IEnumerator LineRendererShowsTrackingPointerAndTimesOut()
        {
            var go = new GameObject("pose pointer test");
            var line = go.AddComponent<LineRenderer>();
            var visualizer = go.AddComponent<PosePointerLineRenderer>();
            visualizer.ConfigureForTests(
                go.transform,
                line,
                length: 2.0f,
                thickness: 0.02f,
                color: Color.green,
                smoothing: 0.0f,
                timeout: 0.1f,
                invertX: false,
                invertY: false);
            visualizer.SetConnected(true);
            visualizer.Apply(
                CreateTrackingState(),
                Time.realtimeSinceStartup);

            yield return null;

            Assert.That(visualizer.IsVisible, Is.True);
            Assert.That(line.positionCount, Is.EqualTo(2));
            Assert.That(
                Vector3.Distance(line.GetPosition(0), go.transform.position),
                Is.LessThan(0.001f));
            Assert.That(
                Vector3.Distance(line.GetPosition(0), line.GetPosition(1)),
                Is.EqualTo(2.0f).Within(0.01f));

            yield return new WaitForSeconds(0.15f);

            Assert.That(visualizer.IsVisible, Is.False);
            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator LineRendererHidesWhenPoseStopsPointing()
        {
            var go = new GameObject("pose pointer invalid test");
            var line = go.AddComponent<LineRenderer>();
            var visualizer = go.AddComponent<PosePointerLineRenderer>();
            visualizer.ConfigureForTests(
                go.transform,
                line,
                length: 1.0f,
                thickness: 0.02f,
                color: Color.cyan,
                smoothing: 0.0f,
                timeout: 0.5f,
                invertX: false,
                invertY: false);
            visualizer.SetConnected(true);
            visualizer.Apply(
                new PosePointerState(
                    1,
                    2,
                    PoseTrackingState.Partial,
                    false,
                    null,
                    new RightArmJointsDto(),
                    new RightArmVisibilityDto()),
                Time.realtimeSinceStartup);

            yield return null;

            Assert.That(visualizer.IsVisible, Is.False);
            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator LineRendererCanInvertHorizontalAndVerticalAxes()
        {
            var normalObject = new GameObject("pose pointer normal");
            var invertedObject = new GameObject("pose pointer inverted");
            var normal = normalObject.AddComponent<PosePointerLineRenderer>();
            var inverted = invertedObject.AddComponent<PosePointerLineRenderer>();
            normal.ConfigureForTests(
                normalObject.transform,
                normalObject.AddComponent<LineRenderer>(),
                length: 1.0f,
                thickness: 0.02f,
                color: Color.white,
                smoothing: 0.0f,
                timeout: 0.5f,
                invertX: false,
                invertY: false);
            inverted.ConfigureForTests(
                invertedObject.transform,
                invertedObject.AddComponent<LineRenderer>(),
                length: 1.0f,
                thickness: 0.02f,
                color: Color.white,
                smoothing: 0.0f,
                timeout: 0.5f,
                invertX: true,
                invertY: true);

            PosePointerState state = CreateTrackingState();
            normal.SetConnected(true);
            inverted.SetConnected(true);
            normal.Apply(state, Time.realtimeSinceStartup);
            inverted.Apply(state, Time.realtimeSinceStartup);

            yield return null;

            Assert.That(normal.CurrentDirection.x, Is.GreaterThan(0.0f));
            Assert.That(normal.CurrentDirection.y, Is.GreaterThan(0.0f));
            Assert.That(inverted.CurrentDirection.x, Is.LessThan(0.0f));
            Assert.That(inverted.CurrentDirection.y, Is.LessThan(0.0f));

            UnityEngine.Object.Destroy(normalObject);
            UnityEngine.Object.Destroy(invertedObject);
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
