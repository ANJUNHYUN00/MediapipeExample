using UnityEngine;

namespace TriageTrace.Presentation
{
    public sealed class PatientDwellSelector : MonoBehaviour
    {
        [SerializeField]
        private PointerRaycaster pointerRaycaster;

        [SerializeField]
        [Min(0.05f)]
        private float dwellSeconds = 0.7f;

        private PatientView _dwellPatient;
        private PatientView _selectedPatient;
        private float _dwellTimer;
        private bool _selectedCurrentDwellPatient;

        public PatientView CurrentDwellPatient => _dwellPatient;
        public PatientView SelectedPatient => _selectedPatient;
        public float DwellTimer => _dwellTimer;
        public float DwellSeconds => dwellSeconds;

        public void ConfigureForTests(
            PointerRaycaster raycaster,
            float seconds)
        {
            pointerRaycaster = raycaster;
            dwellSeconds = Mathf.Max(0.05f, seconds);
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDisable()
        {
            ResetDwell();
        }

        private void OnValidate()
        {
            dwellSeconds = Mathf.Max(0.05f, dwellSeconds);
        }

        private void Update()
        {
            EnsureReferences();

            PatientView currentPatient = pointerRaycaster == null
                ? null
                : pointerRaycaster.CurrentPatient;
            if (currentPatient == null)
            {
                ResetDwell();
                return;
            }

            if (_dwellPatient != currentPatient)
            {
                _dwellPatient = currentPatient;
                _dwellTimer = 0.0f;
                _selectedCurrentDwellPatient = false;
            }

            _dwellTimer += Time.deltaTime;
            if (_dwellTimer >= dwellSeconds &&
                !_selectedCurrentDwellPatient)
            {
                Select(currentPatient);
                _selectedCurrentDwellPatient = true;
            }
        }

        private void EnsureReferences()
        {
            if (pointerRaycaster == null)
            {
                pointerRaycaster = GetComponent<PointerRaycaster>();
            }
        }

        private void ResetDwell()
        {
            _dwellPatient = null;
            _dwellTimer = 0.0f;
            _selectedCurrentDwellPatient = false;
        }

        private void Select(PatientView patient)
        {
            if (patient == null || patient.IsChecked)
            {
                return;
            }

            if (_selectedPatient == patient)
            {
                return;
            }

            if (_selectedPatient != null && !_selectedPatient.IsChecked)
            {
                _selectedPatient.SelectOff();
            }

            _selectedPatient = patient;
            _selectedPatient.SelectOn();
        }
    }
}
