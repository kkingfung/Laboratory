using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Visuals.Data;

namespace Laboratory.Chimera.Visuals.Generators
{
    /// <summary>
    /// Generates magical visual effects for creatures with magical traits
    /// </summary>
    public class MagicalEffectGenerator : MonoBehaviour
    {
        [Header("Magical Effect Configuration")]
        [SerializeField] private float complexityLevel = 1.0f;
        [SerializeField] private bool enableAdvancedEffects = true;
        [SerializeField] private bool enableRuneGeneration = true;
        [SerializeField] private float maxMagicalIntensity = 2.0f;

        [Header("Light Configuration")]
        [SerializeField] private float baseLightIntensity = 1.0f;
        [SerializeField] private float lightRange = 10.0f;
        [SerializeField] private bool enableDynamicLighting = true;
        [SerializeField] private bool enableColorCycling = true;

        [Header("Aura Configuration")]
        [SerializeField] private float auraBaseSize = 2.0f;
        [SerializeField] private float auraPulseSpeed = 1.0f;
        [SerializeField] private bool enableAuraAnimation = true;

        private Dictionary<string, GameObject> activeEffects = new();
        private List<Light> magicalLights = new();
        private Coroutine lightAnimationCoroutine;
        private Coroutine auraAnimationCoroutine;

        // Effect prefabs (would be assigned in inspector or loaded from resources)
        private GameObject auraPrefab;
        private GameObject runePrefab;
        private GameObject energyTrailPrefab;

        public IEnumerator GenerateMagicalEffects(VisualGeneticTraits traits, Light[] existingLights)
        {
            if (!traits.HasMagicalAura && traits.MagicalIntensity <= 0.1f)
            {
                yield break;
            }

            Debug.Log("🔮 Generating magical effects");

            ClearExistingEffects();

            var magicalData = ExtractMagicalVisualData(traits);

            // Generate magical aura
            if (magicalData.HasAura)
            {
                yield return StartCoroutine(CreateMagicalAura(magicalData, traits));
            }

            // Generate energy trail
            if (magicalData.HasEnergyTrail)
            {
                yield return StartCoroutine(CreateEnergyTrail(magicalData, traits));
            }

            // Generate rune markings
            if (magicalData.HasRuneMarkings && enableRuneGeneration)
            {
                yield return StartCoroutine(CreateRuneMarkings(magicalData, traits));
            }

            // Setup magical lighting
            yield return StartCoroutine(SetupMagicalLighting(magicalData, traits, existingLights));

            // Start animation coroutines
            if (enableAdvancedEffects)
            {
                StartEffectAnimations(magicalData, traits);
            }

            Debug.Log($"🔮 Generated {activeEffects.Count} magical effects");
        }

        private MagicalVisualEffects ExtractMagicalVisualData(VisualGeneticTraits traits)
        {
            return new MagicalVisualEffects
            {
                HasAura = traits.HasMagicalAura,
                AuraColor = traits.AccentColor,
                AuraIntensity = Mathf.Min(traits.MagicalIntensity, maxMagicalIntensity),
                AuraSize = auraBaseSize * (1f + traits.MagicalIntensity),
                HasEnergyTrail = traits.MagicalIntensity > 0.5f,
                TrailColor = Color.Lerp(traits.AccentColor, Color.white, 0.3f),
                TrailLength = 2f + traits.MagicalIntensity * 3f,
                HasRuneMarkings = traits.MagicalIntensity > 0.7f,
                RuneColor = traits.AccentColor * 1.5f,
                RuneGlowIntensity = traits.MagicalIntensity
            };
        }

        private IEnumerator CreateMagicalAura(MagicalVisualEffects magicalData, VisualGeneticTraits traits)
        {
            var auraObject = CreateEffectObject("MagicalAura");

            // Create aura visual using a combination of particle system and renderer
            var auraRenderer = auraObject.AddComponent<MeshRenderer>();
            var auraFilter = auraObject.AddComponent<MeshFilter>();

            // Create a sphere mesh for the aura
            auraFilter.mesh = CreateSphereMesh(magicalData.AuraSize);

            // Create aura material
            var auraMaterial = CreateAuraMaterial(magicalData);
            auraRenderer.material = auraMaterial;

            // Position and scale
            auraObject.transform.localPosition = Vector3.zero;
            auraObject.transform.localScale = Vector3.one * magicalData.AuraSize;

            activeEffects["MagicalAura"] = auraObject;
            yield return null;
        }

        private IEnumerator CreateEnergyTrail(MagicalVisualEffects magicalData, VisualGeneticTraits traits)
        {
            var trailObject = CreateEffectObject("EnergyTrail");

            var trailRenderer = trailObject.AddComponent<TrailRenderer>();
            trailRenderer.material = CreateTrailMaterial(magicalData);
            trailRenderer.time = magicalData.TrailLength;
            trailRenderer.startWidth = 0.5f * traits.OverallSize;
            trailRenderer.endWidth = 0.1f * traits.OverallSize;
            trailRenderer.minVertexDistance = 0.1f;

            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(magicalData.TrailColor, 0.0f),
                    new GradientColorKey(magicalData.TrailColor * 0.5f, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            trailRenderer.colorGradient = gradient;

            activeEffects["EnergyTrail"] = trailObject;
            yield return null;
        }

        private IEnumerator CreateRuneMarkings(MagicalVisualEffects magicalData, VisualGeneticTraits traits)
        {
            var runeContainer = CreateEffectObject("RuneMarkings");

            // Create multiple rune objects around the creature
            int runeCount = Mathf.RoundToInt(3 + traits.MagicalIntensity * 3f);

            for (int i = 0; i < runeCount; i++)
            {
                var runeObject = CreateEffectObject($"Rune_{i}", runeContainer.transform);

                // Position runes in a circle around the creature
                float angle = (i / (float)runeCount) * 360f * Mathf.Deg2Rad;
                float radius = 1.5f + traits.OverallSize;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Random.Range(-0.5f, 1.5f),
                    Mathf.Sin(angle) * radius
                );
                runeObject.transform.localPosition = position;

                // Create rune visual
                var runeRenderer = runeObject.AddComponent<MeshRenderer>();
                var runeFilter = runeObject.AddComponent<MeshFilter>();

                runeFilter.mesh = CreateRuneMesh();
                runeRenderer.material = CreateRuneMaterial(magicalData);

                // Random rotation for variety
                runeObject.transform.rotation = Quaternion.Euler(
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f)
                );

                yield return null;
            }

            activeEffects["RuneMarkings"] = runeContainer;
        }

        private IEnumerator SetupMagicalLighting(MagicalVisualEffects magicalData, VisualGeneticTraits traits, Light[] existingLights)
        {
            // Enhance existing lights with magical properties
            foreach (var light in existingLights)
            {
                if (light != null)
                {
                    EnhanceLightWithMagic(light, magicalData);
                    magicalLights.Add(light);
                }
            }

            // Create additional magical lights if needed
            if (magicalData.AuraIntensity > 0.5f)
            {
                var magicalLight = CreateMagicalLight(magicalData, traits);
                if (magicalLight != null)
                {
                    magicalLights.Add(magicalLight);
                }
            }

            yield return null;
        }

        private void EnhanceLightWithMagic(Light light, MagicalVisualEffects magicalData)
        {
            light.color = Color.Lerp(light.color, magicalData.AuraColor, 0.6f);
            light.intensity = light.intensity * (1f + magicalData.AuraIntensity * 0.5f);
            light.range = light.range * (1f + magicalData.AuraIntensity * 0.3f);

            if (enableDynamicLighting)
            {
                light.shadows = LightShadows.Soft;
            }
        }

        private Light CreateMagicalLight(MagicalVisualEffects magicalData, VisualGeneticTraits traits)
        {
            var lightObject = CreateEffectObject("MagicalLight");
            var light = lightObject.AddComponent<Light>();

            light.type = LightType.Point;
            light.color = magicalData.AuraColor;
            light.intensity = baseLightIntensity * magicalData.AuraIntensity;
            light.range = lightRange * (1f + magicalData.AuraIntensity);
            light.shadows = enableDynamicLighting ? LightShadows.Soft : LightShadows.None;

            lightObject.transform.localPosition = Vector3.up * traits.OverallSize;

            activeEffects["MagicalLight"] = lightObject;
            return light;
        }

        private void StartEffectAnimations(MagicalVisualEffects magicalData, VisualGeneticTraits traits)
        {
            if (enableDynamicLighting && magicalLights.Count > 0)
            {
                lightAnimationCoroutine = StartCoroutine(AnimateMagicalLights(magicalData));
            }

            if (enableAuraAnimation && activeEffects.ContainsKey("MagicalAura"))
            {
                auraAnimationCoroutine = StartCoroutine(AnimateMagicalAura(magicalData));
            }
        }

        private IEnumerator AnimateMagicalLights(MagicalVisualEffects magicalData)
        {
            var baseIntensities = new float[magicalLights.Count];
            var baseColors = new Color[magicalLights.Count];

            for (int i = 0; i < magicalLights.Count; i++)
            {
                if (magicalLights[i] != null)
                {
                    baseIntensities[i] = magicalLights[i].intensity;
                    baseColors[i] = magicalLights[i].color;
                }
            }

            while (true)
            {
                float time = Time.time;

                for (int i = 0; i < magicalLights.Count; i++)
                {
                    if (magicalLights[i] == null) continue;

                    // Pulsing intensity
                    float pulse = Mathf.Sin(time * 2f + i * 0.5f) * 0.3f + 1f;
                    magicalLights[i].intensity = baseIntensities[i] * pulse;

                    // Color cycling if enabled
                    if (enableColorCycling)
                    {
                        float hueShift = Mathf.Sin(time * 0.5f + i * 0.3f) * 0.1f;
                        Color.RGBToHSV(baseColors[i], out float h, out float s, out float v);
                        h = (h + hueShift) % 1f;
                        magicalLights[i].color = Color.HSVToRGB(h, s, v);
                    }
                }

                yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator AnimateMagicalAura(MagicalVisualEffects magicalData)
        {
            var auraObject = activeEffects["MagicalAura"];
            var baseScale = auraObject.transform.localScale;

            while (auraObject != null)
            {
                float pulse = Mathf.Sin(Time.time * auraPulseSpeed) * 0.2f + 1f;
                auraObject.transform.localScale = baseScale * pulse;

                // Slow rotation
                auraObject.transform.Rotate(Vector3.up, 10f * Time.deltaTime);

                yield return null;
            }
        }

        private GameObject CreateEffectObject(string name, Transform parent = null)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent ?? transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            return obj;
        }

        private Material CreateAuraMaterial(MagicalVisualEffects magicalData)
        {
            var material = new Material(Shader.Find("Standard"));
            material.SetColor("_Color", magicalData.AuraColor);
            material.SetColor("_EmissionColor", magicalData.AuraColor * magicalData.AuraIntensity);
            material.EnableKeyword("_EMISSION");
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            return material;
        }

        private Material CreateTrailMaterial(MagicalVisualEffects magicalData)
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.SetColor("_Color", magicalData.TrailColor);
            return material;
        }

        private Material CreateRuneMaterial(MagicalVisualEffects magicalData)
        {
            var material = new Material(Shader.Find("Standard"));
            material.SetColor("_Color", magicalData.RuneColor);
            material.SetColor("_EmissionColor", magicalData.RuneColor * magicalData.RuneGlowIntensity);
            material.EnableKeyword("_EMISSION");
            return material;
        }

        private Mesh CreateSphereMesh(float radius)
        {
            var mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            return mesh;
        }

        private Mesh CreateRuneMesh()
        {
            // Create a simple quad for rune symbols
            var mesh = new Mesh();

            Vector3[] vertices = {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0),
                new Vector3(0.5f, 0.5f, 0)
            };

            int[] triangles = { 0, 2, 1, 2, 3, 1 };

            Vector2[] uvs = {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            return mesh;
        }

        private void ClearExistingEffects()
        {
            foreach (var effect in activeEffects.Values)
            {
                if (effect != null)
                {
                    DestroyImmediate(effect);
                }
            }
            activeEffects.Clear();
            magicalLights.Clear();

            if (lightAnimationCoroutine != null)
            {
                StopCoroutine(lightAnimationCoroutine);
                lightAnimationCoroutine = null;
            }

            if (auraAnimationCoroutine != null)
            {
                StopCoroutine(auraAnimationCoroutine);
                auraAnimationCoroutine = null;
            }
        }

        public void SetComplexityLevel(float complexity)
        {
            complexityLevel = Mathf.Clamp01(complexity);

            // Adjust effect quality based on complexity
            if (complexityLevel < 0.5f)
            {
                enableAdvancedEffects = false;
                enableColorCycling = false;
            }
            else
            {
                enableAdvancedEffects = true;
                enableColorCycling = true;
            }
        }

        private void OnDestroy()
        {
            ClearExistingEffects();
        }
    }
}