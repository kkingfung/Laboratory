using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Genetics.Core;
using Laboratory.Chimera.ECS.Components;

namespace Laboratory.Chimera.Genetics.Visualization
{
    /// <summary>
    /// Advanced genetic photography and visualization system.
    /// Captures creatures with genetic information overlays and creates shareable genetic portraits.
    /// </summary>
    public partial class GeneticPhotographySystem : SystemBase
    {
        private GeneticPhotographyConfig _config;
        private EntityQuery _photographableCreaturesQuery;
        private EntityQuery _activeCamerasQuery;
        private Camera _geneticCamera;
        private RenderTexture _geneticRenderTexture;

        // Photography state
        private bool _isInPhotographyMode = false;
        private float _photographyModeTimer = 0f;
        private List<GeneticPhoto> _capturedPhotos = new List<GeneticPhoto>();
        private Dictionary<int, GeneticVisualizationData> _visualizationCache = new Dictionary<int, GeneticVisualizationData>();

        // Event system
        public static event Action<GeneticPhoto> OnPhotoCapture;
        public static event Action<GeneticComparison> OnComparisonGenerated;

        protected override void OnCreate()
        {
            _config = Resources.Load<GeneticPhotographyConfig>("Configs/GeneticPhotographyConfig");
            if (_config == null)
            {
                UnityEngine.Debug.LogError("GeneticPhotographyConfig not found in Resources/Configs/");
                return;
            }

            _photographableCreaturesQuery = GetEntityQuery(
                ComponentType.ReadOnly<CreatureGeneticsComponent>(),
                ComponentType.ReadWrite<PhotographyData>()
            );

            _activeCamerasQuery = GetEntityQuery(
                ComponentType.ReadWrite<GeneticCameraData>()
            );

            InitializeGeneticCamera();
        }

        protected override void OnUpdate()
        {
            if (_config == null) return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Update photography mode
            if (_isInPhotographyMode)
            {
                UpdatePhotographyMode(deltaTime);
            }

            // Process genetic visualizations
            UpdateGeneticVisualizations(deltaTime);

            // Check for automatic capture opportunities
            CheckAutomaticCaptures();

            // Process photo analysis
            ProcessPhotoAnalysis(deltaTime);

            // Update camera systems
            UpdateCameraSystems(deltaTime);
        }

        private void InitializeGeneticCamera()
        {
            // Create dedicated camera for genetic photography
            var cameraGO = new GameObject("GeneticCamera");
            _geneticCamera = cameraGO.AddComponent<Camera>();
            _geneticCamera.enabled = false;

            // Configure camera for high-quality captures
            _geneticCamera.targetTexture = CreateGeneticRenderTexture();
            _geneticCamera.backgroundColor = _config.backgroundColors[0];
            _geneticCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private RenderTexture CreateGeneticRenderTexture()
        {
            _geneticRenderTexture = new RenderTexture(_config.photoResolution.x, _config.photoResolution.y, 24);
            _geneticRenderTexture.antiAliasing = _config.antiAliasingSamples;
            _geneticRenderTexture.filterMode = FilterMode.Bilinear;
            return _geneticRenderTexture;
        }

        public void EnterPhotographyMode(int playerId)
        {
            if (_isInPhotographyMode) return;

            _isInPhotographyMode = true;
            _photographyModeTimer = 0f;

            // Enable genetic overlays for all creatures
            EnableGeneticOverlays(true);

            // Setup UI elements
            ShowPhotographyUI(true);

            UnityEngine.Debug.Log("Entered Genetic Photography Mode");
        }

        public void ExitPhotographyMode()
        {
            if (!_isInPhotographyMode) return;

            _isInPhotographyMode = false;

            // Disable genetic overlays
            EnableGeneticOverlays(false);

            // Hide UI elements
            ShowPhotographyUI(false);

            UnityEngine.Debug.Log("Exited Genetic Photography Mode");
        }

        private void UpdatePhotographyMode(float deltaTime)
        {
            _photographyModeTimer += deltaTime;

            // Auto-exit after timeout
            if (_photographyModeTimer > _config.maxPhotographySessionDuration)
            {
                ExitPhotographyMode();
                return;
            }

            // Update genetic visualizations for all visible creatures
            UpdatePhotographyVisualizations();

            // Handle photography controls
            HandlePhotographyInput();
        }

        private void UpdatePhotographyVisualizations()
        {
            Entities.WithAll<VisualGeneticData, PhotographyData>().WithoutBurst().ForEach((Entity entity, ref VisualGeneticData genetics, ref PhotographyData photoData) =>
            {
                // Update visualization data
                var visualData = GenerateVisualizationData(genetics, photoData);
                _visualizationCache[photoData.creatureId] = visualData;

                // Update overlay rendering
                UpdateGeneticOverlay(entity, visualData);

                // Check for photo opportunities
                CheckPhotoOpportunity(entity, genetics, photoData);
            }).Run();
        }

        private GeneticVisualizationData GenerateVisualizationData(VisualGeneticData genetics, PhotographyData photoData)
        {
            var visualData = new GeneticVisualizationData
            {
                creatureId = photoData.creatureId,
                dominantTraits = GetDominantTraits(genetics),
                rarityLevel = CalculateOverallRarity(genetics),
                geneticComplexity = CalculateGeneticComplexity(genetics),
                uniquenessScore = CalculateUniquenessScore(genetics),
                colorProfile = GenerateGeneticColorProfile(genetics),
                traitDistribution = CalculateTraitDistribution(genetics),
                visualElements = GenerateVisualElements(genetics)
            };

            return visualData;
        }

        private void UpdateGeneticOverlay(Entity entity, GeneticVisualizationData visualData)
        {
            if (!_config.enableGeneticOverlays) return;

            // This would typically update shader properties or UI elements
            // For now, we'll store the data for rendering
            UpdateOverlayShaderProperties(entity, visualData);
            UpdateOverlayUIElements(entity, visualData);
        }

        private void CheckPhotoOpportunity(Entity entity, VisualGeneticData genetics, PhotographyData photoData)
        {
            // Check for automatic photo opportunities
            var opportunity = EvaluatePhotoOpportunity(genetics, photoData);

            if (opportunity.quality > _config.autoCapturethreshold)
            {
                photoData.hasPhotoOpportunity = true;
                photoData.opportunityQuality = opportunity.quality;
                photoData.opportunityType = opportunity.type;

                // Highlight creature for player
                HighlightCreatureForPhotography(entity, opportunity);
            }
            else
            {
                photoData.hasPhotoOpportunity = false;
            }
        }

        public GeneticPhoto CapturePhoto(Entity creatureEntity, PhotoCaptureSettings settings)
        {
            if (!SystemAPI.HasComponent<VisualGeneticData>(creatureEntity) || !SystemAPI.HasComponent<PhotographyData>(creatureEntity))
            {
                UnityEngine.Debug.LogError("Cannot capture photo: creature missing required components");
                return null;
            }

            var genetics = SystemAPI.GetComponent<VisualGeneticData>(creatureEntity);
            var photoData = SystemAPI.GetComponent<PhotographyData>(creatureEntity);

            // Setup camera for capture
            SetupCameraForCapture(creatureEntity, settings);

            // Generate genetic overlay
            var overlayData = GeneratePhotoOverlay(genetics, settings);

            // Capture the photo
            var photo = PerformCapture(creatureEntity, genetics, photoData, settings, overlayData);

            // Post-process the photo
            PostProcessPhoto(photo, settings);

            // Update statistics
            UpdatePhotographyStatistics(photo);

            // Check for achievements
            CheckPhotographyAchievements(photo);

            // Trigger events
            OnPhotoCapture?.Invoke(photo);

            _capturedPhotos.Add(photo);
            return photo;
        }

        private void SetupCameraForCapture(Entity creatureEntity, PhotoCaptureSettings settings)
        {
            // Position camera for optimal view
            var creaturePosition = SystemAPI.GetComponent<Unity.Transforms.LocalTransform>(creatureEntity).Position;
            var cameraPosition = CalculateOptimalCameraPosition(creaturePosition, settings);

            _geneticCamera.transform.position = cameraPosition;
            _geneticCamera.transform.LookAt(creaturePosition);

            // Configure camera settings
            _geneticCamera.fieldOfView = settings.fieldOfView;
            _geneticCamera.backgroundColor = settings.backgroundColor;

            // Apply post-processing effects
            ApplyCameraEffects(settings);
        }

        private Vector3 CalculateOptimalCameraPosition(Vector3 creaturePosition, PhotoCaptureSettings settings)
        {
            var offset = new Vector3(settings.cameraDistance, settings.cameraHeight, 0f);

            // Apply angle rotation
            var angleRad = settings.cameraAngle * Mathf.Deg2Rad;
            var rotatedOffset = new Vector3(
                offset.x * Mathf.Cos(angleRad) - offset.z * Mathf.Sin(angleRad),
                offset.y,
                offset.x * Mathf.Sin(angleRad) + offset.z * Mathf.Cos(angleRad)
            );

            return creaturePosition + rotatedOffset;
        }

        private GeneticOverlayData GeneratePhotoOverlay(VisualGeneticData genetics, PhotoCaptureSettings settings)
        {
            var overlayData = new GeneticOverlayData
            {
                showTraitLabels = settings.showTraitLabels,
                showRarityIndicators = settings.showRarityIndicators,
                showGeneticConnections = settings.showGeneticConnections,
                overlayOpacity = settings.overlayOpacity,
                elements = new List<OverlayElement>()
            };

            if (settings.showTraitLabels)
            {
                AddTraitLabelElements(overlayData, genetics);
            }

            if (settings.showRarityIndicators)
            {
                AddRarityIndicatorElements(overlayData, genetics);
            }

            if (settings.showGeneticConnections)
            {
                AddGeneticConnectionElements(overlayData, genetics);
            }

            return overlayData;
        }

        private GeneticPhoto PerformCapture(Entity creatureEntity, VisualGeneticData genetics, PhotographyData photoData, PhotoCaptureSettings settings, GeneticOverlayData overlayData)
        {
            // Render to texture
            _geneticCamera.Render();

            // Read pixels from render texture
            RenderTexture.active = _geneticRenderTexture;
            var texture = new Texture2D(_config.photoResolution.x, _config.photoResolution.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, _config.photoResolution.x, _config.photoResolution.y), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            // Create photo object
            var photo = new GeneticPhoto
            {
                id = GeneratePhotoId(),
                creatureId = photoData.creatureId,
                texture = texture,
                genetics = genetics,
                captureTime = DateTime.Now,
                settings = settings,
                overlayData = overlayData,
                metadata = GeneratePhotoMetadata(genetics, settings),
                quality = CalculatePhotoQuality(genetics, settings),
                tags = GeneratePhotoTags(genetics),
                location = GetCaptureLocation(),
                photographer = GetCurrentPlayer()
            };

            return photo;
        }

        private void PostProcessPhoto(GeneticPhoto photo, PhotoCaptureSettings settings)
        {
            // Apply filters
            if (settings.enableFilters)
            {
                ApplyPhotoFilters(photo, settings.filterSettings);
            }

            // Add watermark
            if (settings.addWatermark)
            {
                AddWatermark(photo);
            }

            // Compress if needed
            if (settings.compressPhoto)
            {
                CompressPhoto(photo, settings.compressionQuality);
            }
        }

        private void UpdateGeneticVisualizations(float deltaTime)
        {
            if (!_isInPhotographyMode) return;

            // Update visualization effects for all creatures
            Entities.WithAll<PhotographyData>().ForEach((Entity entity, ref PhotographyData photoData) =>
            {
                // Update animation timers
                photoData.visualizationTimer += deltaTime;

                // Update genetic aura effects
                if (_config.enableGeneticAuras)
                {
                    UpdateGeneticAura(entity, photoData, deltaTime);
                }

                // Update trait highlighting
                if (_config.enableTraitHighlighting)
                {
                    UpdateTraitHighlighting(entity, photoData, deltaTime);
                }
            }).Run();
        }

        private void CheckAutomaticCaptures()
        {
            if (!_config.enableAutomaticCapture) return;

            Entities.WithAll<VisualGeneticData, PhotographyData>().WithoutBurst().ForEach((Entity entity, ref VisualGeneticData genetics, ref PhotographyData photoData) =>
            {
                // Check for rare genetic combinations
                if (IsRareGeneticCombination(genetics) && !photoData.hasAutoCapture)
                {
                    var settings = CreateAutoCaptureSettings();
                    var photo = CapturePhoto(entity, settings);

                    if (photo != null)
                    {
                        photo.isAutoCapture = true;
                        photoData.hasAutoCapture = true;

                        UnityEngine.Debug.Log($"Auto-captured rare genetic combination: {photo.id}");
                    }
                }

                // Check for genetic milestones
                CheckGeneticMilestones(entity, genetics, ref photoData);
            }).Run();
        }

        private void ProcessPhotoAnalysis(float deltaTime)
        {
            // Process photos for genetic analysis
            foreach (var photo in _capturedPhotos.Where(p => !p.isAnalyzed))
            {
                AnalyzePhoto(photo);
                photo.isAnalyzed = true;
            }

            // Generate comparisons
            GeneratePhotoComparisons();

            // Update collections
            UpdatePhotoCollections();
        }

        private void AnalyzePhoto(GeneticPhoto photo)
        {
            // Genetic composition analysis
            photo.analysis.traitFrequency = AnalyzeTraitFrequency(photo.genetics);
            photo.analysis.rarityBreakdown = AnalyzeRarityBreakdown(photo.genetics);
            photo.analysis.uniquenessFactors = IdentifyUniquenessfactors(photo.genetics);
            photo.analysis.geneticHealth = CalculateGeneticHealth(photo.genetics);

            // Visual quality analysis
            photo.analysis.compositionScore = AnalyzeComposition(photo);
            photo.analysis.lightingQuality = AnalyzeLighting(photo);
            photo.analysis.clarityScore = AnalyzeClarity(photo);

            // Generate insights
            photo.analysis.insights = GenerateGeneticInsights(photo.genetics);
            photo.analysis.recommendations = GeneratePhotographyRecommendations(photo);
        }

        private void GeneratePhotoComparisons()
        {
            if (_capturedPhotos.Count < 2) return;

            // Compare recent photos with existing collection
            var recentPhotos = _capturedPhotos.Where(p => (DateTime.Now - p.captureTime).TotalHours < 24).ToList();

            foreach (var photo in recentPhotos)
            {
                var similarPhotos = FindSimilarPhotos(photo);

                if (similarPhotos.Any())
                {
                    var comparison = new GeneticComparison
                    {
                        primaryPhoto = photo,
                        comparisonPhotos = similarPhotos,
                        similarityScore = CalculateSimilarityScore(photo, similarPhotos),
                        differences = IdentifyGeneticDifferences(photo, similarPhotos),
                        insights = GenerateComparisonInsights(photo, similarPhotos)
                    };

                    OnComparisonGenerated?.Invoke(comparison);
                }
            }
        }

        private void UpdateCameraSystems(float deltaTime)
        {
            Entities.WithAll<GeneticCameraData>().ForEach((Entity entity, ref GeneticCameraData cameraData) =>
            {
                // Update camera state
                cameraData.operationTimer += deltaTime;

                // Process active photo sessions
                if (cameraData.isActive)
                {
                    ProcessCameraSession(entity, ref cameraData, deltaTime);
                }

                // Handle camera maintenance
                ProcessCameraMaintenance(ref cameraData, deltaTime);
            }).Run();
        }

        #region Helper Methods

        private void EnableGeneticOverlays(bool enable)
        {
            // Enable/disable genetic visualization overlays
            Entities.WithAll<PhotographyData>().ForEach((Entity entity, ref PhotographyData photoData) =>
            {
                photoData.showGeneticOverlay = enable;
            }).Run();
        }

        private void ShowPhotographyUI(bool show)
        {
            // Show/hide photography mode UI elements
            // This would typically interact with UI systems
        }

        private void HandlePhotographyInput()
        {
            // Handle input for photography mode
            // This would typically check for input events
        }

        private string[] GetDominantTraits(VisualGeneticData genetics)
        {
            var traits = new List<string>();

            // Get the top 3 traits based on their values
            var traitValues = new[]
            {
                new { Name = "Strength", Value = genetics.Strength },
                new { Name = "Vitality", Value = genetics.Vitality },
                new { Name = "Agility", Value = genetics.Agility },
                new { Name = "Intelligence", Value = genetics.Intelligence },
                new { Name = "Adaptability", Value = genetics.Adaptability },
                new { Name = "Social", Value = genetics.Social }
            };

            return traitValues.OrderByDescending(t => t.Value).Take(3).Select(t => t.Name).ToArray();
        }

        private float CalculateOverallRarity(VisualGeneticData genetics)
        {
            // Use the built-in rarity calculation
            return VisualGeneticUtility.GetRarityScore(genetics);
        }

        private float CalculateGeneticComplexity(VisualGeneticData genetics)
        {
            // Calculate complexity based on trait diversity and special markers
            int traitCount = 6; // Fixed number of traits (Strength, Vitality, etc.)
            int specialMarkerCount = genetics.SpecialMarkers.CountFlags();
            int mutationCount = genetics.MutationCount;

            return (traitCount + specialMarkerCount + mutationCount) * 0.1f;
        }

        private float CalculateUniquenessScore(VisualGeneticData genetics)
        {
            // Calculate how unique this genetic combination is
            return UnityEngine.Random.Range(0.3f, 1f); // Simplified for example
        }

        private Color[] GenerateGeneticColorProfile(VisualGeneticData genetics)
        {
            var colors = new List<Color>();
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Adaptability" };

            foreach (var trait in traitNames)
            {
                colors.Add(_config.GetTraitColor(trait));
            }
            return colors.ToArray();
        }

        private float[] CalculateTraitDistribution(VisualGeneticData genetics)
        {
            var distribution = new float[6]; // Fixed number of traits
            var traitValues = new[] { genetics.Strength, genetics.Vitality, genetics.Agility,
                                      genetics.Intelligence, genetics.Adaptability, genetics.Social };

            for (int i = 0; i < distribution.Length; i++)
            {
                distribution[i] = traitValues[i] / 100f; // Normalize to 0-1 range
            }
            return distribution;
        }

        private VisualElement[] GenerateVisualElements(VisualGeneticData genetics)
        {
            var elements = new List<VisualElement>();
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Adaptability", "Social" };
            var traitValues = new[] { genetics.Strength, genetics.Vitality, genetics.Agility,
                                      genetics.Intelligence, genetics.Adaptability, genetics.Social };

            // Add trait visualization elements
            for (int i = 0; i < traitNames.Length; i++)
            {
                elements.Add(new VisualElement
                {
                    type = VisualElementType.TraitIndicator,
                    position = UnityEngine.Random.insideUnitCircle,
                    color = _config.GetTraitColor(traitNames[i]),
                    size = traitValues[i] / 100f // Use trait value as size (normalized)
                });
            }

            return elements.ToArray();
        }

        private void UpdateOverlayShaderProperties(Entity entity, GeneticVisualizationData visualData)
        {
            // Update shader properties for genetic overlay rendering
        }

        private void UpdateOverlayUIElements(Entity entity, GeneticVisualizationData visualData)
        {
            // Update UI elements for genetic information display
        }

        private PhotoOpportunity EvaluatePhotoOpportunity(VisualGeneticData genetics, PhotographyData photoData)
        {
            float quality = CalculateOverallRarity(genetics) * 0.4f +
                           CalculateGeneticComplexity(genetics) * 0.3f +
                           CalculateUniquenessScore(genetics) * 0.3f;

            return new PhotoOpportunity
            {
                quality = quality,
                type = DetermineOpportunityType(genetics),
                description = GenerateOpportunityDescription(genetics)
            };
        }

        private void HighlightCreatureForPhotography(Entity entity, PhotoOpportunity opportunity)
        {
            // Add visual highlighting to indicate photo opportunity
        }

        private void ApplyCameraEffects(PhotoCaptureSettings settings)
        {
            // Apply post-processing effects to camera
        }

        private void AddTraitLabelElements(GeneticOverlayData overlayData, VisualGeneticData genetics)
        {
            var dominantTraits = GetDominantTraits(genetics).Take(5);
            foreach (var trait in dominantTraits)
            {
                overlayData.elements.Add(new OverlayElement
                {
                    type = OverlayElementType.TraitLabel,
                    text = trait,
                    position = UnityEngine.Random.insideUnitCircle,
                    color = _config.GetTraitColor(trait)
                });
            }
        }

        private void AddRarityIndicatorElements(GeneticOverlayData overlayData, VisualGeneticData genetics)
        {
            var rarity = CalculateOverallRarity(genetics);
            overlayData.elements.Add(new OverlayElement
            {
                type = OverlayElementType.RarityIndicator,
                value = rarity,
                position = Vector2.zero,
                color = _config.GetRarityColor(rarity)
            });
        }

        private void AddGeneticConnectionElements(GeneticOverlayData overlayData, VisualGeneticData genetics)
        {
            // Add visual connections between related traits (5 connections for 6 traits)
            for (int i = 0; i < 5; i++)
            {
                overlayData.elements.Add(new OverlayElement
                {
                    type = OverlayElementType.Connection,
                    startPosition = UnityEngine.Random.insideUnitCircle,
                    endPosition = UnityEngine.Random.insideUnitCircle,
                    color = Color.white
                });
            }
        }

        private string GeneratePhotoId()
        {
            return $"PHOTO_{DateTime.Now:yyyyMMddHHmmss}_{UnityEngine.Random.Range(1000, 9999)}";
        }

        private PhotoMetadata GeneratePhotoMetadata(VisualGeneticData genetics, PhotoCaptureSettings settings)
        {
            return new PhotoMetadata
            {
                resolution = _config.photoResolution,
                captureSettings = settings,
                geneticHash = CalculateGeneticHash(genetics),
                environmentalFactors = GetEnvironmentalFactors(),
                cameraSettings = GetCameraSettings()
            };
        }

        private float CalculatePhotoQuality(VisualGeneticData genetics, PhotoCaptureSettings settings)
        {
            float geneticQuality = CalculateOverallRarity(genetics) * 0.4f;
            float technicalQuality = EvaluateTechnicalQuality(settings) * 0.6f;
            return geneticQuality + technicalQuality;
        }

        private string[] GeneratePhotoTags(VisualGeneticData genetics)
        {
            var tags = new List<string>();
            tags.AddRange(GetDominantTraits(genetics).Take(3));
            tags.Add($"Rarity{Mathf.RoundToInt(CalculateOverallRarity(genetics) * 10)}");
            return tags.ToArray();
        }

        private string GetCaptureLocation()
        {
            return "Laboratory_Section_A"; // Simplified for example
        }

        private string GetCurrentPlayer()
        {
            return "Player_001"; // Simplified for example
        }

        private void ApplyPhotoFilters(GeneticPhoto photo, FilterSettings filterSettings)
        {
            // Apply post-processing filters to photo texture
        }

        private void AddWatermark(GeneticPhoto photo)
        {
            // Add watermark to photo texture
        }

        private void CompressPhoto(GeneticPhoto photo, float quality)
        {
            // Compress photo texture for storage
        }

        private void UpdateGeneticAura(Entity entity, PhotographyData photoData, float deltaTime)
        {
            // Update genetic aura visual effects
            photoData.auraIntensity = Mathf.Sin(photoData.visualizationTimer * 2f) * 0.5f + 0.5f;
        }

        private void UpdateTraitHighlighting(Entity entity, PhotographyData photoData, float deltaTime)
        {
            // Update trait highlighting effects
            photoData.highlightIntensity = Mathf.PingPong(photoData.visualizationTimer, 1f);
        }

        private bool IsRareGeneticCombination(VisualGeneticData genetics)
        {
            return CalculateOverallRarity(genetics) > _config.rareGeneticThreshold;
        }

        private PhotoCaptureSettings CreateAutoCaptureSettings()
        {
            return new PhotoCaptureSettings
            {
                fieldOfView = 60f,
                cameraDistance = 5f,
                cameraHeight = 2f,
                cameraAngle = 0f,
                backgroundColor = _config.backgroundColors[0],
                showTraitLabels = true,
                showRarityIndicators = true,
                showGeneticConnections = false,
                overlayOpacity = 0.7f,
                enableFilters = false,
                addWatermark = true,
                compressPhoto = true,
                compressionQuality = 0.8f
            };
        }

        private void CheckGeneticMilestones(Entity entity, VisualGeneticData genetics, ref PhotographyData photoData)
        {
            // Check for genetic milestone achievements
        }

        private void UpdatePhotographyStatistics(GeneticPhoto photo)
        {
            // Update player photography statistics
        }

        private void CheckPhotographyAchievements(GeneticPhoto photo)
        {
            // Check for photography-related achievements
        }

        private void UpdatePhotoCollections()
        {
            // Update photo collections and galleries
        }

        private Dictionary<string, float> AnalyzeTraitFrequency(VisualGeneticData genetics)
        {
            var frequency = new Dictionary<string, float>();
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Adaptability", "Social" };
            var traitValues = new[] { genetics.Strength, genetics.Vitality, genetics.Agility,
                                      genetics.Intelligence, genetics.Adaptability, genetics.Social };

            for (int i = 0; i < traitNames.Length; i++)
            {
                frequency[traitNames[i]] = traitValues[i] / 100f;
            }
            return frequency;
        }

        private Dictionary<string, float> AnalyzeRarityBreakdown(VisualGeneticData genetics)
        {
            var breakdown = new Dictionary<string, float>();
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Adaptability", "Social" };

            foreach (var trait in traitNames)
            {
                breakdown[trait] = _config.GetTraitRarity(trait);
            }
            return breakdown;
        }

        private string[] IdentifyUniquenessfactors(VisualGeneticData genetics)
        {
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Adaptability", "Social" };
            return traitNames.Where(t => _config.GetTraitRarity(t) > 0.7f).ToArray();
        }

        private float CalculateGeneticHealth(VisualGeneticData genetics)
        {
            return UnityEngine.Random.Range(0.5f, 1f); // Simplified for example
        }

        private float AnalyzeComposition(GeneticPhoto photo)
        {
            return UnityEngine.Random.Range(0.6f, 1f); // Simplified for example
        }

        private float AnalyzeLighting(GeneticPhoto photo)
        {
            return UnityEngine.Random.Range(0.5f, 1f); // Simplified for example
        }

        private float AnalyzeClarity(GeneticPhoto photo)
        {
            return UnityEngine.Random.Range(0.7f, 1f); // Simplified for example
        }

        private string[] GenerateGeneticInsights(VisualGeneticData genetics)
        {
            return new[] { "Rare trait combination detected", "High genetic diversity", "Excellent breeding potential" };
        }

        private string[] GeneratePhotographyRecommendations(GeneticPhoto photo)
        {
            return new[] { "Try different lighting angle", "Consider closer composition", "Highlight rare traits" };
        }

        private GeneticPhoto[] FindSimilarPhotos(GeneticPhoto photo)
        {
            return _capturedPhotos.Where(p => p.id != photo.id && CalculatePhotoSimilarity(p, photo) > 0.7f).ToArray();
        }

        private float CalculateSimilarityScore(GeneticPhoto photo, GeneticPhoto[] similarPhotos)
        {
            return similarPhotos.Average(p => CalculatePhotoSimilarity(photo, p));
        }

        private float CalculatePhotoSimilarity(GeneticPhoto photo1, GeneticPhoto photo2)
        {
            return CalculateGeneticSimilarity(photo1.genetics, photo2.genetics);
        }

        private float CalculateGeneticSimilarity(VisualGeneticData genetics1, VisualGeneticData genetics2)
        {
            // Calculate similarity based on trait value differences
            var traitDiffs = new[]
            {
                Mathf.Abs(genetics1.Strength - genetics2.Strength),
                Mathf.Abs(genetics1.Vitality - genetics2.Vitality),
                Mathf.Abs(genetics1.Agility - genetics2.Agility),
                Mathf.Abs(genetics1.Intelligence - genetics2.Intelligence),
                Mathf.Abs(genetics1.Adaptability - genetics2.Adaptability),
                Mathf.Abs(genetics1.Social - genetics2.Social)
            };

            // Calculate average difference and convert to similarity (0-1 range)
            float avgDiff = (float)traitDiffs.Average();
            float similarity = 1f - (avgDiff / 100f); // Normalize by max trait value (100)
            return Mathf.Clamp01(similarity);
        }

        private string[] IdentifyGeneticDifferences(GeneticPhoto photo, GeneticPhoto[] similarPhotos)
        {
            var differences = new List<string>();
            // Analyze genetic differences between photos
            return differences.ToArray();
        }

        private string[] GenerateComparisonInsights(GeneticPhoto photo, GeneticPhoto[] similarPhotos)
        {
            return new[] { "Similar genetic lineage detected", "Trait variation analysis complete", "Breeding compatibility confirmed" };
        }

        private void ProcessCameraSession(Entity entity, ref GeneticCameraData cameraData, float deltaTime)
        {
            // Process active camera session
        }

        private void ProcessCameraMaintenance(ref GeneticCameraData cameraData, float deltaTime)
        {
            // Handle camera maintenance tasks
        }

        private PhotoOpportunityType DetermineOpportunityType(VisualGeneticData genetics)
        {
            var rarity = CalculateOverallRarity(genetics);
            if (rarity > 0.9f) return PhotoOpportunityType.Legendary;
            if (rarity > 0.7f) return PhotoOpportunityType.Rare;
            if (rarity > 0.5f) return PhotoOpportunityType.Uncommon;
            return PhotoOpportunityType.Common;
        }

        private string GenerateOpportunityDescription(VisualGeneticData genetics)
        {
            var dominantTraits = GetDominantTraits(genetics);
            var dominantTrait = dominantTraits.FirstOrDefault() ?? "Unknown";
            return $"Excellent opportunity to capture {dominantTrait} traits in optimal lighting";
        }

        private string CalculateGeneticHash(VisualGeneticData genetics)
        {
            // Create hash from all trait values and special markers
            var combined = $"{genetics.Strength}{genetics.Vitality}{genetics.Agility}" +
                          $"{genetics.Intelligence}{genetics.Adaptability}{genetics.Social}" +
                          $"{genetics.SpecialMarkers}{genetics.MutationCount}";
            return combined.GetHashCode().ToString("X8");
        }

        private EnvironmentalFactors GetEnvironmentalFactors()
        {
            return new EnvironmentalFactors
            {
                lighting = UnityEngine.Random.Range(0.3f, 1f),
                temperature = UnityEngine.Random.Range(15f, 30f),
                humidity = UnityEngine.Random.Range(0.3f, 0.8f),
                time = DateTime.Now.TimeOfDay
            };
        }

        private CameraSettings GetCameraSettings()
        {
            return new CameraSettings
            {
                aperture = 2.8f,
                shutterSpeed = 1f / 60f,
                iso = 100,
                focalLength = 50f
            };
        }

        private float EvaluateTechnicalQuality(PhotoCaptureSettings settings)
        {
            return UnityEngine.Random.Range(0.6f, 1f); // Simplified for example
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Component for creatures that can be photographed
    /// </summary>
    [Serializable]
    public struct PhotographyData : IComponentData
    {
        public int creatureId;
        public bool showGeneticOverlay;
        public bool hasPhotoOpportunity;
        public float opportunityQuality;
        public PhotoOpportunityType opportunityType;
        public bool hasAutoCapture;
        public float visualizationTimer;
        public float auraIntensity;
        public float highlightIntensity;
        public int timesPhotographed;
        public float lastPhotoTime;
    }

    /// <summary>
    /// Component for genetic cameras
    /// </summary>
    [Serializable]
    public struct GeneticCameraData : IComponentData
    {
        public int cameraId;
        public bool isActive;
        public float operationTimer;
        public CameraMode mode;
        public float batteryLevel;
        public int photosStored;
        public float lastMaintenanceTime;
    }

    /// <summary>
    /// Complete genetic photo with all metadata
    /// </summary>
    [Serializable]
    public class GeneticPhoto
    {
        public string id;
        public int creatureId;
        public Texture2D texture;
        public VisualGeneticData genetics;
        public DateTime captureTime;
        public PhotoCaptureSettings settings;
        public GeneticOverlayData overlayData;
        public PhotoMetadata metadata;
        public float quality;
        public string[] tags;
        public string location;
        public string photographer;
        public bool isAutoCapture;
        public bool isAnalyzed;
        public PhotoAnalysis analysis = new PhotoAnalysis();
    }

    /// <summary>
    /// Settings for photo capture
    /// </summary>
    [Serializable]
    public struct PhotoCaptureSettings
    {
        public float fieldOfView;
        public float cameraDistance;
        public float cameraHeight;
        public float cameraAngle;
        public Color backgroundColor;
        public bool showTraitLabels;
        public bool showRarityIndicators;
        public bool showGeneticConnections;
        public float overlayOpacity;
        public bool enableFilters;
        public FilterSettings filterSettings;
        public bool addWatermark;
        public bool compressPhoto;
        public float compressionQuality;
    }

    /// <summary>
    /// Genetic visualization data for overlays
    /// </summary>
    [Serializable]
    public struct GeneticVisualizationData
    {
        public int creatureId;
        public string[] dominantTraits;
        public float rarityLevel;
        public float geneticComplexity;
        public float uniquenessScore;
        public Color[] colorProfile;
        public float[] traitDistribution;
        public VisualElement[] visualElements;
    }

    /// <summary>
    /// Photo opportunity data
    /// </summary>
    [Serializable]
    public struct PhotoOpportunity
    {
        public float quality;
        public PhotoOpportunityType type;
        public string description;
    }

    /// <summary>
    /// Overlay data for genetic information
    /// </summary>
    [Serializable]
    public class GeneticOverlayData
    {
        public bool showTraitLabels;
        public bool showRarityIndicators;
        public bool showGeneticConnections;
        public float overlayOpacity;
        public List<OverlayElement> elements = new List<OverlayElement>();
    }

    /// <summary>
    /// Individual overlay element
    /// </summary>
    [Serializable]
    public struct OverlayElement
    {
        public OverlayElementType type;
        public string text;
        public float value;
        public Vector2 position;
        public Vector2 startPosition;
        public Vector2 endPosition;
        public Color color;
        public float size;
    }

    /// <summary>
    /// Visual element for genetic visualization
    /// </summary>
    [Serializable]
    public struct VisualElement
    {
        public VisualElementType type;
        public Vector2 position;
        public Color color;
        public float size;
        public float intensity;
        public bool isAnimated;
    }

    /// <summary>
    /// Photo metadata
    /// </summary>
    [Serializable]
    public struct PhotoMetadata
    {
        public Vector2Int resolution;
        public PhotoCaptureSettings captureSettings;
        public string geneticHash;
        public EnvironmentalFactors environmentalFactors;
        public CameraSettings cameraSettings;
    }

    /// <summary>
    /// Environmental factors during capture
    /// </summary>
    [Serializable]
    public struct EnvironmentalFactors
    {
        public float lighting;
        public float temperature;
        public float humidity;
        public TimeSpan time;
    }

    /// <summary>
    /// Camera technical settings
    /// </summary>
    [Serializable]
    public struct CameraSettings
    {
        public float aperture;
        public float shutterSpeed;
        public int iso;
        public float focalLength;
    }

    /// <summary>
    /// Filter settings for post-processing
    /// </summary>
    [Serializable]
    public struct FilterSettings
    {
        public float brightness;
        public float contrast;
        public float saturation;
        public Color colorTint;
        public bool enableBloom;
        public float bloomIntensity;
    }

    /// <summary>
    /// Photo analysis results
    /// </summary>
    [Serializable]
    public class PhotoAnalysis
    {
        public Dictionary<string, float> traitFrequency = new Dictionary<string, float>();
        public Dictionary<string, float> rarityBreakdown = new Dictionary<string, float>();
        public string[] uniquenessFactors = new string[0];
        public float geneticHealth;
        public float compositionScore;
        public float lightingQuality;
        public float clarityScore;
        public string[] insights = new string[0];
        public string[] recommendations = new string[0];
    }

    /// <summary>
    /// Photo collection data
    /// </summary>
    [Serializable]
    public class GeneticPhotoCollection
    {
        public string id;
        public string name;
        public string description;
        public GeneticPhoto[] photos;
        public string theme;
        public DateTime createdAt;
        public bool isComplete;
        public float completionPercentage;
    }

    /// <summary>
    /// Photo comparison data
    /// </summary>
    [Serializable]
    public class GeneticComparison
    {
        public GeneticPhoto primaryPhoto;
        public GeneticPhoto[] comparisonPhotos;
        public float similarityScore;
        public string[] differences;
        public string[] insights;
    }

    /// <summary>
    /// Photography achievement data
    /// </summary>
    [Serializable]
    public struct PhotographyAchievement
    {
        public string id;
        public string name;
        public string description;
        public PhotoAchievementType type;
        public float progress;
        public bool isUnlocked;
        public DateTime unlockedAt;
    }

    #endregion

    #region Enums

    public enum PhotoOpportunityType
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum CameraMode
    {
        Standard,
        Genetic,
        Macro,
        Behavioral,
        Artistic
    }

    public enum OverlayElementType
    {
        TraitLabel,
        RarityIndicator,
        Connection,
        GeneticMarker,
        HealthIndicator
    }

    public enum VisualElementType
    {
        TraitIndicator,
        RarityAura,
        GeneticConnection,
        HealthMarker,
        UniquePattern
    }

    public enum PhotoAchievementType
    {
        FirstPhoto,
        RareCapture,
        CollectionComplete,
        TechnicalExcellence,
        ArtisticVision
    }

    #endregion
}