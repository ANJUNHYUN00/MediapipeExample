using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TriageTrace.Models;
using UnityEngine;

namespace TriageTrace.Presentation
{
    /// <summary>
    /// Read-only peripheral operations display for the non-medical simulation.
    /// It only observes existing pose and patient state; it never participates in input,
    /// raycasting, dwell selection, or the world-space patient card.
    /// </summary>
    public sealed class ARGuidanceHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PoseReceiverBehaviour poseReceiver;
        [SerializeField] private Camera targetCamera;

        [Header("Text")]
        [SerializeField] private TMP_Text zoneText;
        [SerializeField] private TMP_Text connectionText;
        [SerializeField] private TMP_Text poseText;
        [SerializeField] private TMP_Text leftGuidanceText;
        [SerializeField] private TMP_Text rightGuidanceText;
        [SerializeField] private TMP_Text patientStatusText;
        [SerializeField] private TMP_Text patientRowsText;
        [SerializeField] private TMP_Text teamSyncText;

        [Header("Display")]
        [SerializeField] private string zoneName = "PLATFORM";
        [SerializeField, Min(1)] private int maximumSyncEvents = 3;
        [SerializeField, Min(1)] private int maximumNearbyPatients = 4;

        private readonly List<string> _syncEvents = new List<string>();
        private readonly HashSet<PatientView> _observedPatients = new HashSet<PatientView>();

        public void Configure(
            PoseReceiverBehaviour receiver,
            Camera camera,
            TMP_Text zone,
            TMP_Text connection,
            TMP_Text pose,
            TMP_Text leftGuidance,
            TMP_Text rightGuidance,
            TMP_Text patientStatus,
            TMP_Text patientRows,
            TMP_Text teamSync)
        {
            poseReceiver = receiver;
            targetCamera = camera;
            zoneText = zone;
            connectionText = connection;
            poseText = pose;
            leftGuidanceText = leftGuidance;
            rightGuidanceText = rightGuidance;
            patientStatusText = patientStatus;
            patientRowsText = patientRows;
            teamSyncText = teamSync;
        }

        private void Awake()
        {
            ResolveReferences();
            ObservePatients();
            Refresh();
        }

        private void OnEnable()
        {
            ObservePatients();
        }

        private void OnDisable()
        {
            StopObservingPatients();
        }

        private void Update()
        {
            ResolveReferences();
            ObservePatients();
            Refresh();
        }

        private void ResolveReferences()
        {
            if (poseReceiver == null)
            {
                poseReceiver = FindFirstObjectByType<PoseReceiverBehaviour>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void ObservePatients()
        {
            foreach (PatientView patient in FindObjectsByType<PatientView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (patient != null && _observedPatients.Add(patient))
                {
                    patient.StateChanged += HandlePatientStateChanged;
                }
            }
        }

        private void StopObservingPatients()
        {
            foreach (PatientView patient in _observedPatients)
            {
                if (patient != null)
                {
                    patient.StateChanged -= HandlePatientStateChanged;
                }
            }
            _observedPatients.Clear();
        }

        private void HandlePatientStateChanged(PatientView patient)
        {
            if (patient == null || !patient.IsChecked)
            {
                return;
            }

            _syncEvents.Insert(0, $"> {patient.DisplayName.ToUpperInvariant()} CONFIRMATION RECORDED");
            while (_syncEvents.Count > maximumSyncEvents)
            {
                _syncEvents.RemoveAt(_syncEvents.Count - 1);
            }
        }

        private void Refresh()
        {
            SetText(zoneText, $"DIRECTION  {zoneName.ToUpperInvariant()}\nSIMULATION ONLY");
            UpdatePoseStatus();
            UpdatePatientStatus();
            SetText(teamSyncText, _syncEvents.Count == 0
                ? "LOCAL TEAM SYNC\nNo local confirmations recorded."
                : "LOCAL TEAM SYNC\n" + string.Join("\n", _syncEvents));
        }

        private void UpdatePoseStatus()
        {
            if (poseReceiver == null)
            {
                SetText(connectionText, "LINK WAITING");
                SetText(poseText, "POSE WAITING");
                return;
            }

            PoseDebugPresenterState presenter = poseReceiver.PresenterState;
            SetText(connectionText, presenter.Connected ? "LINK CONNECTED" : "LINK WAITING");
            PosePointerState latest = presenter.Latest;
            SetText(poseText, latest != null && latest.Tracking == PoseTrackingState.Tracking && latest.Pointing
                ? "POSE TRACKING"
                : "POSE WAITING");
        }

        private void UpdatePatientStatus()
        {
            PatientView[] patients = FindObjectsByType<PatientView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int uncheckedCount = 0;
            int checkedCount = 0;
            PatientView left = null;
            PatientView right = null;
            float leftDistance = float.PositiveInfinity;
            float rightDistance = float.PositiveInfinity;

            foreach (PatientView patient in patients)
            {
                if (patient == null) continue;
                if (patient.IsChecked) checkedCount++; else uncheckedCount++;

                if (targetCamera == null || patient.IsChecked) continue;
                Vector3 position = patient.StatusCardAnchor.position;
                float distance = Vector3.Distance(targetCamera.transform.position, position);
                Vector3 local = targetCamera.transform.InverseTransformPoint(position);
                if (local.x < 0.0f && distance < leftDistance) { left = patient; leftDistance = distance; }
                if (local.x >= 0.0f && distance < rightDistance) { right = patient; rightDistance = distance; }
            }

            SetText(patientStatusText, $"NEARBY PATIENTS\n<color=#FFAD33>UNCONFIRMED {uncheckedCount}</color> / <color=#66E6A3>CHECKED {checkedCount}</color>");
            SetText(patientRowsText, BuildNearbyPatientRows(patients));
            SetText(leftGuidanceText, BuildGuidance("<", left, leftDistance));
            SetText(rightGuidanceText, BuildGuidance(">", right, rightDistance));
        }

        private string BuildNearbyPatientRows(IEnumerable<PatientView> patients)
        {
            if (targetCamera == null)
            {
                return "Waiting for main camera.";
            }

            var nearbyPatients = patients
                .Where(patient => patient != null)
                .Select(patient => new NearbyPatient(patient, Vector3.Distance(targetCamera.transform.position, patient.StatusCardAnchor.position)))
                .OrderBy(entry => entry.Distance)
                .ThenBy(entry => entry.Patient.DisplayName, StringComparer.Ordinal)
                .Take(maximumNearbyPatients)
                .Select(entry => FormatNearbyPatientRow(entry.Patient, entry.Distance))
                .ToArray();

            return nearbyPatients.Length == 0 ? "No scenario targets." : string.Join("\n", nearbyPatients);
        }

        private static string FormatNearbyPatientRow(PatientView patient, float distance)
        {
            string status = patient.IsChecked
                ? "<color=#66E6A3>CHECKED</color>"
                : "<color=#FFAD33>UNCONFIRMED</color>";
            return $"{patient.DisplayName.ToUpperInvariant()}  {status}  {distance:0.0}m";
        }

        private static string BuildGuidance(string arrow, PatientView patient, float distance)
        {
            return patient == null
                ? string.Empty
                : $"{arrow}  {patient.DisplayName.ToUpperInvariant()}\n   {distance:0.0} m";
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }

        private readonly struct NearbyPatient
        {
            public NearbyPatient(PatientView patient, float distance)
            {
                Patient = patient;
                Distance = distance;
            }

            public PatientView Patient { get; }
            public float Distance { get; }
        }
    }
}
