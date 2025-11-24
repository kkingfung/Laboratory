using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Subsystems.Audio
{
    /// <summary>
    /// Designer-friendly audio clip database.
    /// Organizes all audio clips by category for easy access and management.
    /// Create via: Assets > Create > Chimera > Audio > Audio Clip Database
    /// </summary>
    [CreateAssetMenu(fileName = "AudioClipDatabase", menuName = "Chimera/Audio/Audio Clip Database")]
    public class AudioClipDatabase : ScriptableObject
    {
        [Header("Music Tracks")]
        [SerializeField] private List<MusicTrackData> musicTracks = new List<MusicTrackData>();

        [Header("Sound Effects")]
        [SerializeField] private List<SFXData> uiSounds = new List<SFXData>();
        [SerializeField] private List<SFXData> combatSounds = new List<SFXData>();
        [SerializeField] private List<SFXData> creatureSounds = new List<SFXData>();
        [SerializeField] private List<SFXData> environmentalSounds = new List<SFXData>();
        [SerializeField] private List<SFXData> breedingSounds = new List<SFXData>();

        [Header("Ambient Audio")]
        [SerializeField] private List<AmbientTrackData> biomeAmbients = new List<AmbientTrackData>();
        [SerializeField] private List<AmbientTrackData> weatherAmbients = new List<AmbientTrackData>();

        // Quick lookup dictionaries (built at runtime)
        private Dictionary<string, MusicTrackData> _musicLookup;
        private Dictionary<string, SFXData> _sfxLookup;
        private Dictionary<string, AmbientTrackData> _ambientLookup;

        private void OnEnable()
        {
            BuildLookupTables();
        }

        /// <summary>
        /// Builds runtime lookup tables for fast audio retrieval
        /// </summary>
        public void BuildLookupTables()
        {
            _musicLookup = musicTracks.ToDictionary(m => m.TrackID, m => m);

            _sfxLookup = new Dictionary<string, SFXData>();
            foreach (var sfx in uiSounds.Concat(combatSounds).Concat(creatureSounds)
                .Concat(environmentalSounds).Concat(breedingSounds))
            {
                if (!_sfxLookup.ContainsKey(sfx.SoundID))
                    _sfxLookup[sfx.SoundID] = sfx;
            }

            _ambientLookup = biomeAmbients.Concat(weatherAmbients)
                .ToDictionary(a => a.AmbientID, a => a);
        }

        // Music Track Getters
        public MusicTrackData GetMusicTrack(string trackID)
        {
            if (_musicLookup == null) BuildLookupTables();
            return _musicLookup.GetValueOrDefault(trackID);
        }

        public List<MusicTrackData> GetMusicByContext(MusicContext context)
        {
            return musicTracks.Where(m => m.Context == context).ToList();
        }

        public MusicTrackData GetRandomMusic(MusicContext context)
        {
            var tracks = GetMusicByContext(context);
            return tracks.Count > 0 ? tracks[UnityEngine.Random.Range(0, tracks.Count)] : null;
        }

        // SFX Getters
        public SFXData GetSFX(string soundID)
        {
            if (_sfxLookup == null) BuildLookupTables();
            return _sfxLookup.GetValueOrDefault(soundID);
        }

        public List<SFXData> GetSFXByCategory(SFXCategory category)
        {
            return category switch
            {
                SFXCategory.UI => uiSounds,
                SFXCategory.Combat => combatSounds,
                SFXCategory.Creature => creatureSounds,
                SFXCategory.Environmental => environmentalSounds,
                SFXCategory.Breeding => breedingSounds,
                _ => new List<SFXData>()
            };
        }

        // Ambient Getters
        public AmbientTrackData GetAmbientTrack(string ambientID)
        {
            if (_ambientLookup == null) BuildLookupTables();
            return _ambientLookup.GetValueOrDefault(ambientID);
        }

        public List<AmbientTrackData> GetBiomeAmbients(string biomeName)
        {
            return biomeAmbients.Where(a => a.BiomeName == biomeName).ToList();
        }

        // Validation
        [ContextMenu("Validate Database")]
        public void ValidateDatabase()
        {
            int errors = 0;

            // Check for missing clips
            foreach (var music in musicTracks)
            {
                if (music.Clip == null)
                {
                    Debug.LogWarning($"[AudioClipDatabase] Music track '{music.TrackID}' has no audio clip assigned!");
                    errors++;
                }
            }

            var allSFX = uiSounds.Concat(combatSounds).Concat(creatureSounds)
                .Concat(environmentalSounds).Concat(breedingSounds);
            foreach (var sfx in allSFX)
            {
                if (sfx.Clips == null || sfx.Clips.Count == 0)
                {
                    Debug.LogWarning($"[AudioClipDatabase] SFX '{sfx.SoundID}' has no audio clips assigned!");
                    errors++;
                }
            }

            // Check for duplicate IDs
            var musicIDs = musicTracks.Select(m => m.TrackID).ToList();
            var duplicateMusic = musicIDs.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var dupID in duplicateMusic)
            {
                Debug.LogError($"[AudioClipDatabase] Duplicate music track ID: '{dupID}'");
                errors++;
            }

            if (errors == 0)
                Debug.Log($"[AudioClipDatabase] Validation passed! {musicTracks.Count} music tracks, {allSFX.Count()} SFX, {biomeAmbients.Count + weatherAmbients.Count} ambient tracks");
            else
                Debug.LogWarning($"[AudioClipDatabase] Validation completed with {errors} warnings/errors");
        }

        [ContextMenu("Rebuild Lookup Tables")]
        public void RebuildLookupTables()
        {
            BuildLookupTables();
            Debug.Log("[AudioClipDatabase] Lookup tables rebuilt");
        }
    }

    // Data Structures

    [Serializable]
    public class MusicTrackData
    {
        [Tooltip("Unique identifier for this music track")]
        public string TrackID = "music_track_001";

        [Tooltip("Display name for designers")]
        public string TrackName = "My Music Track";

        [Tooltip("The audio clip to play")]
        public AudioClip Clip;

        [Tooltip("When should this music play?")]
        public MusicContext Context = MusicContext.Exploration;

        [Tooltip("Intensity level (0-1) for adaptive music")]
        [Range(0f, 1f)] public float IntensityLevel = 0.5f;

        [Tooltip("Should this track loop?")]
        public bool Loop = true;

        [Tooltip("Volume multiplier")]
        [Range(0f, 1f)] public float Volume = 0.8f;

        [Tooltip("Fade in time (seconds)")]
        public float FadeInTime = 2f;

        [Tooltip("Fade out time (seconds)")]
        public float FadeOutTime = 2f;

        [Tooltip("Music layers for adaptive music")]
        public List<AudioClip> AdaptiveLayers = new List<AudioClip>();
    }

    [Serializable]
    public class SFXData
    {
        [Tooltip("Unique identifier for this sound effect")]
        public string SoundID = "sfx_001";

        [Tooltip("Display name for designers")]
        public string SoundName = "My Sound Effect";

        [Tooltip("Audio clips (random variation if multiple)")]
        public List<AudioClip> Clips = new List<AudioClip>();

        [Tooltip("Sound effect category")]
        public SFXCategory Category = SFXCategory.UI;

        [Tooltip("Base volume")]
        [Range(0f, 1f)] public float Volume = 1f;

        [Tooltip("Enable pitch variation?")]
        public bool EnablePitchVariation = true;

        [Tooltip("Pitch variation range")]
        [Range(0f, 0.5f)] public float PitchVariation = 0.1f;

        [Tooltip("Enable volume variation?")]
        public bool EnableVolumeVariation = false;

        [Tooltip("Volume variation range")]
        [Range(0f, 0.3f)] public float VolumeVariation = 0.1f;

        [Tooltip("3D spatial audio settings")]
        public bool Is3D = false;
        [Range(1f, 500f)] public float MaxDistance = 100f;
        [Range(0f, 1f)] public float SpatialBlend = 1f;
    }

    [Serializable]
    public class AmbientTrackData
    {
        [Tooltip("Unique identifier for this ambient track")]
        public string AmbientID = "ambient_001";

        [Tooltip("Display name")]
        public string AmbientName = "My Ambient Track";

        [Tooltip("Biome name (if biome-specific)")]
        public string BiomeName = "Forest";

        [Tooltip("Ambient audio clip")]
        public AudioClip Clip;

        [Tooltip("Should loop?")]
        public bool Loop = true;

        [Tooltip("Base volume")]
        [Range(0f, 1f)] public float Volume = 0.6f;

        [Tooltip("Blend time when transitioning")]
        public float BlendTime = 3f;

        [Tooltip("Time of day modifier (0=night, 1=day)")]
        [Range(0f, 1f)] public float TimeOfDayWeight = 0.5f;
    }

    // Enums

    public enum MusicContext
    {
        MainMenu,
        Exploration,
        Combat,
        Breeding,
        Discovery,
        Peaceful,
        Intense,
        Emotional,
        Victory,
        Defeat
    }

    public enum SFXCategory
    {
        UI,
        Combat,
        Creature,
        Environmental,
        Breeding
    }
}
