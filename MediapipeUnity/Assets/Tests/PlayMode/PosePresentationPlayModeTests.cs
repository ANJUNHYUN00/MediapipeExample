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

        [UnityTest]
        public IEnumerator PointerRaycasterHighlightsOnlyCurrentPatient()
        {
            int patientLayer = 3;
            var pointerObject = new GameObject("pose pointer raycaster");
            var visualizer = pointerObject.AddComponent<PosePointerLineRenderer>();
            visualizer.ConfigureForTests(
                pointerObject.transform,
                pointerObject.AddComponent<LineRenderer>(),
                length: 4.0f,
                thickness: 0.02f,
                color: Color.white,
                smoothing: 0.0f,
                timeout: 0.5f,
                invertX: false,
                invertY: false);
            visualizer.SetConnected(true);

            var raycaster = pointerObject.AddComponent<PointerRaycaster>();
            raycaster.ConfigureForTests(
                visualizer,
                pointerObject.transform,
                1 << patientLayer,
                10.0f);

            GameObject first = CreatePatient(
                "first patient",
                new Vector3(0.0f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView firstPatient);
            GameObject second = CreatePatient(
                "second patient",
                new Vector3(3.6f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView secondPatient);

            visualizer.Apply(CreatePointerState(0.5, 0.5), Time.realtimeSinceStartup);
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(raycaster.CurrentPatient, Is.SameAs(firstPatient));
            Assert.That(firstPatient.IsHighlighted, Is.True);
            Assert.That(
                firstPatient.InteractionState,
                Is.EqualTo(PatientInteractionState.Highlighted));
            Assert.That(secondPatient.IsHighlighted, Is.False);

            visualizer.Apply(CreatePointerState(0.95, 0.5), Time.realtimeSinceStartup);
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(raycaster.CurrentPatient, Is.SameAs(secondPatient));
            Assert.That(firstPatient.IsHighlighted, Is.False);
            Assert.That(
                firstPatient.InteractionState,
                Is.EqualTo(PatientInteractionState.Unseen));
            Assert.That(secondPatient.IsHighlighted, Is.True);
            Assert.That(
                secondPatient.InteractionState,
                Is.EqualTo(PatientInteractionState.Highlighted));

            UnityEngine.Object.Destroy(pointerObject);
            UnityEngine.Object.Destroy(first);
            UnityEngine.Object.Destroy(second);
        }

        [UnityTest]
        public IEnumerator PointerRaycasterClearsHighlightWhenPointerHidden()
        {
            int patientLayer = 3;
            var pointerObject = new GameObject("pose pointer clear raycaster");
            var visualizer = pointerObject.AddComponent<PosePointerLineRenderer>();
            visualizer.ConfigureForTests(
                pointerObject.transform,
                pointerObject.AddComponent<LineRenderer>(),
                length: 4.0f,
                thickness: 0.02f,
                color: Color.white,
                smoothing: 0.0f,
                timeout: 0.5f,
                invertX: false,
                invertY: false);
            visualizer.SetConnected(true);

            var raycaster = pointerObject.AddComponent<PointerRaycaster>();
            raycaster.ConfigureForTests(
                visualizer,
                pointerObject.transform,
                1 << patientLayer,
                10.0f);

            GameObject patient = CreatePatient(
                "clear patient",
                new Vector3(0.0f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView patientView);

            visualizer.Apply(CreatePointerState(0.5, 0.5), Time.realtimeSinceStartup);
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(patientView.IsHighlighted, Is.True);

            visualizer.SetConnected(false);
            yield return null;

            Assert.That(raycaster.CurrentPatient, Is.Null);
            Assert.That(patientView.IsHighlighted, Is.False);
            Assert.That(
                patientView.InteractionState,
                Is.EqualTo(PatientInteractionState.Unseen));

            UnityEngine.Object.Destroy(pointerObject);
            UnityEngine.Object.Destroy(patient);
        }

        [UnityTest]
        public IEnumerator DwellSelectorSelectsCurrentPatientAfterThreshold()
        {
            int patientLayer = 3;
            GameObject pointerObject = CreatePointerRig(
                "dwell selector",
                patientLayer,
                out PosePointerLineRenderer visualizer,
                out PointerRaycaster raycaster);
            var selector = pointerObject.AddComponent<PatientDwellSelector>();
            selector.ConfigureForTests(raycaster, 0.1f);

            GameObject patient = CreatePatient(
                "dwell patient",
                new Vector3(0.0f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView patientView);

            visualizer.Apply(CreatePointerState(0.5, 0.5), Time.realtimeSinceStartup);
            yield return new WaitForSeconds(0.15f);
            yield return null;

            Assert.That(selector.SelectedPatient, Is.SameAs(patientView));
            Assert.That(patientView.IsSelected, Is.True);
            Assert.That(
                patientView.InteractionState,
                Is.EqualTo(PatientInteractionState.InProgress));

            UnityEngine.Object.Destroy(pointerObject);
            UnityEngine.Object.Destroy(patient);
        }

        [UnityTest]
        public IEnumerator DwellSelectorResetsTimerWhenPatientChanges()
        {
            int patientLayer = 3;
            GameObject pointerObject = CreatePointerRig(
                "dwell switch selector",
                patientLayer,
                out PosePointerLineRenderer visualizer,
                out PointerRaycaster raycaster);
            var selector = pointerObject.AddComponent<PatientDwellSelector>();
            selector.ConfigureForTests(raycaster, 0.2f);

            GameObject first = CreatePatient(
                "first dwell patient",
                new Vector3(0.0f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView firstPatient);
            GameObject second = CreatePatient(
                "second dwell patient",
                new Vector3(3.6f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView secondPatient);

            visualizer.Apply(CreatePointerState(0.5, 0.5), Time.realtimeSinceStartup);
            yield return new WaitForSeconds(0.12f);
            yield return null;

            visualizer.Apply(CreatePointerState(0.95, 0.5), Time.realtimeSinceStartup);
            yield return new WaitForSeconds(0.12f);
            yield return null;

            Assert.That(firstPatient.IsSelected, Is.False);
            Assert.That(secondPatient.IsSelected, Is.False);

            yield return new WaitForSeconds(0.12f);
            yield return null;

            Assert.That(selector.SelectedPatient, Is.SameAs(secondPatient));
            Assert.That(firstPatient.IsSelected, Is.False);
            Assert.That(
                firstPatient.InteractionState,
                Is.EqualTo(PatientInteractionState.Unseen));
            Assert.That(secondPatient.IsSelected, Is.True);
            Assert.That(
                secondPatient.InteractionState,
                Is.EqualTo(PatientInteractionState.InProgress));

            UnityEngine.Object.Destroy(pointerObject);
            UnityEngine.Object.Destroy(first);
            UnityEngine.Object.Destroy(second);
        }

        [UnityTest]
        public IEnumerator DwellSelectorResetsTimerWhenPointerHidden()
        {
            int patientLayer = 3;
            GameObject pointerObject = CreatePointerRig(
                "dwell hidden selector",
                patientLayer,
                out PosePointerLineRenderer visualizer,
                out PointerRaycaster raycaster);
            var selector = pointerObject.AddComponent<PatientDwellSelector>();
            selector.ConfigureForTests(raycaster, 0.2f);

            GameObject patient = CreatePatient(
                "hidden dwell patient",
                new Vector3(0.0f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView patientView);

            visualizer.Apply(CreatePointerState(0.5, 0.5), Time.realtimeSinceStartup);
            yield return new WaitForSeconds(0.12f);
            yield return null;

            visualizer.SetConnected(false);
            yield return new WaitForSeconds(0.15f);
            yield return null;

            Assert.That(selector.CurrentDwellPatient, Is.Null);
            Assert.That(selector.DwellTimer, Is.EqualTo(0.0f).Within(0.001f));
            Assert.That(selector.SelectedPatient, Is.Null);
            Assert.That(patientView.IsSelected, Is.False);

            UnityEngine.Object.Destroy(pointerObject);
            UnityEngine.Object.Destroy(patient);
        }

        [UnityTest]
        public IEnumerator DwellSelectorKeepsOnlyOnePatientSelected()
        {
            int patientLayer = 3;
            GameObject pointerObject = CreatePointerRig(
                "single selected dwell selector",
                patientLayer,
                out PosePointerLineRenderer visualizer,
                out PointerRaycaster raycaster);
            var selector = pointerObject.AddComponent<PatientDwellSelector>();
            selector.ConfigureForTests(raycaster, 0.1f);

            GameObject first = CreatePatient(
                "first selected patient",
                new Vector3(0.0f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView firstPatient);
            GameObject second = CreatePatient(
                "second selected patient",
                new Vector3(3.6f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView secondPatient);

            visualizer.Apply(CreatePointerState(0.5, 0.5), Time.realtimeSinceStartup);
            yield return new WaitForSeconds(0.15f);
            yield return null;

            Assert.That(firstPatient.IsSelected, Is.True);

            visualizer.Apply(CreatePointerState(0.95, 0.5), Time.realtimeSinceStartup);
            yield return new WaitForSeconds(0.15f);
            yield return null;

            Assert.That(selector.SelectedPatient, Is.SameAs(secondPatient));
            Assert.That(firstPatient.IsSelected, Is.False);
            Assert.That(
                firstPatient.InteractionState,
                Is.EqualTo(PatientInteractionState.Unseen));
            Assert.That(secondPatient.IsSelected, Is.True);
            Assert.That(
                secondPatient.InteractionState,
                Is.EqualTo(PatientInteractionState.InProgress));

            UnityEngine.Object.Destroy(pointerObject);
            UnityEngine.Object.Destroy(first);
            UnityEngine.Object.Destroy(second);
        }

        [UnityTest]
        public IEnumerator PatientViewStartsUnseenAndCanBeMarkedChecked()
        {
            GameObject patient = CreatePatient(
                "state default patient",
                Vector3.forward,
                layer: 3,
                baseColor: Color.gray,
                out PatientView patientView);

            yield return null;

            Assert.That(
                patientView.InteractionState,
                Is.EqualTo(PatientInteractionState.Unseen));

            patientView.MarkChecked();

            Assert.That(
                patientView.InteractionState,
                Is.EqualTo(PatientInteractionState.Checked));
            Assert.That(patientView.IsChecked, Is.True);

            UnityEngine.Object.Destroy(patient);
        }

        [UnityTest]
        public IEnumerator CheckedPatientIgnoresHoverAndDwellStateChanges()
        {
            int patientLayer = 3;
            GameObject pointerObject = CreatePointerRig(
                "checked protected dwell selector",
                patientLayer,
                out PosePointerLineRenderer visualizer,
                out PointerRaycaster raycaster);
            var selector = pointerObject.AddComponent<PatientDwellSelector>();
            selector.ConfigureForTests(raycaster, 0.1f);

            GameObject patient = CreatePatient(
                "checked protected patient",
                new Vector3(0.0f, 0.0f, 4.0f),
                patientLayer,
                Color.gray,
                out PatientView patientView);
            patientView.MarkChecked();

            visualizer.Apply(CreatePointerState(0.5, 0.5), Time.realtimeSinceStartup);
            yield return new WaitForSeconds(0.15f);
            yield return null;

            Assert.That(raycaster.CurrentPatient, Is.SameAs(patientView));
            Assert.That(
                patientView.InteractionState,
                Is.EqualTo(PatientInteractionState.Checked));
            Assert.That(selector.SelectedPatient, Is.Null);

            visualizer.SetConnected(false);
            yield return null;

            Assert.That(
                patientView.InteractionState,
                Is.EqualTo(PatientInteractionState.Checked));

            UnityEngine.Object.Destroy(pointerObject);
            UnityEngine.Object.Destroy(patient);
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
            return CreatePointerState(0.7, 0.3);
        }

        private static PosePointerState CreatePointerState(double x, double y)
        {
            return new PosePointerState(
                1,
                1,
                PoseTrackingState.Tracking,
                true,
                new PointerDto { X = x, Y = y },
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

        private static GameObject CreatePatient(
            string name,
            Vector3 position,
            int layer,
            Color baseColor,
            out PatientView patientView)
        {
            GameObject patient = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patient.name = name;
            patient.layer = layer;
            patient.transform.position = position;
            patient.transform.localScale = Vector3.one;

            var renderer = patient.GetComponent<Renderer>();
            renderer.material.color = baseColor;

            patientView = patient.AddComponent<PatientView>();
            patientView.ConfigureForTests(
                new[] { renderer },
                Color.cyan,
                "_Color",
                selectionColor: Color.blue,
                baseStateColor: baseColor,
                checkedStateColor: Color.white);
            return patient;
        }

        private static GameObject CreatePointerRig(
            string name,
            int patientLayer,
            out PosePointerLineRenderer visualizer,
            out PointerRaycaster raycaster)
        {
            var pointerObject = new GameObject(name);
            visualizer = pointerObject.AddComponent<PosePointerLineRenderer>();
            visualizer.ConfigureForTests(
                pointerObject.transform,
                pointerObject.AddComponent<LineRenderer>(),
                length: 4.0f,
                thickness: 0.02f,
                color: Color.white,
                smoothing: 0.0f,
                timeout: 0.5f,
                invertX: false,
                invertY: false);
            visualizer.SetConnected(true);

            raycaster = pointerObject.AddComponent<PointerRaycaster>();
            raycaster.ConfigureForTests(
                visualizer,
                pointerObject.transform,
                1 << patientLayer,
                10.0f);
            return pointerObject;
        }
    }
}
