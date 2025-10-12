using System;
using UnityEngine;

namespace Laboratory.Subsystems.Audio
{
    /// <summary>
    /// Configuration for the Audio Subsystem.
    /// Defines audio quality settings, channel limits, performance options, and integration settings.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSubsystemConfig", menuName = "Chimera/Subsystems/Audio/Config")]
    public class AudioSubsystemConfig : ScriptableObject
    {
        [Header("Core Audio Configuration")]
        public CoreAudioConfig CoreConfig = new CoreAudioConfig();

        [Header("Performance Configuration")]
        public AudioPerformanceConfig PerformanceConfig = new AudioPerformanceConfig();

        [Header("Music System Configuration")]
        public MusicSystemConfig MusicConfig = new MusicSystemConfig();

        [Header("SFX System Configuration")]
        public SFXSystemConfig SFXConfig = new SFXSystemConfig();

        [Header("Ambient Audio Configuration")]
        public AmbientAudioConfig AmbientConfig = new AmbientAudioConfig();

        [Header("Dynamic Audio Configuration")]
        public DynamicAudioConfig DynamicConfig = new DynamicAudioConfig();

        [Header("Integration Configuration")]
        public AudioIntegrationConfig IntegrationConfig = new AudioIntegrationConfig();

        private void OnValidate()
        {
            CoreConfig.Validate();
            PerformanceConfig.Validate();
            MusicConfig.Validate();
            SFXConfig.Validate();
            AmbientConfig.Validate();
            DynamicConfig.Validate();
            IntegrationConfig.Validate();
        }
    }

    [Serializable]
    public class CoreAudioConfig
    {
        [Header("Audio Quality")]
        [SerializeField] private AudioQuality audioQuality = AudioQuality.High;
        [SerializeField] [Range(8000, 48000)] private int sampleRate = 44100;
        [SerializeField] [Range(64, 2048)] private int bufferSize = 512;
        [SerializeField] private AudioSpeakerMode speakerMode = AudioSpeakerMode.Stereo;

        [Header("Master Volume")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float ambientVolume = 0.6f;
        [SerializeField] [Range(0f, 1f)] private float voiceVolume = 1f;

        [Header("Mixer Groups")]
        [SerializeField] private string masterMixerGroup = "Master";
        [SerializeField] private string musicMixerGroup = "Music";
        [SerializeField] private string sfxMixerGroup = "SFX";
        [SerializeField] private string ambientMixerGroup = "Ambient";
        [SerializeField] private string voiceMixerGroup = "Voice";

        public AudioQuality AudioQuality => audioQuality;
        public int SampleRate => sampleRate;
        public int BufferSize => bufferSize;
        public AudioSpeakerMode SpeakerMode => speakerMode;
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public float AmbientVolume => ambientVolume;
        public float VoiceVolume => voiceVolume;
        public string MasterMixerGroup => masterMixerGroup;
        public string MusicMixerGroup => musicMixerGroup;
        public string SFXMixerGroup => sfxMixerGroup;
        public string AmbientMixerGroup => ambientMixerGroup;
        public string VoiceMixerGroup => voiceMixerGroup;

        public void Validate()
        {
            sampleRate = Mathf.Clamp(sampleRate, 8000, 48000);
            bufferSize = Mathf.Clamp(bufferSize, 64, 2048);
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            ambientVolume = Mathf.Clamp01(ambientVolume);
            voiceVolume = Mathf.Clamp01(voiceVolume);
        }
    }

    [Serializable]
    public class AudioPerformanceConfig
    {
        [Header("Channel Limits")]
        [SerializeField] [Range(16, 256)] private int maxAudioSources = 64;
        [SerializeField] [Range(4, 32)] private int maxMusicSources = 8;
        [SerializeField] [Range(8, 64)] private int maxSFXSources = 32;
        [SerializeField] [Range(4, 16)] private int maxAmbientSources = 8;
        [SerializeField] [Range(2, 8)] private int maxVoiceSources = 4;

        [Header("Distance Culling")]
        [SerializeField] private bool enableDistanceCulling = true;
        [SerializeField] [Range(10f, 1000f)] private float maxAudioDistance = 100f;
        [SerializeField] [Range(0.1f, 10f)] private float cullUpdateRate = 0.5f;

        [Header("Performance Optimization")]
        [SerializeField] private bool enableAudioCompression = true;
        [SerializeField] private bool enableAudioStreaming = true;
        [SerializeField] private bool enableAudioCaching = true;
        [SerializeField] [Range(10, 1000)] private int maxCachedClips = 100;

        [Header("LOD System")]
        [SerializeField] private bool enableAudioLOD = true;
        [SerializeField] [Range(10f, 100f)] private float highQualityDistance = 25f;
        [SerializeField] [Range(25f, 200f)] private float mediumQualityDistance = 75f;

        public int MaxAudioSources => maxAudioSources;
        public int MaxMusicSources => maxMusicSources;
        public int MaxSFXSources => maxSFXSources;
        public int MaxAmbientSources => maxAmbientSources;
        public int MaxVoiceSources => maxVoiceSources;
        public bool EnableDistanceCulling => enableDistanceCulling;
        public float MaxAudioDistance => maxAudioDistance;
        public float CullUpdateRate => cullUpdateRate;
        public bool EnableAudioCompression => enableAudioCompression;
        public bool EnableAudioStreaming => enableAudioStreaming;
        public bool EnableAudioCaching => enableAudioCaching;
        public int MaxCachedClips => maxCachedClips;
        public bool EnableAudioLOD => enableAudioLOD;
        public float HighQualityDistance => highQualityDistance;
        public float MediumQualityDistance => mediumQualityDistance;

        public void Validate()
        {
            maxAudioSources = Mathf.Clamp(maxAudioSources, 16, 256);
            maxMusicSources = Mathf.Clamp(maxMusicSources, 4, 32);
            maxSFXSources = Mathf.Clamp(maxSFXSources, 8, 64);
            maxAmbientSources = Mathf.Clamp(maxAmbientSources, 4, 16);
            maxVoiceSources = Mathf.Clamp(maxVoiceSources, 2, 8);
            maxAudioDistance = Mathf.Clamp(maxAudioDistance, 10f, 1000f);
            cullUpdateRate = Mathf.Clamp(cullUpdateRate, 0.1f, 10f);
            maxCachedClips = Mathf.Clamp(maxCachedClips, 10, 1000);
            highQualityDistance = Mathf.Clamp(highQualityDistance, 10f, 100f);
            mediumQualityDistance = Mathf.Clamp(mediumQualityDistance, 25f, 200f);
        }
    }

    [Serializable]
    public class MusicSystemConfig
    {
        [Header("Music Playback")]
        [SerializeField] private bool enableDynamicMusic = true;
        [SerializeField] private bool enableMusicCrossfade = true;
        [SerializeField] [Range(0.1f, 10f)] private float crossfadeDuration = 2f;
        [SerializeField] private bool enableMusicLooping = true;

        [Header("Adaptive Music")]
        [SerializeField] private bool enableAdaptiveMusic = true;
        [SerializeField] private bool enableCombatMusic = true;
        [SerializeField] private bool enableExplorationMusic = true;
        [SerializeField] private bool enableBreedingMusic = true;

        [Header("Music Layers")]
        [SerializeField] private bool enableLayeredMusic = true;
        [SerializeField] [Range(2, 8)] private int maxMusicLayers = 4;
        [SerializeField] [Range(0.1f, 5f)] private float layerTransitionTime = 1f;

        public bool EnableDynamicMusic => enableDynamicMusic;
        public bool EnableMusicCrossfade => enableMusicCrossfade;
        public float CrossfadeDuration => crossfadeDuration;
        public bool EnableMusicLooping => enableMusicLooping;
        public bool EnableAdaptiveMusic => enableAdaptiveMusic;
        public bool EnableCombatMusic => enableCombatMusic;
        public bool EnableExplorationMusic => enableExplorationMusic;
        public bool EnableBreedingMusic => enableBreedingMusic;
        public bool EnableLayeredMusic => enableLayeredMusic;
        public int MaxMusicLayers => maxMusicLayers;
        public float LayerTransitionTime => layerTransitionTime;

        public void Validate()
        {
            crossfadeDuration = Mathf.Clamp(crossfadeDuration, 0.1f, 10f);
            maxMusicLayers = Mathf.Clamp(maxMusicLayers, 2, 8);
            layerTransitionTime = Mathf.Clamp(layerTransitionTime, 0.1f, 5f);
        }
    }

    [Serializable]
    public class SFXSystemConfig
    {
        [Header("SFX Playback")]
        [SerializeField] private bool enableSpatialAudio = true;
        [SerializeField] private bool enableSFXVariation = true;
        [SerializeField] [Range(0.1f, 2f)] private float pitchVariationRange = 0.2f;
        [SerializeField] [Range(0.1f, 2f)] private float volumeVariationRange = 0.1f;

        [Header("SFX Categories")]
        [SerializeField] private bool enableUISound = true;
        [SerializeField] private bool enableCreatureSounds = true;
        [SerializeField] private bool enableEnvironmentalSounds = true;
        [SerializeField] private bool enableCombatSounds = true;
        [SerializeField] private bool enableBreedingSounds = true;

        [Header("SFX Processing")]
        [SerializeField] private bool enableReverbZones = true;
        [SerializeField] private bool enableDoppler = true;
        [SerializeField] [Range(0f, 5f)] private float dopplerLevel = 1f;
        [SerializeField] private bool enableOcclusion = true;

        public bool EnableSpatialAudio => enableSpatialAudio;
        public bool EnableSFXVariation => enableSFXVariation;
        public float PitchVariationRange => pitchVariationRange;
        public float VolumeVariationRange => volumeVariationRange;
        public bool EnableUISound => enableUISound;
        public bool EnableCreatureSounds => enableCreatureSounds;
        public bool EnableEnvironmentalSounds => enableEnvironmentalSounds;
        public bool EnableCombatSounds => enableCombatSounds;
        public bool EnableBreedingSounds => enableBreedingSounds;
        public bool EnableReverbZones => enableReverbZones;
        public bool EnableDoppler => enableDoppler;
        public float DopplerLevel => dopplerLevel;
        public bool EnableOcclusion => enableOcclusion;

        public void Validate()
        {
            pitchVariationRange = Mathf.Clamp(pitchVariationRange, 0.1f, 2f);
            volumeVariationRange = Mathf.Clamp(volumeVariationRange, 0.1f, 2f);
            dopplerLevel = Mathf.Clamp(dopplerLevel, 0f, 5f);
        }
    }

    [Serializable]
    public class AmbientAudioConfig
    {
        [Header("Ambient System")]
        [SerializeField] private bool enableAmbientAudio = true;
        [SerializeField] private bool enableBiomeAmbients = true;
        [SerializeField] private bool enableWeatherAmbients = true;
        [SerializeField] private bool enableTimeOfDayAmbients = true;

        [Header("Ambient Mixing")]
        [SerializeField] [Range(0.1f, 10f)] private float ambientBlendTime = 3f;
        [SerializeField] [Range(2, 8)] private int maxAmbientLayers = 4;
        [SerializeField] private bool enableAmbientRandomization = true;
        [SerializeField] [Range(5f, 60f)] private float randomizationInterval = 15f;

        [Header("Environmental Audio")]
        [SerializeField] private bool enableWind = true;
        [SerializeField] private bool enableWater = true;
        [SerializeField] private bool enableCreatureAmbients = true;
        [SerializeField] private bool enableVegetationSounds = true;

        public bool EnableAmbientAudio => enableAmbientAudio;
        public bool EnableBiomeAmbients => enableBiomeAmbients;
        public bool EnableWeatherAmbients => enableWeatherAmbients;
        public bool EnableTimeOfDayAmbients => enableTimeOfDayAmbients;
        public float AmbientBlendTime => ambientBlendTime;
        public int MaxAmbientLayers => maxAmbientLayers;
        public bool EnableAmbientRandomization => enableAmbientRandomization;
        public float RandomizationInterval => randomizationInterval;
        public bool EnableWind => enableWind;
        public bool EnableWater => enableWater;
        public bool EnableCreatureAmbients => enableCreatureAmbients;
        public bool EnableVegetationSounds => enableVegetationSounds;

        public void Validate()
        {
            ambientBlendTime = Mathf.Clamp(ambientBlendTime, 0.1f, 10f);
            maxAmbientLayers = Mathf.Clamp(maxAmbientLayers, 2, 8);
            randomizationInterval = Mathf.Clamp(randomizationInterval, 5f, 60f);
        }
    }

    [Serializable]
    public class DynamicAudioConfig
    {
        [Header("Dynamic Audio")]
        [SerializeField] private bool enableDynamicAudio = true;
        [SerializeField] private bool enableEmotionalAudio = true;
        [SerializeField] private bool enableActivityBasedAudio = true;
        [SerializeField] private bool enablePopulationAudio = true;

        [Header("Audio Triggers")]
        [SerializeField] private bool enableCreatureBirthAudio = true;
        [SerializeField] private bool enableBreedingAudio = true;
        [SerializeField] private bool enableDiscoveryAudio = true;
        [SerializeField] private bool enableEcosystemChangeAudio = true;

        [Header("Dynamic Response")]
        [SerializeField] [Range(0.1f, 5f)] private float dynamicResponseTime = 1f;
        [SerializeField] [Range(0.1f, 10f)] private float audioFadeTime = 2f;
        [SerializeField] private bool enableAudioMemory = true;
        [SerializeField] [Range(30f, 600f)] private float audioMemoryDuration = 120f;

        public bool EnableDynamicAudio => enableDynamicAudio;
        public bool EnableEmotionalAudio => enableEmotionalAudio;
        public bool EnableActivityBasedAudio => enableActivityBasedAudio;
        public bool EnablePopulationAudio => enablePopulationAudio;
        public bool EnableCreatureBirthAudio => enableCreatureBirthAudio;
        public bool EnableBreedingAudio => enableBreedingAudio;
        public bool EnableDiscoveryAudio => enableDiscoveryAudio;
        public bool EnableEcosystemChangeAudio => enableEcosystemChangeAudio;
        public float DynamicResponseTime => dynamicResponseTime;
        public float AudioFadeTime => audioFadeTime;
        public bool EnableAudioMemory => enableAudioMemory;
        public float AudioMemoryDuration => audioMemoryDuration;

        public void Validate()
        {
            dynamicResponseTime = Mathf.Clamp(dynamicResponseTime, 0.1f, 5f);
            audioFadeTime = Mathf.Clamp(audioFadeTime, 0.1f, 10f);
            audioMemoryDuration = Mathf.Clamp(audioMemoryDuration, 30f, 600f);
        }
    }

    [Serializable]
    public class AudioIntegrationConfig
    {
        [Header("Subsystem Integration")]
        [SerializeField] private bool integrateWithEcosystem = true;
        [SerializeField] private bool integrateWithGenetics = true;
        [SerializeField] private bool integrateWithBreeding = true;
        [SerializeField] private bool integrateWithDiscovery = true;

        [Header("Performance Integration")]
        [SerializeField] private bool enablePerformanceScaling = true;
        [SerializeField] private bool enableQualityScaling = true;
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] [Range(0.5f, 2f)] private float performanceThreshold = 0.8f;

        [Header("Debug and Monitoring")]
        [SerializeField] private bool enableAudioDebug = false;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private bool enableAudioVisualization = false;
        [SerializeField] private bool logAudioEvents = false;

        public bool IntegrateWithEcosystem => integrateWithEcosystem;
        public bool IntegrateWithGenetics => integrateWithGenetics;
        public bool IntegrateWithBreeding => integrateWithBreeding;
        public bool IntegrateWithDiscovery => integrateWithDiscovery;
        public bool EnablePerformanceScaling => enablePerformanceScaling;
        public bool EnableQualityScaling => enableQualityScaling;
        public float TargetFrameRate => targetFrameRate;
        public float PerformanceThreshold => performanceThreshold;
        public bool EnableAudioDebug => enableAudioDebug;
        public bool EnablePerformanceMonitoring => enablePerformanceMonitoring;
        public bool EnableAudioVisualization => enableAudioVisualization;
        public bool LogAudioEvents => logAudioEvents;

        public void Validate()
        {
            targetFrameRate = Mathf.Clamp(targetFrameRate, 30f, 120f);
            performanceThreshold = Mathf.Clamp(performanceThreshold, 0.5f, 2f);
        }
    }

    // Enums for configuration options

    public enum AudioQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }
}