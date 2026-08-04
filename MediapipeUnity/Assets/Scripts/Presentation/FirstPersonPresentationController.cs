using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrace.Presentation
{
    /// <summary>
    /// Visual-only first-person arms presentation. This component reads the existing pose
    /// pointer direction but never writes to the pointer, raycast, or scenario interaction flow.
    /// </summary>
    public sealed class FirstPersonPresentationController : MonoBehaviour
    {
        [Serializable]
        public sealed class FingerPoseOffset
        {
            [SerializeField] private Transform bone;
            [SerializeField] private Vector3 pointingEulerOffset;

            public Transform Bone => bone;
            public Vector3 PointingEulerOffset => pointingEulerOffset;

            public FingerPoseOffset(Transform targetBone)
            {
                bone = targetBone;
            }
        }

        [Header("Installed presentation objects")]
        [SerializeField] private Camera handsCamera;
        [SerializeField] private Transform armsRig;
        [SerializeField] private Transform medicalBag;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightUpperArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform rightHand;

        [Header("Existing pose input (read only)")]
        [SerializeField] private PosePointerLineRenderer pointerLine;

        [Header("Visibility alignment")]
        [SerializeField]
        private Vector3 visualCenterCameraLocal =
            new Vector3(0.0f, -0.42f, 0.65f);

        [SerializeField]
        private bool autoAlignUsingRendererBounds = true;

        [Header("Medical bag placement")]
        [SerializeField]
        private bool autoNormalizeMedicalBagScale = true;

        [SerializeField, Min(0.001f)]
        private float medicalBagMaxDimension = 0.32f;

        [SerializeField]
        private Vector3 medicalBagLocalPosition = Vector3.zero;

        [SerializeField]
        private Vector3 medicalBagLocalRotation = Vector3.zero;

        [SerializeField]
        private Vector3 medicalBagLocalScaleMultiplier = Vector3.one;

        [Header("Right-arm visual response")]
        [SerializeField, Min(0.0f)] private float interpolationSpeed = 8.0f;
        [SerializeField, Range(0.0f, 90.0f)] private float maxYawDegrees = 28.0f;
        [SerializeField, Range(0.0f, 90.0f)] private float maxPitchDegrees = 22.0f;
        [SerializeField] private Vector3 upperArmYawAxis = Vector3.up;
        [SerializeField] private Vector3 upperArmPitchAxis = Vector3.right;
        [SerializeField] private Vector3 forearmYawAxis = Vector3.up;
        [SerializeField] private Vector3 forearmPitchAxis = Vector3.right;
        [SerializeField, Range(0.0f, 1.0f)] private float upperArmWeight = 0.45f;
        [SerializeField, Range(0.0f, 1.0f)] private float forearmWeight = 0.8f;

        [Header("Pointing finger offsets (asset-axis tuning required)")]
        [SerializeField] private List<FingerPoseOffset> pointingFingerOffsets =
            new List<FingerPoseOffset>();

        private Quaternion baseUpperArmRotation;
        private Quaternion baseForearmRotation;
        private Quaternion baseRightHandRotation;
        private readonly Dictionary<Transform, Quaternion> baseFingerRotations =
            new Dictionary<Transform, Quaternion>();
        private bool hasBasePose;

        public Camera HandsCamera => handsCamera;
        public Transform ArmsRig => armsRig;
        public Transform MedicalBag => medicalBag;

        public void ConfigureHandsCamera(Camera installedHandsCamera)
        {
            handsCamera = installedHandsCamera;
        }

        public void Configure(
            Camera installedHandsCamera,
            Transform installedArmsRig,
            Transform installedMedicalBag,
            Transform installedLeftHand,
            Transform installedRightUpperArm,
            Transform installedRightForearm,
            Transform installedRightHand,
            PosePointerLineRenderer installedPointerLine,
            IEnumerable<FingerPoseOffset> fingerOffsets)
        {
            handsCamera = installedHandsCamera;
            armsRig = installedArmsRig;
            medicalBag = installedMedicalBag;
            leftHand = installedLeftHand;
            rightUpperArm = installedRightUpperArm;
            rightForearm = installedRightForearm;
            rightHand = installedRightHand;
            pointerLine = installedPointerLine;
            pointingFingerOffsets = fingerOffsets == null
                ? new List<FingerPoseOffset>()
                : new List<FingerPoseOffset>(fingerOffsets);
            CaptureBasePose();
        }

        private void Awake()
        {
            CaptureBasePose();
        }

        private IEnumerator Start()
        {
            // SkinnedMeshRenderer bounds become reliable after the first rendered-frame update.
            yield return null;
            ApplyMedicalBagPresentation();
            if (autoAlignUsingRendererBounds)
            {
                AlignArmsRigToRendererBounds();
            }

            yield return new WaitForEndOfFrame();
            DiagnoseMedicalBagVisibility();
        }

        private void OnEnable()
        {
            CaptureBasePose();
        }

        private void OnValidate()
        {
            interpolationSpeed = Mathf.Max(0.0f, interpolationSpeed);
            maxYawDegrees = Mathf.Clamp(maxYawDegrees, 0.0f, 90.0f);
            maxPitchDegrees = Mathf.Clamp(maxPitchDegrees, 0.0f, 90.0f);
            medicalBagMaxDimension = Mathf.Max(0.001f, medicalBagMaxDimension);
        }

        private void LateUpdate()
        {
            if (!hasBasePose || armsRig == null)
            {
                CaptureBasePose();
            }

            if (!hasBasePose)
            {
                return;
            }

            bool isPointing = pointerLine != null && pointerLine.IsVisible;
            float yaw = 0.0f;
            float pitch = 0.0f;
            if (isPointing)
            {
                GetClampedLocalAim(pointerLine.CurrentDirection, out yaw, out pitch);
            }

            float blend = interpolationSpeed <= 0.0f
                ? 1.0f
                : 1.0f - Mathf.Exp(-interpolationSpeed * Time.deltaTime);
            ApplyArmPose(yaw, pitch, blend);
            ApplyFingerPose(isPointing, blend);
        }

        private void CaptureBasePose()
        {
            hasBasePose = rightUpperArm != null && rightForearm != null;
            if (!hasBasePose)
            {
                return;
            }

            baseUpperArmRotation = rightUpperArm.localRotation;
            baseForearmRotation = rightForearm.localRotation;
            baseRightHandRotation = rightHand == null
                ? Quaternion.identity
                : rightHand.localRotation;
            baseFingerRotations.Clear();
            foreach (FingerPoseOffset offset in pointingFingerOffsets)
            {
                if (offset != null && offset.Bone != null)
                {
                    baseFingerRotations[offset.Bone] = offset.Bone.localRotation;
                }
            }
        }

        private void GetClampedLocalAim(
            Vector3 worldDirection,
            out float yaw,
            out float pitch)
        {
            Vector3 localDirection = armsRig.InverseTransformDirection(worldDirection);
            if (!IsFinite(localDirection) || localDirection.sqrMagnitude <= 0.0001f)
            {
                yaw = 0.0f;
                pitch = 0.0f;
                return;
            }

            localDirection.Normalize();
            yaw = Mathf.Clamp(
                Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg,
                -maxYawDegrees,
                maxYawDegrees);
            float horizontalLength = new Vector2(
                localDirection.x,
                localDirection.z).magnitude;
            pitch = Mathf.Clamp(
                -Mathf.Atan2(localDirection.y, horizontalLength) * Mathf.Rad2Deg,
                -maxPitchDegrees,
                maxPitchDegrees);
        }

        private void ApplyArmPose(float yaw, float pitch, float blend)
        {
            rightUpperArm.localRotation = Quaternion.Slerp(
                rightUpperArm.localRotation,
                OffsetRotation(
                    baseUpperArmRotation,
                    upperArmYawAxis,
                    upperArmPitchAxis,
                    yaw * upperArmWeight,
                    pitch * upperArmWeight),
                blend);
            rightForearm.localRotation = Quaternion.Slerp(
                rightForearm.localRotation,
                OffsetRotation(
                    baseForearmRotation,
                    forearmYawAxis,
                    forearmPitchAxis,
                    yaw * forearmWeight,
                    pitch * forearmWeight),
                blend);
            if (rightHand != null)
            {
                rightHand.localRotation = Quaternion.Slerp(
                    rightHand.localRotation,
                    baseRightHandRotation,
                    blend);
            }
        }

        private void ApplyFingerPose(bool isPointing, float blend)
        {
            foreach (FingerPoseOffset offset in pointingFingerOffsets)
            {
                if (offset == null || offset.Bone == null ||
                    !baseFingerRotations.TryGetValue(offset.Bone, out Quaternion baseRotation))
                {
                    continue;
                }

                Quaternion target = isPointing
                    ? baseRotation * Quaternion.Euler(offset.PointingEulerOffset)
                    : baseRotation;
                offset.Bone.localRotation = Quaternion.Slerp(
                    offset.Bone.localRotation,
                    target,
                    blend);
            }
        }

        /// <summary>
        /// Moves only the visual rig so the combined skinned-renderer bounds center reaches the
        /// configured point in the hands camera view. It intentionally does not alter any pointer
        /// origin, raycast direction, or scenario object.
        /// </summary>
        public bool AlignArmsRigToRendererBounds()
        {
            if (handsCamera == null || armsRig == null)
            {
                return false;
            }

            SkinnedMeshRenderer[] renderers =
                armsRig.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            bool hasBounds = false;
            Bounds combinedBounds = default;
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                Debug.LogWarning(
                    "FirstPersonPresentationController could not find an enabled " +
                    "SkinnedMeshRenderer to align.",
                    this);
                return false;
            }

            Vector3 targetWorldCenter = handsCamera.transform.TransformPoint(
                visualCenterCameraLocal);
            armsRig.position += targetWorldCenter - combinedBounds.center;
            return true;
        }

        /// <summary>
        /// Applies the configurable hand-relative placement and normalizes the bag using all of
        /// its renderer bounds. This is visual-only and does not affect interaction colliders.
        /// </summary>
        public bool ApplyMedicalBagPresentation()
        {
            if (medicalBag == null)
            {
                Debug.LogWarning(
                    "FirstPersonPresentationController has no MedicalBag transform to place.",
                    this);
                return false;
            }

            medicalBag.localPosition = medicalBagLocalPosition;
            medicalBag.localRotation = Quaternion.Euler(medicalBagLocalRotation);
            if (!autoNormalizeMedicalBagScale)
            {
                return true;
            }

            if (!TryGetCombinedRendererBounds(medicalBag, out Bounds combinedBounds))
            {
                Debug.LogWarning(
                    "FirstPersonPresentationController could not find an enabled Renderer " +
                    "for MedicalBag. Check the model renderer and FirstPersonHands layer.",
                    this);
                return false;
            }

            float currentMaxDimension = Mathf.Max(
                combinedBounds.size.x,
                combinedBounds.size.y,
                combinedBounds.size.z);
            if (!IsFinite(currentMaxDimension) || currentMaxDimension <= 0.0001f)
            {
                Debug.LogWarning(
                    "FirstPersonPresentationController received invalid MedicalBag bounds. " +
                    "Check renderer scale and imported model units.",
                    this);
                return false;
            }

            float normalization = medicalBagMaxDimension / currentMaxDimension;
            medicalBag.localScale = Vector3.Scale(
                medicalBag.localScale * normalization,
                medicalBagLocalScaleMultiplier);
            return true;
        }

        private void DiagnoseMedicalBagVisibility()
        {
            if (medicalBag == null || !autoNormalizeMedicalBagScale)
            {
                return;
            }

            Renderer[] renderers = medicalBag.GetComponentsInChildren<Renderer>(true);
            bool hasEnabledRenderer = false;
            bool visibleToAnyCamera = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                hasEnabledRenderer = true;
                visibleToAnyCamera |= renderer.isVisible;
            }

            if (!hasEnabledRenderer)
            {
                Debug.LogWarning(
                    "MedicalBag has no enabled Renderer after scale normalization.",
                    this);
            }
            else if (!visibleToAnyCamera)
            {
                Debug.LogWarning(
                    "MedicalBag is still not visible after scale normalization. " +
                    "Check FirstPersonHandsCamera culling mask, bag local placement, and " +
                    "the model renderer bounds.",
                    this);
            }
        }

        private static bool TryGetCombinedRendererBounds(
            Transform root,
            out Bounds combinedBounds)
        {
            bool hasBounds = false;
            combinedBounds = default;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }
            return hasBounds;
        }

        private static Quaternion OffsetRotation(
            Quaternion baseRotation,
            Vector3 yawAxis,
            Vector3 pitchAxis,
            float yaw,
            float pitch)
        {
            Quaternion yawRotation = yawAxis.sqrMagnitude <= 0.0001f
                ? Quaternion.identity
                : Quaternion.AngleAxis(yaw, yawAxis.normalized);
            Quaternion pitchRotation = pitchAxis.sqrMagnitude <= 0.0001f
                ? Quaternion.identity
                : Quaternion.AngleAxis(pitch, pitchAxis.normalized);
            return baseRotation * yawRotation * pitchRotation;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
