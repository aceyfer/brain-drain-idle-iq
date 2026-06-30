using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Subtle idle floating wobble for a 2D sprite character: a sine wave on Y (small amplitude,
    /// per-instance randomized frequency) plus a desynced sine wave on Z rotation (tiny max
    /// angle, phase-offset so its peaks don't land at the same time as the Y wobble's).
    /// Transform-only, no physics.
    ///
    /// Designed to sit on the same GameObject as PedestrianWalkController without fighting it:
    /// PedestrianWalkController only ever moves the X axis (transform.Translate(Vector3.left,
    /// ...)), so writing an absolute Y here every frame doesn't get clobbered by, or clobber,
    /// the walk -- the two scripts touch disjoint axes. The one real exception is
    /// PedestrianWalkController.Respawn(), which assigns a brand-new random Y directly; it
    /// calls ResetBaseY() right after doing so (if this component is present on the same
    /// GameObject) specifically so baseY tracks that new Y instead of overriding it back on the
    /// next LateUpdate.
    /// </summary>
    public sealed class PedestrianWobble : MonoBehaviour
    {
        [Header("Y Wobble")]
        [SerializeField] private float yAmplitude = 0.08f;
        [SerializeField] private float yFrequencyMin = 1.2f;
        [SerializeField] private float yFrequencyMax = 2.0f;

        [Header("Z Rotation Wobble")]
        [SerializeField] private float maxZRotationDegrees = 2f;

        private float baseY;
        private float yFrequency;
        private float rotationPhaseOffset;

        private void Start()
        {
            baseY = transform.position.y;
            yFrequency = Random.Range(yFrequencyMin, yFrequencyMax);
            rotationPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        /// <summary>Re-captures transform.position.y as the new baseY. Called by PedestrianWalkController.Respawn().</summary>
        public void ResetBaseY()
        {
            baseY = transform.position.y;
        }

        private void LateUpdate()
        {
            float t = Time.time * yFrequency;

            Vector3 position = transform.position;
            position.y = baseY + Mathf.Sin(t) * yAmplitude;
            transform.position = position;

            float zAngle = Mathf.Sin(t + rotationPhaseOffset) * maxZRotationDegrees;
            transform.localRotation = Quaternion.Euler(0f, 0f, zAngle);
        }
    }
}
