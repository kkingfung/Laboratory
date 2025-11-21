using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Genetics.Visualization;

namespace Laboratory.Chimera.ECS
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
            foreach (var (genetics, photoData, entity) in SystemAPI.Query<RefRO<CreatureGeneticsComponent>, RefRW<PhotographyData>>().WithEntityAccess())
            {
                // Update visualization data
                var visualData = GenerateVisualizationData(genetics.ValueRO, photoData.ValueRO);
                _visualizationCache[photoData.ValueRO.creatureId] = visualData;

                // Update overlay rendering
                UpdateGeneticOverlay(entity, visualData);

                // Check for photo opportunities
                CheckPhotoOpportunity(entity, genetics.ValueRO, photoData.ValueRW);
            }
        }

        private GeneticVisualizationData GenerateVisualizationData(in CreatureGeneticsComponent genetics, PhotographyData photoData)
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

        private void CheckPhotoOpportunity(Entity entity, in CreatureGeneticsComponent genetics, PhotographyData photoData)
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
            if (!SystemAPI.HasComponent<CreatureGeneticsComponent>(creatureEntity) || !SystemAPI.HasComponent<PhotographyData>(creatureEntity))
            {
                UnityEngine.Debug.LogError("Cannot capture photo: creature missing required components");
                return null;
            }

            var genetics = SystemAPI.GetComponent<CreatureGeneticsComponent>(creatureEntity);
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

        private GeneticOverlayData GeneratePhotoOverlay(in CreatureGeneticsComponent genetics, PhotoCaptureSettings settings)
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

        private GeneticPhoto PerformCapture(Entity creatureEntity, in CreatureGeneticsComponent genetics, PhotographyData photoData, PhotoCaptureSettings settings, GeneticOverlayData overlayData)
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
            foreach (var (photoData, entity) in SystemAPI.Query<RefRW<PhotographyData>>().WithEntityAccess())
            {
                // Update animation timers
                photoData.ValueRW.visualizationTimer += deltaTime;

                // Update genetic aura effects
                if (_config.enableGeneticAuras)
                {
                    UpdateGeneticAura(entity, photoData.ValueRO, deltaTime);
                }

                // Update trait highlighting
                if (_config.enableTraitHighlighting)
                {
                    UpdateTraitHighlighting(entity, photoData.ValueRO, deltaTime);
                }
            }
        }

        private void CheckAutomaticCaptures()
        {
            if (!_config.enableAutomaticCapture) return;

            foreach (var (genetics, photoData, entity) in SystemAPI.Query<RefRO<CreatureGeneticsComponent>, RefRW<PhotographyData>>().WithEntityAccess())
            {
                // Check for rare genetic combinations
                if (IsRareGeneticCombination(genetics.ValueRO) && !photoData.ValueRO.hasAutoCapture)
                {
                    var settings = CreateAutoCaptureSettings();
                    var photo = CapturePhoto(entity, settings);

                    if (photo != null)
                    {
                        photo.isAutoCapture = true;
                        photoData.ValueRW.hasAutoCapture = true;

                        UnityEngine.Debug.Log($"Auto-captured rare genetic combination: {photo.id}");
                    }
                }

                // Check for genetic milestones
                CheckGeneticMilestones(entity, genetics.ValueRO, ref photoData.ValueRW);
            }
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
            foreach (var (cameraData, entity) in SystemAPI.Query<RefRW<GeneticCameraData>>().WithEntityAccess())
            {
                // Update camera state
                cameraData.ValueRW.operationTimer += deltaTime;

                // Process active photo sessions
                if (cameraData.ValueRO.isActive)
                {
                    var cameraValue = cameraData.ValueRW;
                    ProcessCameraSession(entity, ref cameraValue, deltaTime);
                    cameraData.ValueRW = cameraValue;
                }

                // Handle camera maintenance
                var cameraValue2 = cameraData.ValueRW;
                ProcessCameraMaintenance(ref cameraValue2, deltaTime);
                cameraData.ValueRW = cameraValue2;
            }
        }

        #region Helper Methods

        private void EnableGeneticOverlays(bool enable)
        {
            // Enable/disable genetic visualization overlays
            foreach (var (photoData, entity) in SystemAPI.Query<RefRW<PhotographyData>>().WithEntityAccess())
            {
                photoData.ValueRW.showGeneticOverlay = enable;
            }
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

        private string[] GetDominantTraits(in CreatureGeneticsComponent genetics)
        {
            var traits = new List<string>();

            // Get the top 3 traits based on their values
            var traitValues = new[]
            {
                new { Name = "Strength", Value = genetics.StrengthTrait },
                new { Name = "Vitality", Value = genetics.VitalityTrait },
                new { Name = "Agility", Value = genetics.AgilityTrait },
                new { Name = "Intelligence", Value = genetics.IntellectTrait },
                new { Name = "Resilience", Value = genetics.ResilienceTrait },
                new { Name = "Charm", Value = genetics.CharmTrait }
            };

            return traitValues.OrderByDescending(t => t.Value).Take(3).Select(t => t.Name).ToArray();
        }

        private float CalculateOverallRarity(in CreatureGeneticsComponent genetics)
        {
            // Calculate rarity based on genetic traits and shiny status
            float rarity = 0f;
            rarity += genetics.StrengthTrait * 0.2f;
            rarity += genetics.VitalityTrait * 0.2f;
            rarity += genetics.AgilityTrait * 0.2f;
            rarity += genetics.IntellectTrait * 0.2f;
            rarity += genetics.ResilienceTrait * 0.1f;
            rarity += genetics.CharmTrait * 0.1f;

            // Shiny creatures are much rarer
            if (genetics.IsShiny) rarity *= 2f;

            return Mathf.Clamp01(rarity);
        }

        private float CalculateGeneticComplexity(in CreatureGeneticsComponent genetics)
        {
            // Calculate complexity based on trait diversity and genetic data
            int traitCount = 6; // Fixed number of traits (Strength, Vitality, etc.)
            int activeGeneCount = genetics.ActiveGeneCount;
            int generation = genetics.Generation;

            return (traitCount + activeGeneCount + generation) * 0.05f;
        }

        private float CalculateUniquenessScore(in CreatureGeneticsComponent genetics)
        {
            // Calculate how unique this genetic combination is
            return UnityEngine.Random.Range(0.3f, 1f); // Simplified for example
        }

        private Color[] GenerateGeneticColorProfile(in CreatureGeneticsComponent genetics)
        {
            var colors = new List<Color>();
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Adaptability" };

            foreach (var trait in traitNames)
            {
                colors.Add(_config.GetTraitColor(trait));
            }
            return colors.ToArray();
        }

        private float[] CalculateTraitDistribution(in CreatureGeneticsComponent genetics)
        {
            var distribution = new float[6]; // Fixed number of traits
            var traitValues = new[] { genetics.StrengthTrait, genetics.VitalityTrait, genetics.AgilityTrait,
                                      genetics.IntellectTrait, genetics.ResilienceTrait, genetics.CharmTrait };

            for (int i = 0; i < distribution.Length; i++)
            {
                distribution[i] = traitValues[i]; // Already normalized to 0-1 range
            }
            return distribution;
        }

        private VisualElement[] GenerateVisualElements(in CreatureGeneticsComponent genetics)
        {
            var elements = new List<VisualElement>();
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Resilience", "Charm" };
            var traitValues = new[] { genetics.StrengthTrait, genetics.VitalityTrait, genetics.AgilityTrait,
                                      genetics.IntellectTrait, genetics.ResilienceTrait, genetics.CharmTrait };

            // Add trait visualization elements
            for (int i = 0; i < traitNames.Length; i++)
            {
                elements.Add(new VisualElement
                {
                    type = VisualElementType.TraitIndicator,
                    position = UnityEngine.Random.insideUnitCircle,
                    color = _config.GetTraitColor(traitNames[i]),
                    size = traitValues[i] // Use trait value as size (already normalized)
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

        private PhotoOpportunity EvaluatePhotoOpportunity(in CreatureGeneticsComponent genetics, PhotographyData photoData)
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

        private void AddTraitLabelElements(GeneticOverlayData overlayData, in CreatureGeneticsComponent genetics)
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

        private void AddRarityIndicatorElements(GeneticOverlayData overlayData, in CreatureGeneticsComponent genetics)
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

        private void AddGeneticConnectionElements(GeneticOverlayData overlayData, in CreatureGeneticsComponent genetics)
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

        private PhotoMetadata GeneratePhotoMetadata(in CreatureGeneticsComponent genetics, PhotoCaptureSettings settings)
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

        private float CalculatePhotoQuality(in CreatureGeneticsComponent genetics, PhotoCaptureSettings settings)
        {
            float geneticQuality = CalculateOverallRarity(genetics) * 0.4f;
            float technicalQuality = EvaluateTechnicalQuality(settings) * 0.6f;
            return geneticQuality + technicalQuality;
        }

        private string[] GeneratePhotoTags(in CreatureGeneticsComponent genetics)
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

        private bool IsRareGeneticCombination(in CreatureGeneticsComponent genetics)
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

        private void CheckGeneticMilestones(Entity entity, in CreatureGeneticsComponent genetics, ref PhotographyData photoData)
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

        private Dictionary<string, float> AnalyzeTraitFrequency(in CreatureGeneticsComponent genetics)
        {
            var frequency = new Dictionary<string, float>();
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Resilience", "Charm" };
            var traitValues = new[] { genetics.StrengthTrait, genetics.VitalityTrait, genetics.AgilityTrait,
                                      genetics.IntellectTrait, genetics.ResilienceTrait, genetics.CharmTrait };

            for (int i = 0; i < traitNames.Length; i++)
            {
                frequency[traitNames[i]] = traitValues[i];
            }
            return frequency;
        }

        private Dictionary<string, float> AnalyzeRarityBreakdown(in CreatureGeneticsComponent genetics)
        {
            var breakdown = new Dictionary<string, float>();
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Resilience", "Charm" };

            foreach (var trait in traitNames)
            {
                breakdown[trait] = _config.GetTraitRarity(trait);
            }
            return breakdown;
        }

        private string[] IdentifyUniquenessfactors(in CreatureGeneticsComponent genetics)
        {
            var traitNames = new[] { "Strength", "Vitality", "Agility", "Intelligence", "Resilience", "Charm" };
            return traitNames.Where(t => _config.GetTraitRarity(t) > 0.7f).ToArray();
        }

        private float CalculateGeneticHealth(in CreatureGeneticsComponent genetics)
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

        private string[] GenerateGeneticInsights(in CreatureGeneticsComponent genetics)
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

        private float CalculateGeneticSimilarity(in CreatureGeneticsComponent genetics1, in CreatureGeneticsComponent genetics2)
        {
            // Calculate similarity based on trait value differences
            var traitDiffs = new[]
            {
                Mathf.Abs(genetics1.StrengthTrait - genetics2.StrengthTrait),
                Mathf.Abs(genetics1.VitalityTrait - genetics2.VitalityTrait),
                Mathf.Abs(genetics1.AgilityTrait - genetics2.AgilityTrait),
                Mathf.Abs(genetics1.IntellectTrait - genetics2.IntellectTrait),
                Mathf.Abs(genetics1.ResilienceTrait - genetics2.ResilienceTrait),
                Mathf.Abs(genetics1.CharmTrait - genetics2.CharmTrait)
            };

            // Calculate average difference and convert to similarity (0-1 range)
            float avgDiff = (float)traitDiffs.Average();
            float similarity = 1f - avgDiff; // Traits are already normalized 0-1
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

        private PhotoOpportunityType DetermineOpportunityType(in CreatureGeneticsComponent genetics)
        {
            var rarity = CalculateOverallRarity(genetics);
            if (rarity > 0.9f) return PhotoOpportunityType.Legendary;
            if (rarity > 0.7f) return PhotoOpportunityType.Rare;
            if (rarity > 0.5f) return PhotoOpportunityType.Uncommon;
            return PhotoOpportunityType.Common;
        }

        private string GenerateOpportunityDescription(in CreatureGeneticsComponent genetics)
        {
            var dominantTraits = GetDominantTraits(genetics);
            var dominantTrait = dominantTraits.FirstOrDefault() ?? "Unknown";
            return $"Excellent opportunity to capture {dominantTrait} traits in optimal lighting";
        }

        private string CalculateGeneticHash(in CreatureGeneticsComponent genetics)
        {
            // Create hash from all trait values and genetic data
            var combined = $"{genetics.StrengthTrait}{genetics.VitalityTrait}{genetics.AgilityTrait}" +
                          $"{genetics.IntellectTrait}{genetics.ResilienceTrait}{genetics.CharmTrait}" +
                          $"{genetics.Generation}{genetics.ActiveGeneCount}{genetics.IsShiny}";
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
        public CreatureGeneticsComponent genetics;
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
}