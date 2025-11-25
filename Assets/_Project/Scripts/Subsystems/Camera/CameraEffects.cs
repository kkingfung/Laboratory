using UnityEngine;
using System.Collections;

namespace Laboratory.Subsystems.Camera
{
    /// <summary>
    /// Camera effects system for shake, zoom, and other visual effects
    /// Performance-optimized with coroutine management
    /// </summary>
    public class CameraEffects : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CameraConfig config;

        // Shake state
        private Coroutine _shakeCoroutine;
        private Vector3 _shakeOffset = Vector3.zero;
        private float _currentShakeIntensity = 0f;

        // Zoom state
        private Coroutine _zoomCoroutine;
        private float _targetFOV;
        private float _defaultFOV = 60f;

        // Tilt state
        private Coroutine _tiltCoroutine;
        private float _currentTilt = 0f;

        // References
        private UnityEngine.Camera _camera;
        private Transform _cameraTransform;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _cameraTransform = transform;
            _defaultFOV = _camera.fieldOfView;
            _targetFOV = _defaultFOV;
        }

        /// <summary>
        /// Apply camera shake effect
        /// </summary>
        public void Shake(float intensity, float duration, float frequency = 25f)
        {
            if (config != null && !config.EnableCameraEffects) return;

            // Stop existing shake
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }

            float finalIntensity = intensity;
            float finalDuration = duration;

            if (config != null)
            {
                finalIntensity *= config.ShakeIntensityMultiplier;
                finalDuration *= config.ShakeDurationMultiplier;
            }

            _shakeCoroutine = StartCoroutine(ShakeCoroutine(finalIntensity, finalDuration, frequency));
        }

        /// <summary>
        /// Shake coroutine implementation
        /// </summary>
        private IEnumerator ShakeCoroutine(float intensity, float duration, float frequency)
        {
            float elapsed = 0f;
            Vector3 originalPosition = _cameraTransform.localPosition - _shakeOffset;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = 1f - (elapsed / duration);

                _currentShakeIntensity = intensity * progress;

                // Perlin noise for smooth shake
                float x = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;
                float z = (Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) - 0.5f) * 2f;

                _shakeOffset = new Vector3(x, y, z) * _currentShakeIntensity;
                _cameraTransform.localPosition = originalPosition + _shakeOffset;

                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _cameraTransform.localPosition = originalPosition;
            _currentShakeIntensity = 0f;
        }

        /// <summary>
        /// Smoothly zoom camera to target FOV
        /// </summary>
        public void ZoomTo(float targetFOV, float duration = 0.5f)
        {
            if (_zoomCoroutine != null)
            {
                StopCoroutine(_zoomCoroutine);
            }

            _targetFOV = targetFOV;
            _zoomCoroutine = StartCoroutine(ZoomCoroutine(targetFOV, duration));
        }

        /// <summary>
        /// Zoom back to default FOV
        /// </summary>
        public void ZoomToDefault(float duration = 0.5f)
        {
            ZoomTo(_defaultFOV, duration);
        }

        /// <summary>
        /// Zoom coroutine implementation
        /// </summary>
        private IEnumerator ZoomCoroutine(float targetFOV, float duration)
        {
            float startFOV = _camera.fieldOfView;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _camera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
                yield return null;
            }

            _camera.fieldOfView = targetFOV;
        }

        /// <summary>
        /// Apply camera tilt (roll rotation)
        /// </summary>
        public void Tilt(float angle, float duration = 0.3f)
        {
            if (_tiltCoroutine != null)
            {
                StopCoroutine(_tiltCoroutine);
            }

            _tiltCoroutine = StartCoroutine(TiltCoroutine(angle, duration));
        }

        /// <summary>
        /// Reset camera tilt to zero
        /// </summary>
        public void ResetTilt(float duration = 0.3f)
        {
            Tilt(0f, duration);
        }

        /// <summary>
        /// Tilt coroutine implementation
        /// </summary>
        private IEnumerator TiltCoroutine(float targetAngle, float duration)
        {
            float startTilt = _currentTilt;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _currentTilt = Mathf.Lerp(startTilt, targetAngle, t);

                Vector3 currentRotation = _cameraTransform.localEulerAngles;
                currentRotation.z = _currentTilt;
                _cameraTransform.localEulerAngles = currentRotation;

                yield return null;
            }

            _currentTilt = targetAngle;
            Vector3 finalRotation = _cameraTransform.localEulerAngles;
            finalRotation.z = _currentTilt;
            _cameraTransform.localEulerAngles = finalRotation;
        }

        /// <summary>
        /// Impulse shake (quick hit feedback)
        /// </summary>
        public void ImpulseShake(float intensity = 0.3f)
        {
            Shake(intensity, 0.2f, 30f);
        }

        /// <summary>
        /// Explosion shake (longer, more intense)
        /// </summary>
        public void ExplosionShake(float intensity = 0.5f)
        {
            Shake(intensity, 0.5f, 20f);
        }

        /// <summary>
        /// Rumble shake (continuous low intensity)
        /// </summary>
        public void RumbleShake(float intensity = 0.1f, float duration = 1f)
        {
            Shake(intensity, duration, 15f);
        }

        /// <summary>
        /// Get current shake offset for external use
        /// </summary>
        public Vector3 GetShakeOffset()
        {
            return _shakeOffset;
        }

        /// <summary>
        /// Check if camera is currently shaking
        /// </summary>
        public bool IsShaking()
        {
            return _currentShakeIntensity > 0.01f;
        }

        /// <summary>
        /// Stop all effects immediately
        /// </summary>
        public void StopAllEffects()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeOffset = Vector3.zero;
                _currentShakeIntensity = 0f;
            }

            if (_zoomCoroutine != null)
            {
                StopCoroutine(_zoomCoroutine);
            }

            if (_tiltCoroutine != null)
            {
                StopCoroutine(_tiltCoroutine);
            }
        }

        private void OnDestroy()
        {
            StopAllEffects();
        }
    }
}
