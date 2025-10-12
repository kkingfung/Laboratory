using System;

namespace Laboratory.Chimera.Genetics.Visualization
{
    /// <summary>
    /// Shared types and enums for the genetic visualization and photography systems
    /// </summary>

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

    [Serializable]
    public struct PhotoCaptureSettings
    {
        public float fieldOfView;
        public float cameraDistance;
        public float cameraHeight;
        public float cameraAngle;
        public UnityEngine.Color backgroundColor;
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

    [Serializable]
    public struct FilterSettings
    {
        public float brightness;
        public float contrast;
        public float saturation;
        public UnityEngine.Color colorTint;
        public bool enableBloom;
        public float bloomIntensity;
    }
}