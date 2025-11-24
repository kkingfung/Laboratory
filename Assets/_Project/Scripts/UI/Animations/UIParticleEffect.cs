using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.UI.Animations
{
    /// <summary>
    /// Simple particle effect system for UI feedback (confetti, sparkles, stars, etc.)
    /// Lightweight and performant for UI celebrations and visual feedback
    /// </summary>
    public class UIParticleEffect : MonoBehaviour
    {
        [Header("Particle Settings")]
        [SerializeField] private ParticleEffectType effectType = ParticleEffectType.Confetti;
        [SerializeField] private int particleCount = 50;
        [SerializeField] private float duration = 2f;
        [SerializeField] private float spread = 200f;
        [SerializeField] private float speed = 500f;
        [SerializeField] private float gravity = 300f;

        [Header("Visuals")]
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private Gradient colorGradient;
        [SerializeField] private Vector2 particleSize = new Vector2(10f, 10f);
        [SerializeField] private bool fadeOut = true;

        [Header("Auto-Play")]
        [SerializeField] private bool playOnEnable = false;
        [SerializeField] private bool destroyOnComplete = false;

        // Particle data
        private List<ParticleData> _particles = new List<ParticleData>();
        private RectTransform _rectTransform;
        private Canvas _canvas;

        public enum ParticleEffectType
        {
            Confetti,
            Sparkle,
            Star,
            Heart,
            Custom
        }

        private class ParticleData
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public UnityEngine.UI.Image image;
            public Vector2 velocity;
            public float lifetime;
            public float maxLifetime;
            public float rotation;
            public float rotationSpeed;
            public Color startColor;
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            // Initialize color gradient if null
            if (colorGradient == null)
            {
                colorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(Color.yellow, 0f);
                colorKeys[1] = new GradientColorKey(Color.red, 1f);
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                colorGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void Update()
        {
            UpdateParticles();
        }

        #region Public Methods

        /// <summary>
        /// Plays the particle effect
        /// </summary>
        public void Play()
        {
            ClearParticles();
            CreateParticles();
        }

        /// <summary>
        /// Stops the particle effect
        /// </summary>
        public void Stop()
        {
            ClearParticles();
        }

        #endregion

        #region Private Methods

        private void CreateParticles()
        {
            for (int i = 0; i < particleCount; i++)
            {
                CreateParticle();
            }
        }

        private void CreateParticle()
        {
            // Create game object
            GameObject particleObj = new GameObject($"Particle_{_particles.Count}");
            particleObj.transform.SetParent(transform, false);

            // Add RectTransform
            RectTransform rectTransform = particleObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = particleSize;
            rectTransform.anchoredPosition = Vector2.zero;

            // Add Image
            UnityEngine.UI.Image image = particleObj.AddComponent<UnityEngine.UI.Image>();
            image.sprite = particleSprite;
            image.raycastTarget = false;

            // Calculate initial velocity based on effect type
            Vector2 velocity = GetInitialVelocity();

            // Calculate color
            float colorT = Random.value;
            Color color = colorGradient.Evaluate(colorT);

            // Create particle data
            ParticleData particle = new ParticleData
            {
                gameObject = particleObj,
                rectTransform = rectTransform,
                image = image,
                velocity = velocity,
                lifetime = 0f,
                maxLifetime = duration + Random.Range(-0.3f, 0.3f),
                rotation = Random.Range(0f, 360f),
                rotationSpeed = Random.Range(-360f, 360f),
                startColor = color
            };

            image.color = color;
            _particles.Add(particle);
        }

        private Vector2 GetInitialVelocity()
        {
            switch (effectType)
            {
                case ParticleEffectType.Confetti:
                    // Burst upward and outward
                    float angle = Random.Range(-30f, 30f) + 90f; // Mostly upward
                    float velocityMagnitude = speed * Random.Range(0.7f, 1.3f);
                    return new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * velocityMagnitude,
                        Mathf.Sin(angle * Mathf.Deg2Rad) * velocityMagnitude
                    );

                case ParticleEffectType.Sparkle:
                    // Radial burst
                    float sparkleAngle = Random.Range(0f, 360f);
                    float sparkleSpeed = speed * Random.Range(0.5f, 1.5f);
                    return new Vector2(
                        Mathf.Cos(sparkleAngle * Mathf.Deg2Rad) * sparkleSpeed,
                        Mathf.Sin(sparkleAngle * Mathf.Deg2Rad) * sparkleSpeed
                    );

                case ParticleEffectType.Star:
                case ParticleEffectType.Heart:
                    // Gentle upward float
                    return new Vector2(
                        Random.Range(-spread * 0.5f, spread * 0.5f),
                        speed * Random.Range(0.8f, 1.2f)
                    );

                case ParticleEffectType.Custom:
                default:
                    // Random direction
                    return new Vector2(
                        Random.Range(-spread, spread),
                        Random.Range(-spread, spread)
                    );
            }
        }

        private void UpdateParticles()
        {
            if (_particles.Count == 0) return;

            float deltaTime = Time.deltaTime;

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                ParticleData particle = _particles[i];

                // Update lifetime
                particle.lifetime += deltaTime;

                if (particle.lifetime >= particle.maxLifetime)
                {
                    // Particle expired
                    Destroy(particle.gameObject);
                    _particles.RemoveAt(i);
                    continue;
                }

                // Apply gravity
                particle.velocity.y -= gravity * deltaTime;

                // Update position
                Vector2 newPosition = particle.rectTransform.anchoredPosition + particle.velocity * deltaTime;
                particle.rectTransform.anchoredPosition = newPosition;

                // Update rotation
                particle.rotation += particle.rotationSpeed * deltaTime;
                particle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, particle.rotation);

                // Fade out
                if (fadeOut)
                {
                    float lifetimeRatio = particle.lifetime / particle.maxLifetime;
                    float alpha = 1f - lifetimeRatio;
                    Color color = particle.startColor;
                    color.a = alpha;
                    particle.image.color = color;
                }
            }

            // Check if all particles are done
            if (_particles.Count == 0 && destroyOnComplete)
            {
                Destroy(gameObject);
            }
        }

        private void ClearParticles()
        {
            foreach (var particle in _particles)
            {
                if (particle.gameObject != null)
                {
                    Destroy(particle.gameObject);
                }
            }
            _particles.Clear();
        }

        #endregion

        #region Editor Utilities

        [ContextMenu("Play Effect")]
        private void PlayEffect()
        {
            Play();
        }

        [ContextMenu("Stop Effect")]
        private void StopEffect()
        {
            Stop();
        }

        #endregion

        private void OnDestroy()
        {
            ClearParticles();
        }
    }
}
