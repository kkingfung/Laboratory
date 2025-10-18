using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Chimera.Visuals.Data
{
    /// <summary>
    /// Data structures for procedural visual generation
    /// </summary>

    [Serializable]
    public struct VisualGeneticTraits
    {
        // Size and Scale
        public float OverallSize;
        public float HeadScale;
        public float BodyScale;
        public float LimbScale;
        public float TailScale;

        // Color Genetics
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color AccentColor;
        public float ColorIntensity;
        public float ColorVariation;

        // Pattern Genetics
        public PatternType PrimaryPattern;
        public PatternType SecondaryPattern;
        public float PatternIntensity;
        public float PatternScale;
        public float PatternComplexity;

        // Texture Properties
        public float Roughness;
        public float Metallic;
        public float Emission;
        public float Transparency;
        public float Iridescence;

        // Special Effects
        public bool HasMagicalAura;
        public bool HasParticleTrail;
        public bool HasGlow;
        public float MagicalIntensity;
        public EffectType PrimaryEffect;
    }

    [Serializable]
    public struct MaterialGenetics
    {
        public float BaseMetallic;
        public float BaseSmoothness;
        public float BaseEmission;
        public Color EmissionColor;
        public float NormalStrength;
        public float OcclusionStrength;
        public float DetailScale;
        public bool UseCustomShader;
        public string ShaderName;
    }

    [Serializable]
    public struct PatternData
    {
        public PatternType Type;
        public float Scale;
        public float Intensity;
        public Color PatternColor;
        public Vector2 Offset;
        public float Rotation;
        public float Complexity;
        public bool IsAnimated;
        public float AnimationSpeed;
    }

    [Serializable]
    public struct ParticleEffectData
    {
        public EffectType Type;
        public Color EffectColor;
        public float Intensity;
        public float Size;
        public float LifeTime;
        public float EmissionRate;
        public Vector3 Velocity;
        public bool UseGravity;
        public bool IsLooping;
    }

    [Serializable]
    public struct BiomeAdaptation
    {
        public BiomeType TargetBiome;
        public Color AdaptiveColor;
        public float AdaptationStrength;
        public Dictionary<string, float> BiomeModifiers;
        public bool HasCamouflage;
        public float CamouflageEffectiveness;
    }

    [Serializable]
    public struct MagicalVisualEffects
    {
        public bool HasAura;
        public Color AuraColor;
        public float AuraIntensity;
        public float AuraSize;
        public bool HasEnergyTrail;
        public Color TrailColor;
        public float TrailLength;
        public bool HasRuneMarkings;
        public Color RuneColor;
        public float RuneGlowIntensity;
    }

    [Serializable]
    public class VisualCache
    {
        public Dictionary<string, Material> Materials = new();
        public Dictionary<string, Texture2D> Textures = new();
        public Dictionary<string, Mesh> GeneratedMeshes = new();
        public DateTime LastCacheUpdate;
        public string CurrentVisualHash;
    }

    [Serializable]
    public struct LODConfiguration
    {
        public float[] LODDistances;
        public float[] ComplexityMultipliers;
        public bool[] EnableEffectsPerLOD;
        public int[] MaxParticlesPerLOD;
    }

    public enum PatternType
    {
        None,
        Stripes,
        Spots,
        Scales,
        Tribal,
        Geometric,
        Organic,
        Crystalline,
        Magical,
        Custom
    }

    public enum EffectType
    {
        None,
        Fire,
        Ice,
        Lightning,
        Poison,
        Holy,
        Shadow,
        Nature,
        Arcane,
        Cosmic
    }

    public enum BiomeType
    {
        Forest,
        Desert,
        Tundra,
        Ocean,
        Mountain,
        Swamp,
        Cave,
        Volcanic,
        Sky,
        Void
    }
}