using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Visuals.Data;

namespace Laboratory.Chimera.Visuals.Generators
{
    /// <summary>
    /// Generates particle effects based on genetic traits
    /// </summary>
    public class ParticleEffectGenerator : MonoBehaviour
    {
        [Header("Particle Configuration")]
        [SerializeField] private float complexityLevel = 1.0f;
        [SerializeField] private int maxActiveEffects = 10;
        [SerializeField] private bool enableEnvironmentalEffects = true;
        [SerializeField] private bool enableGeneticEffects = true;

        [Header("Effect Presets")]
        [SerializeField] private ParticleSystem fireEffectPrefab;
        [SerializeField] private ParticleSystem magicalAuraPrefab;
        [SerializeField] private ParticleSystem sparkleEffectPrefab;
        [SerializeField] private ParticleSystem poisonCloudPrefab;
        [SerializeField] private ParticleSystem iceShardsPrefab;

        private Dictionary<EffectType, ParticleEffectData> effectTemplates = new();
        private List<ParticleSystem> activeEffects = new();

        private void Awake()
        {
            InitializeEffectTemplates();
        }

        public IEnumerator GenerateEffects(VisualGeneticTraits traits, ParticleSystem[] existingSystems)
        {
            Debug.Log("✨ Generating genetic particle effects");

            ClearExistingEffects();

            // Apply effects based on genetic traits
            if (traits.HasMagicalAura && enableGeneticEffects)
            {
                yield return StartCoroutine(CreateMagicalAuraEffect(traits));
            }

            if (traits.HasGlow && enableGeneticEffects)
            {
                yield return StartCoroutine(CreateGlowEffect(traits));
            }

            if (traits.PrimaryEffect != EffectType.None && enableGeneticEffects)
            {
                yield return StartCoroutine(CreateElementalEffect(traits));
            }

            if (traits.HasParticleTrail && enableGeneticEffects)
            {
                yield return StartCoroutine(CreateTrailEffect(traits));
            }

            ApplyComplexityScaling();

            Debug.Log($"✨ Generated {activeEffects.Count} particle effects");
        }

        private void InitializeEffectTemplates()
        {
            effectTemplates[EffectType.Fire] = new ParticleEffectData
            {
                Type = EffectType.Fire,
                EffectColor = new Color(1f, 0.5f, 0f, 1f),
                Intensity = 0.8f,
                Size = 1.0f,
                LifeTime = 2.0f,
                EmissionRate = 50f,
                Velocity = Vector3.up * 3f,
                UseGravity = false,
                IsLooping = true
            };

            effectTemplates[EffectType.Ice] = new ParticleEffectData
            {
                Type = EffectType.Ice,
                EffectColor = new Color(0.7f, 0.9f, 1f, 1f),
                Intensity = 0.6f,
                Size = 0.8f,
                LifeTime = 3.0f,
                EmissionRate = 30f,
                Velocity = Vector3.up * 1f,
                UseGravity = true,
                IsLooping = true
            };

            effectTemplates[EffectType.Lightning] = new ParticleEffectData
            {
                Type = EffectType.Lightning,
                EffectColor = new Color(1f, 1f, 0.8f, 1f),
                Intensity = 1.0f,
                Size = 0.5f,
                LifeTime = 0.5f,
                EmissionRate = 100f,
                Velocity = Vector3.zero,
                UseGravity = false,
                IsLooping = false
            };

            effectTemplates[EffectType.Poison] = new ParticleEffectData
            {
                Type = EffectType.Poison,
                EffectColor = new Color(0.5f, 1f, 0.2f, 0.7f),
                Intensity = 0.4f,
                Size = 1.5f,
                LifeTime = 4.0f,
                EmissionRate = 20f,
                Velocity = Vector3.up * 0.5f,
                UseGravity = false,
                IsLooping = true
            };

            effectTemplates[EffectType.Arcane] = new ParticleEffectData
            {
                Type = EffectType.Arcane,
                EffectColor = new Color(0.8f, 0.3f, 1f, 1f),
                Intensity = 0.9f,
                Size = 0.7f,
                LifeTime = 2.5f,
                EmissionRate = 40f,
                Velocity = Vector3.up * 2f,
                UseGravity = false,
                IsLooping = true
            };
        }

        private IEnumerator CreateMagicalAuraEffect(VisualGeneticTraits traits)
        {
            var auraEffect = CreateParticleSystem("MagicalAura", magicalAuraPrefab);
            if (auraEffect == null) yield break;

            var main = auraEffect.main;
            main.startColor = traits.AccentColor * traits.MagicalIntensity;
            main.startLifetime = 3.0f + traits.MagicalIntensity * 2f;
            main.startSpeed = 1.0f + traits.MagicalIntensity;

            var emission = auraEffect.emission;
            emission.rateOverTime = 20f * traits.MagicalIntensity * complexityLevel;

            var shape = auraEffect.shape;
            shape.radius = 2.0f + traits.MagicalIntensity;

            var colorOverLifetime = auraEffect.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(traits.AccentColor, 0.0f),
                    new GradientColorKey(traits.AccentColor * 0.8f, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(traits.MagicalIntensity, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = gradient;

            activeEffects.Add(auraEffect);
            yield return null;
        }

        private IEnumerator CreateGlowEffect(VisualGeneticTraits traits)
        {
            var glowEffect = CreateParticleSystem("GlowEffect", sparkleEffectPrefab);
            if (glowEffect == null) yield break;

            var main = glowEffect.main;
            main.startColor = traits.AccentColor;
            main.startLifetime = 1.5f;
            main.startSpeed = 0.5f;
            main.startSize = 0.1f;

            var emission = glowEffect.emission;
            emission.rateOverTime = 30f * complexityLevel;

            var shape = glowEffect.shape;
            shape.radius = 1.5f;

            var velocityOverLifetime = glowEffect.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.radial = 2.0f;

            activeEffects.Add(glowEffect);
            yield return null;
        }

        private IEnumerator CreateElementalEffect(VisualGeneticTraits traits)
        {
            if (!effectTemplates.TryGetValue(traits.PrimaryEffect, out var template))
            {
                yield break;
            }

            ParticleSystem prefab = GetEffectPrefab(traits.PrimaryEffect);
            var elementalEffect = CreateParticleSystem($"Elemental_{traits.PrimaryEffect}", prefab);
            if (elementalEffect == null) yield break;

            ApplyParticleEffectData(elementalEffect, template, traits);
            activeEffects.Add(elementalEffect);

            yield return null;
        }

        private IEnumerator CreateTrailEffect(VisualGeneticTraits traits)
        {
            var trailEffect = CreateParticleSystem("ParticleTrail", sparkleEffectPrefab);
            if (trailEffect == null) yield break;

            var main = trailEffect.main;
            main.startColor = traits.SecondaryColor;
            main.startLifetime = 2.0f;
            main.startSpeed = 1.0f;
            main.startSize = 0.2f;

            var emission = trailEffect.emission;
            emission.rateOverTime = 15f * complexityLevel;

            var shape = trailEffect.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;

            var velocityOverLifetime = trailEffect.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;

            activeEffects.Add(trailEffect);
            yield return null;
        }

        private ParticleSystem CreateParticleSystem(string name, ParticleSystem prefab)
        {
            if (activeEffects.Count >= maxActiveEffects)
            {
                Debug.LogWarning($"Maximum particle effects reached ({maxActiveEffects})");
                return null;
            }

            ParticleSystem newEffect;
            if (prefab != null)
            {
                newEffect = Instantiate(prefab, transform);
            }
            else
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform);
                newEffect = go.AddComponent<ParticleSystem>();
            }

            newEffect.name = name;
            return newEffect;
        }

        private ParticleSystem GetEffectPrefab(EffectType effectType)
        {
            return effectType switch
            {
                EffectType.Fire => fireEffectPrefab,
                EffectType.Ice => iceShardsPrefab,
                EffectType.Poison => poisonCloudPrefab,
                EffectType.Arcane => magicalAuraPrefab,
                _ => sparkleEffectPrefab
            };
        }

        private void ApplyParticleEffectData(ParticleSystem particleSystem, ParticleEffectData data, VisualGeneticTraits traits)
        {
            var main = particleSystem.main;
            main.startColor = Color.Lerp(data.EffectColor, traits.PrimaryColor, 0.3f);
            main.startLifetime = data.LifeTime;
            main.startSpeed = data.Velocity.magnitude;
            main.startSize = data.Size * traits.OverallSize;
            main.loop = data.IsLooping;

            var emission = particleSystem.emission;
            emission.rateOverTime = data.EmissionRate * data.Intensity * complexityLevel;

            if (data.UseGravity)
            {
                var forceOverLifetime = particleSystem.forceOverLifetime;
                forceOverLifetime.enabled = true;
                forceOverLifetime.y = -9.81f;
            }

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(main.startColor.color, 0.0f),
                    new GradientColorKey(main.startColor.color * 0.5f, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(data.Intensity, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = gradient;
        }

        private void ApplyComplexityScaling()
        {
            foreach (var effect in activeEffects)
            {
                if (effect == null) continue;

                var emission = effect.emission;
                emission.rateOverTime = emission.rateOverTime.constant * complexityLevel;

                if (complexityLevel < 0.5f)
                {
                    var main = effect.main;
                    main.maxParticles = Mathf.RoundToInt(main.maxParticles * complexityLevel * 2f);
                }
            }
        }

        private void ClearExistingEffects()
        {
            foreach (var effect in activeEffects)
            {
                if (effect != null)
                {
                    DestroyImmediate(effect.gameObject);
                }
            }
            activeEffects.Clear();
        }

        public void SetComplexityLevel(float complexity)
        {
            complexityLevel = Mathf.Clamp01(complexity);
            ApplyComplexityScaling();
        }

        private void OnDestroy()
        {
            ClearExistingEffects();
        }
    }
}