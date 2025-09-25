using UnityEngine;
using Laboratory.Chimera.Genetics;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Laboratory.Chimera.Visuals
{
    /// <summary>
    /// Advanced pattern generation system for Project Chimera creatures.
    /// Creates sophisticated patterns including camouflage, iridescence, bioluminescence,
    /// and adaptive coloration based on genetic traits and environmental factors.
    /// </summary>
    public class AdvancedPatternSystem : MonoBehaviour
    {
        [Header("Pattern Configuration")]
        [SerializeField] private PatternDefinition[] availablePatterns;
        [SerializeField] private bool enableAdaptiveCamouflage = true;
        [SerializeField] private bool enableEnvironmentalAdaptation = true;
        [SerializeField] private bool enableRealTimePatternUpdates = false;
        
        [Header("Pattern Quality")]
        [SerializeField] private int baseTextureResolution = 512;
        [SerializeField] private bool useTextureCompression = true;
        [SerializeField] private float patternUpdateInterval = 2f;
        
        [Header("Adaptive Systems")]
        [SerializeField] private float adaptationSpeed = 0.1f;
        [SerializeField] private float camouflageEffectiveness = 1f;
        [SerializeField] private bool enableEmotionalColoration = true;
        
        // Pattern state
        private string currentPattern = "";
        private float currentComplexity = 0.5f;
        private Dictionary<string, Texture2D> patternCache = new Dictionary<string, Texture2D>();
        private Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        
        // Adaptive coloration
        private Color[] currentPalette;
        private string lastEnvironment = "";
        private float lastAdaptationTime = 0f;
        
        // Components
        private CreatureInstanceComponent creatureInstance;
        private Renderer[] targetRenderers;
        
        #region Initialization
        
        private void Awake()
        {
            creatureInstance = GetComponent<CreatureInstanceComponent>();
            targetRenderers = GetComponentsInChildren<Renderer>();
            
            InitializeDefaultPatterns();
        }
        
        private void Start()
        {
            if (enableRealTimePatternUpdates)
            {
                InvokeRepeating(nameof(UpdateAdaptivePatterns), patternUpdateInterval, patternUpdateInterval);
            }
            
            if (enableAdaptiveCamouflage)
            {
                StartCoroutine(MonitorEnvironmentForCamouflage());
            }
        }
        
        private void InitializeDefaultPatterns()
        {
            if (availablePatterns == null || availablePatterns.Length == 0)
            {
                CreateDefaultPatternDefinitions();
            }
        }
        
        private void CreateDefaultPatternDefinitions()
        {
            availablePatterns = new PatternDefinition[]
            {
                new PatternDefinition
                {
                    patternName = "Solid",
                    type = PatternType.Solid,
                    complexityMultiplier = 0.1f,
                    defaultColors = new Color[] { Color.gray }
                },
                new PatternDefinition
                {
                    patternName = "Stripes",
                    type = PatternType.Stripes,
                    complexityMultiplier = 0.5f,
                    defaultColors = new Color[] { Color.black, Color.white }
                },
                new PatternDefinition
                {
                    patternName = "Spots",
                    type = PatternType.Spots,
                    complexityMultiplier = 0.6f,
                    defaultColors = new Color[] { Color.brown, Color.yellow }
                },
                new PatternDefinition
                {
                    patternName = "Camouflage",
                    type = PatternType.Camouflage,
                    complexityMultiplier = 0.8f,
                    defaultColors = new Color[] { Color.green, Color.brown, Color.black }
                },
                new PatternDefinition
                {
                    patternName = "Iridescent",
                    type = PatternType.Iridescent,
                    complexityMultiplier = 0.9f,
                    defaultColors = new Color[] { Color.cyan, Color.magenta, Color.yellow }
                },
                new PatternDefinition
                {
                    patternName = "Bioluminescent",
                    type = PatternType.Bioluminescent,
                    complexityMultiplier = 0.7f,
                    defaultColors = new Color[] { Color.blue, Color.cyan }
                }
            };
        }
        
        #endregion
        
        #region Pattern Generation
        
        /// <summary>
        /// Generate a specific pattern with given complexity
        /// </summary>
        public void GeneratePattern(string patternType, float complexity)
        {
            currentPattern = patternType;
            currentComplexity = Mathf.Clamp01(complexity);
            
            Debug.Log($"🎨 Generating pattern: {patternType} (complexity: {complexity:F2})");
            
            // Get or generate palette
            Color[] palette = GetGeneticColorPalette();
            
            // Generate pattern texture
            Texture2D patternTexture = CreatePatternTexture(patternType, complexity, palette);
            
            // Apply to renderers
            ApplyPatternToRenderers(patternTexture);
            
            // Cache for performance
            string cacheKey = $"{patternType}_{complexity:F2}_{GetPaletteHash(palette)}";
            patternCache[cacheKey] = patternTexture;
            
            Debug.Log($"✅ Pattern applied: {patternType}");
        }
        
        /// <summary>
        /// Create procedural pattern texture
        /// </summary>
        private Texture2D CreatePatternTexture(string patternType, float complexity, Color[] palette)
        {
            int resolution = Mathf.RoundToInt(baseTextureResolution * (0.5f + complexity * 0.5f));
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            
            switch (patternType.ToLower())
            {
                case "stripes":
                    GenerateStripesPattern(texture, palette, complexity);
                    break;
                case "spots":
                    GenerateSpotsPattern(texture, palette, complexity);
                    break;
                case "camouflage":
                    GenerateCamouflagePattern(texture, palette, complexity);
                    break;
                case "iridescent":
                    GenerateIridescentPattern(texture, palette, complexity);
                    break;
                case "bioluminescent":
                    GenerateBioluminescentPattern(texture, palette, complexity);
                    break;
                case "geometric":
                    GenerateGeometricPattern(texture, palette, complexity);
                    break;
                case "fractal":
                    GenerateFractalPattern(texture, palette, complexity);
                    break;
                default:
                    GenerateSolidPattern(texture, palette);
                    break;
            }
            
            texture.Apply();
            
            if (useTextureCompression)
            {
                texture.Compress(true);
            }
            
            return texture;
        }
        
        #endregion
        
        #region Pattern Generators
        
        private void GenerateStripesPattern(Texture2D texture, Color[] palette, float complexity)
        {
            int width = texture.width;
            int height = texture.height;
            
            // Stripe width based on complexity
            float stripeWidth = Mathf.Lerp(width * 0.1f, width * 0.02f, complexity);
            bool vertical = UnityEngine.Random.value > 0.5f;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int coordinate = vertical ? x : y;
                    int stripeIndex = Mathf.FloorToInt(coordinate / stripeWidth) % palette.Length;
                    
                    // Add noise for organic look
                    float noise = Mathf.PerlinNoise(x * 0.01f * complexity, y * 0.01f * complexity);
                    stripeIndex = (stripeIndex + (noise > 0.6f ? 1 : 0)) % palette.Length;
                    
                    texture.SetPixel(x, y, palette[stripeIndex]);
                }
            }
        }
        
        private void GenerateSpotsPattern(Texture2D texture, Color[] palette, float complexity)
        {
            int width = texture.width;
            int height = texture.height;
            
            // Fill with base color
            Color baseColor = palette[0];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, baseColor);
                }
            }
            
            // Generate spots
            int spotCount = Mathf.RoundToInt(20 + complexity * 30);
            float maxSpotSize = width * 0.08f;
            float minSpotSize = width * 0.02f;
            
            for (int i = 0; i < spotCount; i++)
            {
                int centerX = UnityEngine.Random.Range(0, width);
                int centerY = UnityEngine.Random.Range(0, height);
                float spotSize = UnityEngine.Random.Range(minSpotSize, maxSpotSize * complexity);
                Color spotColor = palette[UnityEngine.Random.Range(1, palette.Length)];
                
                // Draw circular spot with soft edges
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                        
                        if (distance <= spotSize)
                        {
                            float alpha = 1f - (distance / spotSize);
                            alpha = Mathf.Pow(alpha, 2f); // Soft falloff
                            
                            Color currentColor = texture.GetPixel(x, y);
                            Color blendedColor = Color.Lerp(currentColor, spotColor, alpha);
                            texture.SetPixel(x, y, blendedColor);
                        }
                    }
                }
            }
        }
        
        private void GenerateCamouflagePattern(Texture2D texture, Color[] palette, float complexity)
        {
            int width = texture.width;
            int height = texture.height;
            
            // Multiple octaves of noise for organic camouflage
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float noise1 = Mathf.PerlinNoise(x * 0.01f, y * 0.01f);
                    float noise2 = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.5f;
                    float noise3 = Mathf.PerlinNoise(x * 0.07f, y * 0.07f) * 0.25f;
                    
                    float combinedNoise = (noise1 + noise2 + noise3) * complexity;
                    
                    // Map noise to color palette
                    int colorIndex = Mathf.FloorToInt(combinedNoise * palette.Length);
                    colorIndex = Mathf.Clamp(colorIndex, 0, palette.Length - 1);
                    
                    // Blend with neighbors for smoother transitions
                    Color finalColor = palette[colorIndex];
                    
                    // Add micro-detail
                    if (complexity > 0.7f)
                    {
                        float microNoise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
                        finalColor = Color.Lerp(finalColor, palette[(colorIndex + 1) % palette.Length], microNoise * 0.2f);
                    }
                    
                    texture.SetPixel(x, y, finalColor);
                }
            }
        }
        
        private void GenerateIridescentPattern(Texture2D texture, Color[] palette, float complexity)
        {
            int width = texture.width;
            int height = texture.height;
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Calculate angle and distance from center
                    Vector2 pos = new Vector2(x, y);
                    float angle = Mathf.Atan2(pos.y - center.y, pos.x - center.x);
                    float distance = Vector2.Distance(pos, center) / (width * 0.5f);
                    
                    // Create rainbow effect
                    float hue = (angle + Mathf.PI) / (2f * Mathf.PI); // 0-1
                    hue += distance * complexity * 0.5f; // Distance affects hue
                    hue = hue % 1f; // Wrap around
                    
                    // Add wave patterns
                    float wave1 = Mathf.Sin(distance * 20f * complexity);
                    float wave2 = Mathf.Cos(angle * 8f * complexity);
                    hue += (wave1 + wave2) * 0.1f;
                    
                    Color iridescent = Color.HSVToRGB(hue, 0.8f + complexity * 0.2f, 0.9f);
                    texture.SetPixel(x, y, iridescent);
                }
            }
        }
        
        private void GenerateBioluminescentPattern(Texture2D texture, Color[] palette, float complexity)
        {
            int width = texture.width;
            int height = texture.height;
            
            // Dark base
            Color baseColor = new Color(0.1f, 0.1f, 0.15f);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, baseColor);
                }
            }
            
            // Glowing patterns
            int glowSpotCount = Mathf.RoundToInt(5 + complexity * 15);
            
            for (int i = 0; i < glowSpotCount; i++)
            {
                int centerX = UnityEngine.Random.Range(0, width);
                int centerY = UnityEngine.Random.Range(0, height);
                float glowSize = UnityEngine.Random.Range(width * 0.03f, width * 0.1f * complexity);
                Color glowColor = palette[UnityEngine.Random.Range(0, palette.Length)];
                
                // Create glowing spot
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                        
                        if (distance <= glowSize * 2f)
                        {
                            float intensity = Mathf.Exp(-distance / glowSize);
                            intensity = Mathf.Clamp01(intensity);
                            
                            Color currentColor = texture.GetPixel(x, y);
                            Color blendedColor = Color.Lerp(currentColor, glowColor, intensity * 0.8f);
                            texture.SetPixel(x, y, blendedColor);
                        }
                    }
                }
            }
            
            // Add connecting lines for circuit-like pattern
            if (complexity > 0.6f)
            {
                GenerateBioluminescentLines(texture, palette, complexity);
            }
        }
        
        private void GenerateBioluminescentLines(Texture2D texture, Color[] palette, float complexity)
        {
            int width = texture.width;
            int height = texture.height;
            int lineCount = Mathf.RoundToInt(3 + complexity * 7);
            
            for (int i = 0; i < lineCount; i++)
            {
                Vector2 start = new Vector2(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height));
                Vector2 end = new Vector2(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height));
                Color lineColor = palette[UnityEngine.Random.Range(0, palette.Length)];
                
                DrawGlowingLine(texture, start, end, lineColor, 2f);
            }
        }
        
        private void DrawGlowingLine(Texture2D texture, Vector2 start, Vector2 end, Color color, float width)
        {
            int steps = Mathf.RoundToInt(Vector2.Distance(start, end));
            
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                
                // Draw thick line with glow
                for (int dx = -Mathf.RoundToInt(width); dx <= Mathf.RoundToInt(width); dx++)
                {
                    for (int dy = -Mathf.RoundToInt(width); dy <= Mathf.RoundToInt(width); dy++)
                    {
                        int x = Mathf.RoundToInt(point.x) + dx;
                        int y = Mathf.RoundToInt(point.y) + dy;
                        
                        if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                        {
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);
                            float intensity = Mathf.Exp(-distance / width);
                            
                            Color currentColor = texture.GetPixel(x, y);
                            Color blendedColor = Color.Lerp(currentColor, color, intensity * 0.6f);
                            texture.SetPixel(x, y, blendedColor);
                        }
                    }
                }
            }
        }
        
        private void GenerateGeometricPattern(Texture2D texture, Color[] palette, float complexity)
        {
            int width = texture.width;
            int height = texture.height;
            
            // Geometric shapes based on complexity
            int shapeCount = Mathf.RoundToInt(5 + complexity * 15);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, palette[0]);
                }
            }
            
            for (int i = 0; i < shapeCount; i++)
            {
                DrawGeometricShape(texture, palette, complexity);
            }
        }
        
        private void DrawGeometricShape(Texture2D texture, Color[] palette, float complexity)
        {
            int width = texture.width;
            int height = texture.height;
            
            Vector2 center = new Vector2(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height));
            float size = UnityEngine.Random.Range(width * 0.05f, width * 0.15f * complexity);
            Color shapeColor = palette[UnityEngine.Random.Range(1, palette.Length)];
            
            int shapeType = UnityEngine.Random.Range(0, 3);
            
            switch (shapeType)
            {
                case 0: // Circle
                    DrawCircle(texture, center, size, shapeColor);
                    break;
                case 1: // Square
                    DrawSquare(texture, center, size, shapeColor);
                    break;
                case 2: // Triangle
                    DrawTriangle(texture, center, size, shapeColor);
                    break;
            }
        }
        
        private void DrawCircle(Texture2D texture, Vector2 center, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + radius));
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance <= radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
        
        private void DrawSquare(Texture2D texture, Vector2 center, float size, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - size * 0.5f));
            int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + size * 0.5f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - size * 0.5f));
            int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + size * 0.5f));
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
        
        private void DrawTriangle(Texture2D texture, Vector2 center, float size, Color color)
        {
            // Simple triangle drawing - can be improved
            Vector2[] vertices = new Vector2[]
            {
                center + Vector2.up * size * 0.5f,
                center + new Vector2(-size * 0.5f, -size * 0.5f),
                center + new Vector2(size * 0.5f, -size * 0.5f)
            };
            
            // Simple rasterization
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - size * 0.5f));
            int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + size * 0.5f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - size * 0.5f));
            int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + size * 0.5f));
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (IsPointInTriangle(new Vector2(x, y), vertices[0], vertices[1], vertices[2]))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
        
        private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            float alpha = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / denom;
            float beta = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / denom;
            float gamma = 1 - alpha - beta;
            
            return alpha >= 0 && beta >= 0 && gamma >= 0;
        }
        
        private void GenerateFractalPattern(Texture2D texture, Color[] palette, float complexity)
        {
            int width = texture.width;
            int height = texture.height;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float fractalValue = CalculateFractal(x, y, width, height, complexity);
                    int colorIndex = Mathf.FloorToInt(fractalValue * palette.Length);
                    colorIndex = Mathf.Clamp(colorIndex, 0, palette.Length - 1);
                    
                    texture.SetPixel(x, y, palette[colorIndex]);
                }
            }
        }
        
        private float CalculateFractal(int x, int y, int width, int height, float complexity)
        {
            // Simple Mandelbrot-like fractal
            float cx = (x - width * 0.5f) / (width * 0.3f);
            float cy = (y - height * 0.5f) / (height * 0.3f);
            
            float zx = 0f;
            float zy = 0f;
            int maxIterations = Mathf.RoundToInt(10 + complexity * 40);
            
            for (int i = 0; i < maxIterations; i++)
            {
                float zx2 = zx * zx;
                float zy2 = zy * zy;
                
                if (zx2 + zy2 > 4f)
                {
                    return i / (float)maxIterations;
                }
                
                zy = 2f * zx * zy + cy;
                zx = zx2 - zy2 + cx;
            }
            
            return 1f;
        }
        
        private void GenerateSolidPattern(Texture2D texture, Color[] palette)
        {
            Color solidColor = palette[0];
            
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    texture.SetPixel(x, y, solidColor);
                }
            }
        }
        
        #endregion
        
        #region Adaptive Systems
        
        private void UpdateAdaptivePatterns()
        {
            if (enableEmotionalColoration)
            {
                ApplyEmotionalColoration();
            }
            
            if (enableEnvironmentalAdaptation)
            {
                CheckEnvironmentalAdaptation();
            }
        }
        
        private void ApplyEmotionalColoration()
        {
            if (creatureInstance?.CreatureData == null) return;
            
            // Get emotional state (you might have different properties)
            float happiness = creatureInstance.CreatureData.Happiness;
            float stress = 1f - happiness; // Simple stress calculation
            
            // Apply emotional tint
            Color emotionalTint = Color.white;
            
            if (happiness > 0.8f)
            {
                emotionalTint = Color.Lerp(Color.white, Color.green, (happiness - 0.8f) * 5f * 0.2f);
            }
            else if (stress > 0.6f)
            {
                emotionalTint = Color.Lerp(Color.white, Color.red, (stress - 0.6f) * 2.5f * 0.15f);
            }
            
            ApplyColorTintToRenderers(emotionalTint);
        }
        
        private void CheckEnvironmentalAdaptation()
        {
            string currentEnvironment = DetectCurrentEnvironment();
            
            if (currentEnvironment != lastEnvironment && Time.time - lastAdaptationTime > 5f)
            {
                StartCoroutine(AdaptToEnvironment(currentEnvironment));
                lastEnvironment = currentEnvironment;
                lastAdaptationTime = Time.time;
            }
        }
        
        private string DetectCurrentEnvironment()
        {
            // Simple environment detection based on position or triggers
            // You might have a more sophisticated environment system
            
            Collider[] nearbyTriggers = Physics.OverlapSphere(transform.position, 10f);
            
            foreach (var trigger in nearbyTriggers)
            {
                if (trigger.CompareTag("Environment"))
                {
                    return trigger.name.ToLower();
                }
            }
            
            // Default based on Y position
            if (transform.position.y > 20f)
                return "mountain";
            else if (transform.position.y < -5f)
                return "underwater";
            else
                return "temperate";
        }
        
        private IEnumerator AdaptToEnvironment(string environment)
        {
            Color[] targetPalette = GetEnvironmentalPalette(environment);
            Color[] startPalette = currentPalette ?? GetGeneticColorPalette();
            
            float duration = 3f / adaptationSpeed;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                Color[] blendedPalette = new Color[targetPalette.Length];
                for (int i = 0; i < blendedPalette.Length; i++)
                {
                    Color startColor = i < startPalette.Length ? startPalette[i] : Color.white;
                    blendedPalette[i] = Color.Lerp(startColor, targetPalette[i], t);
                }
                
                currentPalette = blendedPalette;
                ApplyPaletteToRenderers(blendedPalette);
                
                yield return null;
            }
            
            Debug.Log($"🌍 Adapted to environment: {environment}");
        }
        
        private IEnumerator MonitorEnvironmentForCamouflage()
        {
            while (enabled)
            {
                if (enableAdaptiveCamouflage)
                {
                    UpdateCamouflageAdaptation();
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        private void UpdateCamouflageAdaptation()
        {
            // Check if creature should camouflage
            float camouflageGene = 0f;
            
            if (creatureInstance?.CreatureData?.GeneticProfile != null)
            {
                var camouflageTraits = creatureInstance.CreatureData.GeneticProfile.Genes
                    .Where(g => g.traitName.ToLower().Contains("camouflage"))
                    .ToArray();
                
                if (camouflageTraits.Length > 0)
                {
                    camouflageGene = camouflageTraits.Max(g => g.value ?? 0f);
                }
            }
            
            if (camouflageGene > 0.3f)
            {
                ApplyCamouflageColoration(camouflageGene);
            }
        }
        
        private void ApplyCamouflageColoration(float camouflageStrength)
        {
            // Get environmental colors around creature
            Color[] envColors = SampleEnvironmentalColors();
            
            if (envColors.Length > 0)
            {
                // Blend creature colors toward environmental colors
                ApplyEnvironmentalBlending(envColors, camouflageStrength * camouflageEffectiveness);
            }
        }
        
        private Color[] SampleEnvironmentalColors()
        {
            List<Color> envColors = new List<Color>();
            
            // Raycast around creature to sample terrain colors
            Vector3[] directions = {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                Vector3.forward + Vector3.left, Vector3.forward + Vector3.right,
                Vector3.back + Vector3.left, Vector3.back + Vector3.right
            };
            
            foreach (var direction in directions)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, direction, out hit, 5f))
                {
                    Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
                    if (hitRenderer != null && hitRenderer.material != null)
                    {
                        envColors.Add(hitRenderer.material.color);
                    }
                }
            }
            
            return envColors.ToArray();
        }
        
        #endregion
        
        #region Color Management
        
        private Color[] GetGeneticColorPalette()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile == null)
            {
                return new Color[] { Color.gray, Color.white, Color.black };
            }
            
            var genetics = creatureInstance.CreatureData.GeneticProfile;
            List<Color> palette = new List<Color>();
            
            // Extract colors from genetic traits
            var colorGenes = genetics.Genes.Where(g => g.traitName.ToLower().Contains("color")).ToArray();
            
            foreach (var gene in colorGenes)
            {
                if (gene.value.HasValue)
                {
                    Color geneticColor = Color.HSVToRGB(gene.value.Value, 0.7f, 0.8f);
                    palette.Add(geneticColor);
                }
            }
            
            // Ensure we have at least 3 colors
            while (palette.Count < 3)
            {
                float hue = UnityEngine.Random.value;
                palette.Add(Color.HSVToRGB(hue, 0.6f, 0.7f));
            }
            
            return palette.ToArray();
        }
        
        private Color[] GetEnvironmentalPalette(string environment)
        {
            switch (environment.ToLower())
            {
                case "desert":
                    return new Color[] {
                        new Color(0.9f, 0.8f, 0.6f), // Sand
                        new Color(0.8f, 0.6f, 0.4f), // Darker sand
                        new Color(0.7f, 0.5f, 0.3f)  // Rock
                    };
                case "forest":
                    return new Color[] {
                        new Color(0.2f, 0.5f, 0.1f), // Dark green
                        new Color(0.4f, 0.3f, 0.1f), // Brown
                        new Color(0.1f, 0.3f, 0.1f)  // Very dark green
                    };
                case "arctic":
                    return new Color[] {
                        Color.white,
                        new Color(0.9f, 0.95f, 1f),  // Slight blue tint
                        new Color(0.8f, 0.9f, 0.95f) // More blue
                    };
                case "underwater":
                    return new Color[] {
                        new Color(0.1f, 0.3f, 0.6f), // Deep blue
                        new Color(0.2f, 0.5f, 0.8f), // Lighter blue
                        new Color(0.1f, 0.2f, 0.4f)  // Very deep blue
                    };
                case "mountain":
                    return new Color[] {
                        new Color(0.5f, 0.5f, 0.5f), // Gray stone
                        new Color(0.6f, 0.6f, 0.6f), // Lighter stone
                        new Color(0.3f, 0.3f, 0.3f)  // Dark stone
                    };
                default:
                    return new Color[] {
                        new Color(0.4f, 0.6f, 0.3f), // Grass green
                        new Color(0.5f, 0.4f, 0.2f), // Earth brown
                        new Color(0.3f, 0.5f, 0.2f)  // Forest green
                    };
            }
        }
        
        private void ApplyPatternToRenderers(Texture2D patternTexture)
        {
            foreach (var renderer in targetRenderers)
            {
                if (renderer == null) continue;
                
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        // Create material instance
                        materials[i] = new Material(materials[i]);
                        materials[i].mainTexture = patternTexture;
                    }
                }
                renderer.materials = materials;
            }
        }
        
        private void ApplyPaletteToRenderers(Color[] palette)
        {
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] == null) continue;
                
                Color colorToUse = palette[i % palette.Length];
                
                var materials = targetRenderers[i].materials;
                for (int m = 0; m < materials.Length; m++)
                {
                    if (materials[m] != null)
                    {
                        materials[m].color = colorToUse;
                    }
                }
            }
        }
        
        private void ApplyColorTintToRenderers(Color tint)
        {
            foreach (var renderer in targetRenderers)
            {
                if (renderer == null) continue;
                
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        Color currentColor = materials[i].color;
                        materials[i].color = currentColor * tint;
                    }
                }
            }
        }
        
        private void ApplyEnvironmentalBlending(Color[] envColors, float blendStrength)
        {
            if (envColors.Length == 0) return;
            
            Color avgEnvColor = Color.black;
            foreach (var color in envColors)
            {
                avgEnvColor += color;
            }
            avgEnvColor /= envColors.Length;
            
            foreach (var renderer in targetRenderers)
            {
                if (renderer == null) continue;
                
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        Color currentColor = materials[i].color;
                        materials[i].color = Color.Lerp(currentColor, avgEnvColor, blendStrength * 0.3f);
                    }
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private string GetPaletteHash(Color[] palette)
        {
            // PERFORMANCE OPTIMIZED: Use hash code directly instead of string concatenation
            int hash = 17; // Prime number for hash calculation
            foreach (var color in palette)
            {
                hash = hash * 31 + ((int)(color.r * 100)).GetHashCode();
                hash = hash * 31 + ((int)(color.g * 100)).GetHashCode();
                hash = hash * 31 + ((int)(color.b * 100)).GetHashCode();
            }
            return hash.ToString(); // Only one ToString() call
        }
        
        public string GetCurrentPattern()
        {
            return currentPattern;
        }
        
        public float GetPatternIntensity()
        {
            return currentComplexity;
        }
        
        public Dictionary<string, object> GetCustomPatternData()
        {
            return new Dictionary<string, object>
            {
                ["pattern"] = currentPattern,
                ["complexity"] = currentComplexity,
                ["paletteSize"] = currentPalette?.Length ?? 0,
                ["cacheSize"] = patternCache.Count
            };
        }
        
        public void AdaptToCurrentEnvironment()
        {
            string environment = DetectCurrentEnvironment();
            StartCoroutine(AdaptToEnvironment(environment));
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Force Pattern Regeneration")]
        public void ForcePatternRegeneration()
        {
            if (!string.IsNullOrEmpty(currentPattern))
            {
                GeneratePattern(currentPattern, currentComplexity);
            }
        }
        
        [ContextMenu("Clear Pattern Cache")]
        public void ClearPatternCache()
        {
            patternCache.Clear();
            materialCache.Clear();
            Debug.Log("Pattern cache cleared");
        }
        
        #endregion
    }
    
    #region Data Structures
    
    [System.Serializable]
    public class PatternDefinition
    {
        public string patternName;
        public PatternType type;
        public float complexityMultiplier = 1.0f;
        public Color[] defaultColors;
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }
    
    public enum PatternType
    {
        Solid,
        Stripes,
        Spots,
        Geometric,
        Organic,
        Fractal,
        Camouflage,
        Iridescent,
        Bioluminescent
    }
    
    #endregion
}