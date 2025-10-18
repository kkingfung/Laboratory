using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Visuals.Data;

namespace Laboratory.Chimera.Visuals.Generators
{
    /// <summary>
    /// Advanced pattern generation system for creature visuals
    /// </summary>
    public class PatternGenerator : MonoBehaviour
    {
        [Header("Pattern Configuration")]
        [SerializeField] private int textureResolution = 512;
        [SerializeField] private bool enableAnimatedPatterns = true;
        [SerializeField] private float animationSpeed = 1.0f;
        [SerializeField] private bool cacheGeneratedTextures = true;

        private Dictionary<string, Texture2D> patternCache = new();
        private float complexityLevel = 1.0f;

        public void SetComplexityLevel(float complexity)
        {
            complexityLevel = Mathf.Clamp01(complexity);
        }

        public IEnumerator GeneratePatterns(VisualGeneticTraits traits, Renderer[] renderers)
        {
            Debug.Log("🎨 Generating procedural patterns");

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.material == null) continue;

                yield return StartCoroutine(ApplyPatternsToRenderer(traits, renderer));
            }

            Debug.Log("✨ Pattern generation complete");
        }

        private IEnumerator ApplyPatternsToRenderer(VisualGeneticTraits traits, Renderer renderer)
        {
            // Generate primary pattern
            if (traits.PrimaryPattern != PatternType.None)
            {
                var primaryTexture = GeneratePatternTexture(traits.PrimaryPattern, traits, "_Primary");
                ApplyPatternToMaterial(renderer.material, primaryTexture, "_MainTex");
                yield return null;
            }

            // Generate secondary pattern if complex enough
            if (traits.PatternComplexity > 0.5f && traits.SecondaryPattern != PatternType.None)
            {
                var secondaryTexture = GeneratePatternTexture(traits.SecondaryPattern, traits, "_Secondary");
                ApplyPatternToMaterial(renderer.material, secondaryTexture, "_DetailAlbedoMap");
                yield return null;
            }

            // Apply animated patterns if enabled
            if (enableAnimatedPatterns && traits.PatternComplexity > 0.7f)
            {
                StartCoroutine(AnimatePatterns(renderer.material, traits));
            }
        }

        private Texture2D GeneratePatternTexture(PatternType patternType, VisualGeneticTraits traits, string suffix)
        {
            var cacheKey = $"{patternType}_{traits.PatternScale}_{traits.PatternIntensity}_{suffix}";

            if (cacheGeneratedTextures && patternCache.TryGetValue(cacheKey, out var cachedTexture))
            {
                return cachedTexture;
            }

            var resolution = Mathf.RoundToInt(textureResolution * complexityLevel);
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

            switch (patternType)
            {
                case PatternType.Stripes:
                    GenerateStripePattern(texture, traits);
                    break;
                case PatternType.Spots:
                    GenerateSpotPattern(texture, traits);
                    break;
                case PatternType.Scales:
                    GenerateScalePattern(texture, traits);
                    break;
                case PatternType.Tribal:
                    GenerateTribalPattern(texture, traits);
                    break;
                case PatternType.Geometric:
                    GenerateGeometricPattern(texture, traits);
                    break;
                case PatternType.Organic:
                    GenerateOrganicPattern(texture, traits);
                    break;
                case PatternType.Crystalline:
                    GenerateCrystallinePattern(texture, traits);
                    break;
                case PatternType.Magical:
                    GenerateMagicalPattern(texture, traits);
                    break;
                default:
                    GenerateNoisePattern(texture, traits);
                    break;
            }

            texture.Apply();

            if (cacheGeneratedTextures)
            {
                patternCache[cacheKey] = texture;
            }

            return texture;
        }

        private void GenerateStripePattern(Texture2D texture, VisualGeneticTraits traits)
        {
            var colors = texture.GetPixels();
            var resolution = texture.width;
            var stripeWidth = Mathf.Max(1, Mathf.RoundToInt(resolution * traits.PatternScale * 0.1f));

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var stripeIndex = (x / stripeWidth) % 2;
                    var color = stripeIndex == 0 ? traits.PrimaryColor : traits.SecondaryColor;

                    // Apply intensity
                    color = Color.Lerp(traits.PrimaryColor, color, traits.PatternIntensity);

                    colors[y * resolution + x] = color;
                }
            }

            texture.SetPixels(colors);
        }

        private void GenerateSpotPattern(Texture2D texture, VisualGeneticTraits traits)
        {
            var colors = texture.GetPixels();
            var resolution = texture.width;
            var spotCount = Mathf.RoundToInt(20 * traits.PatternComplexity);
            var spotSize = traits.PatternScale * 0.1f;

            // Fill with base color
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = traits.PrimaryColor;
            }

            // Generate random spots
            var random = new System.Random(traits.PrimaryColor.GetHashCode());
            for (int spot = 0; spot < spotCount; spot++)
            {
                var centerX = random.Next(0, resolution);
                var centerY = random.Next(0, resolution);
                var radius = random.Next(1, Mathf.RoundToInt(resolution * spotSize));

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        var distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                        if (distance <= radius)
                        {
                            var intensity = 1f - (distance / radius);
                            var spotColor = Color.Lerp(traits.PrimaryColor, traits.SecondaryColor, intensity * traits.PatternIntensity);
                            colors[y * resolution + x] = spotColor;
                        }
                    }
                }
            }

            texture.SetPixels(colors);
        }

        private void GenerateScalePattern(Texture2D texture, VisualGeneticTraits traits)
        {
            var colors = texture.GetPixels();
            var resolution = texture.width;
            var scaleSize = Mathf.Max(4, Mathf.RoundToInt(resolution * traits.PatternScale * 0.05f));

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // Create hexagonal scale pattern
                    var scaleX = x / scaleSize;
                    var scaleY = y / scaleSize;

                    // Offset every other row for hexagonal tiling
                    if (scaleY % 2 == 1) scaleX += 0.5f;

                    var centerX = (scaleX - Mathf.Floor(scaleX)) - 0.5f;
                    var centerY = (scaleY - Mathf.Floor(scaleY)) - 0.5f;

                    var distance = Mathf.Sqrt(centerX * centerX + centerY * centerY);
                    var scaleIntensity = Mathf.Clamp01(1f - distance * 2f);

                    var color = Color.Lerp(traits.PrimaryColor, traits.AccentColor, scaleIntensity * traits.PatternIntensity);
                    colors[y * resolution + x] = color;
                }
            }

            texture.SetPixels(colors);
        }

        private void GenerateTribalPattern(Texture2D texture, VisualGeneticTraits traits)
        {
            var colors = texture.GetPixels();
            var resolution = texture.width;

            // Fill with base color
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = traits.PrimaryColor;
            }

            // Generate tribal lines and shapes
            var random = new System.Random(traits.SecondaryColor.GetHashCode());
            var lineCount = Mathf.RoundToInt(10 * traits.PatternComplexity);

            for (int line = 0; line < lineCount; line++)
            {
                var startX = random.Next(0, resolution);
                var startY = random.Next(0, resolution);
                var endX = random.Next(0, resolution);
                var endY = random.Next(0, resolution);
                var thickness = Mathf.Max(1, Mathf.RoundToInt(traits.PatternScale * 3));

                DrawLine(colors, resolution, startX, startY, endX, endY, thickness, traits.SecondaryColor, traits.PatternIntensity);
            }

            texture.SetPixels(colors);
        }

        private void GenerateGeometricPattern(Texture2D texture, VisualGeneticTraits traits)
        {
            var colors = texture.GetPixels();
            var resolution = texture.width;
            var gridSize = Mathf.Max(8, Mathf.RoundToInt(resolution * traits.PatternScale * 0.1f));

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var gridX = x / gridSize;
                    var gridY = y / gridSize;

                    // Create checkerboard with geometric variations
                    var pattern = (gridX + gridY) % 2;
                    var color = pattern == 0 ? traits.PrimaryColor : traits.SecondaryColor;

                    // Add diamond pattern within each cell
                    var cellX = x % gridSize;
                    var cellY = y % gridSize;
                    var centerDist = Vector2.Distance(new Vector2(cellX, cellY), new Vector2(gridSize/2f, gridSize/2f));
                    var diamondIntensity = 1f - (centerDist / (gridSize * 0.7f));

                    if (diamondIntensity > 0)
                    {
                        color = Color.Lerp(color, traits.AccentColor, diamondIntensity * traits.PatternIntensity);
                    }

                    colors[y * resolution + x] = color;
                }
            }

            texture.SetPixels(colors);
        }

        private void GenerateOrganicPattern(Texture2D texture, VisualGeneticTraits traits)
        {
            var colors = texture.GetPixels();
            var resolution = texture.width;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // Multi-octave Perlin noise for organic feel
                    var noise1 = Mathf.PerlinNoise(x * traits.PatternScale * 0.01f, y * traits.PatternScale * 0.01f);
                    var noise2 = Mathf.PerlinNoise(x * traits.PatternScale * 0.02f, y * traits.PatternScale * 0.02f) * 0.5f;
                    var noise3 = Mathf.PerlinNoise(x * traits.PatternScale * 0.04f, y * traits.PatternScale * 0.04f) * 0.25f;

                    var combinedNoise = (noise1 + noise2 + noise3) / 1.75f;
                    var color = Color.Lerp(traits.PrimaryColor, traits.SecondaryColor, combinedNoise * traits.PatternIntensity);

                    colors[y * resolution + x] = color;
                }
            }

            texture.SetPixels(colors);
        }

        private void GenerateCrystallinePattern(Texture2D texture, VisualGeneticTraits traits)
        {
            var colors = texture.GetPixels();
            var resolution = texture.width;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // Create crystal-like angular patterns
                    var angle = Mathf.Atan2(y - resolution/2f, x - resolution/2f);
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(resolution/2f, resolution/2f));

                    var segments = Mathf.RoundToInt(6 * traits.PatternComplexity);
                    var segmentAngle = (Mathf.PI * 2) / segments;
                    var currentSegment = Mathf.FloorToInt(angle / segmentAngle);

                    var intensity = Mathf.Abs(Mathf.Sin(distance * traits.PatternScale * 0.1f));
                    var color = currentSegment % 2 == 0 ? traits.PrimaryColor : traits.SecondaryColor;
                    color = Color.Lerp(color, traits.AccentColor, intensity * traits.PatternIntensity);

                    colors[y * resolution + x] = color;
                }
            }

            texture.SetPixels(colors);
        }

        private void GenerateMagicalPattern(Texture2D texture, VisualGeneticTraits traits)
        {
            var colors = texture.GetPixels();
            var resolution = texture.width;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // Magical swirling energy pattern
                    var centerX = resolution / 2f;
                    var centerY = resolution / 2f;
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    var angle = Mathf.Atan2(y - centerY, x - centerX);

                    var spiral = Mathf.Sin(angle * 3 + distance * traits.PatternScale * 0.05f + Time.time * animationSpeed);
                    var energy = Mathf.Sin(distance * traits.PatternScale * 0.02f + Time.time * animationSpeed * 2);

                    var intensity = (spiral + energy) * 0.5f + 0.5f;
                    var color = Color.Lerp(traits.PrimaryColor, Color.white, intensity * traits.PatternIntensity);

                    // Add magical glow
                    if (traits.HasMagicalAura)
                    {
                        color = Color.Lerp(color, traits.AccentColor, traits.MagicalIntensity * 0.3f);
                    }

                    colors[y * resolution + x] = color;
                }
            }

            texture.SetPixels(colors);
        }

        private void GenerateNoisePattern(Texture2D texture, VisualGeneticTraits traits)
        {
            var colors = texture.GetPixels();
            var resolution = texture.width;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var noise = Mathf.PerlinNoise(x * traits.PatternScale * 0.01f, y * traits.PatternScale * 0.01f);
                    var color = Color.Lerp(traits.PrimaryColor, traits.SecondaryColor, noise * traits.PatternIntensity);
                    colors[y * resolution + x] = color;
                }
            }

            texture.SetPixels(colors);
        }

        private void DrawLine(Color[] colors, int resolution, int x0, int y0, int x1, int y1, int thickness, Color color, float intensity)
        {
            // Bresenham's line algorithm with thickness
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int x = x0;
            int y = y0;
            int n1 = 0, n2 = 0;
            int xStep = x1 > x0 ? 1 : -1;
            int yStep = y1 > y0 ? 1 : -1;

            if (dx > dy)
            {
                n1 = dy + dy;
                n2 = n1 - dx;
                n2 -= dx;

                for (int i = 0; i <= dx; i++)
                {
                    DrawThickPixel(colors, resolution, x, y, thickness, color, intensity);

                    if (n2 < 0)
                    {
                        n2 += n1;
                    }
                    else
                    {
                        y += yStep;
                        n2 += n2;
                    }
                    x += xStep;
                }
            }
            else
            {
                n1 = dx + dx;
                n2 = n1 - dy;
                n2 -= dy;

                for (int i = 0; i <= dy; i++)
                {
                    DrawThickPixel(colors, resolution, x, y, thickness, color, intensity);

                    if (n2 < 0)
                    {
                        n2 += n1;
                    }
                    else
                    {
                        x += xStep;
                        n2 += n2;
                    }
                    y += yStep;
                }
            }
        }

        private void DrawThickPixel(Color[] colors, int resolution, int centerX, int centerY, int thickness, Color color, float intensity)
        {
            for (int dy = -thickness/2; dy <= thickness/2; dy++)
            {
                for (int dx = -thickness/2; dx <= thickness/2; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        var index = y * resolution + x;
                        colors[index] = Color.Lerp(colors[index], color, intensity);
                    }
                }
            }
        }

        private void ApplyPatternToMaterial(Material material, Texture2D texture, string propertyName)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private IEnumerator AnimatePatterns(Material material, VisualGeneticTraits traits)
        {
            while (enableAnimatedPatterns && material != null)
            {
                if (material.HasProperty("_Time"))
                {
                    material.SetFloat("_Time", Time.time * animationSpeed);
                }

                if (traits.HasMagicalAura && material.HasProperty("_MagicalIntensity"))
                {
                    var intensity = Mathf.Sin(Time.time * animationSpeed * 2f) * 0.5f + 0.5f;
                    material.SetFloat("_MagicalIntensity", intensity * traits.MagicalIntensity);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnDestroy()
        {
            // Clean up cached textures
            foreach (var texture in patternCache.Values)
            {
                if (texture != null)
                {
                    DestroyImmediate(texture);
                }
            }
            patternCache.Clear();
        }
    }
}