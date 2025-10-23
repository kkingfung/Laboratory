using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Core.Activities.Music
{
    /// <summary>
    /// 🎵 RHYTHM STUDIO SYSTEM - Complete music and rhythm mini-game
    /// FEATURES: Genetic signature music, rhythm games, performances, composition
    /// PERFORMANCE: Real-time audio processing and rhythm detection
    /// GENETICS: Agility, Intelligence, Sociability create unique musical signatures
    /// </summary>

    #region Music Components

    /// <summary>
    /// Rhythm studio configuration and state
    /// </summary>
    public struct RhythmStudioComponent : IComponentData
    {
        public StudioType Type;
        public StudioStatus Status;
        public int MaxPerformers;
        public int CurrentPerformers;
        public float SessionTimer;
        public float SessionDuration;
        public int SongsComposed;
        public int PerformancesCompleted;
        public float AverageRhythmAccuracy;
        public MusicGenre CurrentGenre;
        public int DifficultyLevel;
        public bool IsRecording;
        public float MasterVolume;
        public int AudienceSize;
    }

    /// <summary>
    /// Individual performer state in music activities
    /// </summary>
    public struct MusicPerformerComponent : IComponentData
    {
        public Entity Studio;
        public PerformerStatus Status;
        public MusicRole Role;
        public InstrumentType PrimaryInstrument;
        public InstrumentType SecondaryInstrument;
        public float RhythmAccuracy;
        public float BestAccuracy;
        public int NotesHit;
        public int NotesMissed;
        public int PerfectHits;
        public int ComboCount;
        public int MaxCombo;
        public float PerformanceScore;
        public bool IsInFlow;
        public float FlowDuration;
        public MusicMood CurrentMood;
    }

    /// <summary>
    /// Musical performance and rhythm capabilities
    /// </summary>
    public struct MusicPerformanceComponent : IComponentData
    {
        // Core musical abilities (from genetics)
        public float RhythmSense;
        public float TonalAccuracy;
        public float MusicalCreativity;
        public float PerformanceCharisma;
        public float TimingPrecision;
        public float HarmonicIntelligence;
        public float ImprovisationSkill;

        // Instrument proficiencies
        public float StringInstruments;
        public float WindInstruments;
        public float PercussionInstruments;
        public float KeyboardInstruments;
        public float VocalAbility;
        public float ElectronicInstruments;

        // Genre specializations
        public float ClassicalMusic;
        public float JazzMusic;
        public float RockMusic;
        public float ElectronicMusic;
        public float FolkMusic;
        public float ExperimentalMusic;

        // Performance bonuses
        public float StagePresence;
        public float AudienceConnection;
        public float CollaborationSkill;
        public int PerformanceExperience;
        public bool HasPerfectPitch;
        public bool CanCompose;
    }

    /// <summary>
    /// Genetic music signature component
    /// </summary>
    public struct GeneticMusicSignatureComponent : IComponentData
    {
        // Musical DNA derived from genetics
        public float BaseFrequency;
        public float HarmonicComplexity;
        public float RhythmicPattern;
        public float MelodicCurvature;
        public float TempoPreference;
        public float DynamicRange;
        public float TimbreCharacteristics;

        // Scale and mode preferences
        public MusicalScale PreferredScale;
        public MusicalMode PreferredMode;
        public TimeSignature PreferredTimeSignature;
        public int PreferredKey;

        // Emotional expression through music
        public float MusicalEmotion;
        public float EnergyLevel;
        public float ComplexityLevel;
        public float SocialHarmony;

        // Composition fingerprint
        public uint GeneticMusicHash;
        public bool IsSignatureGenerated;
        public float SignatureStrength;
    }

    /// <summary>
    /// Rhythm game mechanics
    /// </summary>
    public struct RhythmGameComponent : IComponentData
    {
        public RhythmGameType GameType;
        public float BPM;
        public int BeatsPerMeasure;
        public float CurrentBeat;
        public float BeatTimer;
        public int NotesOnTrack;
        public int CurrentNoteIndex;
        public float ScrollSpeed;
        public RhythmDifficulty Difficulty;
        public bool IsPaused;
        public float SongProgress;
        public int TotalNotes;
        public float AccuracyWindow;
        public bool HasMultipleTracks;
    }

    /// <summary>
    /// Musical composition system
    /// </summary>
    public struct MusicCompositionComponent : IComponentData
    {
        public CompositionType Type;
        public CompositionStatus Status;
        public float CompositionProgress;
        public int MeasuresCompleted;
        public int TotalMeasures;
        public int InstrumentTracks;
        public float MusicalComplexity;
        public MusicGenre Genre;
        public bool IsCollaborative;
        public Entity CollaborationPartner;
        public float CreativityScore;
        public float TechnicalScore;
        public float OriginalityScore;
    }

    /// <summary>
    /// Performance and audience system
    /// </summary>
    public struct PerformanceAudienceComponent : IComponentData
    {
        public Entity Performer;
        public int AudienceSize;
        public float AudienceEngagement;
        public float AudienceSatisfaction;
        public float AudienceEnergy;
        public PerformanceVenue Venue;
        public bool IsLivePerformance;
        public int AppearanceCount;
        public float FanBaseSize;
        public float CriticalReception;
        public int EncoreRequests;
    }

    #endregion

    #region Music Enums

    public enum StudioType : byte
    {
        Recording_Studio,
        Performance_Hall,
        Practice_Room,
        Composition_Suite,
        Rhythm_Game_Arena,
        Collaborative_Space,
        Concert_Venue,
        Electronic_Lab
    }

    public enum StudioStatus : byte
    {
        Open,
        Session_Active,
        Recording,
        Performance,
        Composition,
        Rhythm_Challenge,
        Closed,
        Maintenance
    }

    public enum PerformerStatus : byte
    {
        Idle,
        Warming_Up,
        Performing,
        Composing,
        Playing_Rhythm_Game,
        In_Flow_State,
        Taking_Break,
        Collaborating
    }

    public enum MusicRole : byte
    {
        Solo_Performer,
        Lead_Musician,
        Rhythm_Section,
        Harmony_Support,
        Composer,
        Conductor,
        Improviser,
        Rhythm_Gamer
    }

    public enum InstrumentType : byte
    {
        None,
        Piano,
        Guitar,
        Violin,
        Drums,
        Flute,
        Trumpet,
        Saxophone,
        Bass,
        Synthesizer,
        Voice,
        Harp,
        Xylophone
    }

    public enum MusicGenre : byte
    {
        Classical,
        Jazz,
        Rock,
        Electronic,
        Folk,
        Blues,
        World,
        Experimental,
        Ambient,
        Dance
    }

    public enum MusicMood : byte
    {
        Peaceful,
        Energetic,
        Melancholy,
        Joyful,
        Mysterious,
        Intense,
        Playful,
        Romantic,
        Epic,
        Meditative
    }

    public enum MusicalScale : byte
    {
        Major,
        Minor,
        Pentatonic,
        Blues,
        Dorian,
        Mixolydian,
        Chromatic,
        Whole_Tone
    }

    public enum MusicalMode : byte
    {
        Ionian,
        Dorian,
        Phrygian,
        Lydian,
        Mixolydian,
        Aeolian,
        Locrian
    }

    public enum TimeSignature : byte
    {
        FourFour = 44,
        ThreeFour = 34,
        TwoFour = 24,
        SixEight = 68,
        FiveFour = 54,
        SevenEight = 78
    }

    public enum RhythmGameType : byte
    {
        Note_Timing,
        Beat_Matching,
        Melody_Following,
        Chord_Progression,
        Rhythm_Memory,
        Improvisation_Challenge
    }

    public enum RhythmDifficulty : byte
    {
        Beginner,
        Easy,
        Medium,
        Hard,
        Expert,
        Master
    }

    public enum CompositionType : byte
    {
        Solo_Piece,
        Duet,
        Small_Ensemble,
        Orchestra,
        Electronic_Track,
        Hybrid_Composition
    }

    public enum CompositionStatus : byte
    {
        Planning,
        Composing,
        Arranging,
        Refining,
        Complete,
        Performing
    }

    public enum PerformanceVenue : byte
    {
        Practice_Room,
        Small_Stage,
        Concert_Hall,
        Outdoor_Venue,
        Recording_Studio,
        Virtual_Space
    }

    #endregion

    #region Music Systems

    /// <summary>
    /// Main rhythm studio management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class RhythmStudioManagementSystem : SystemBase
    {
        private EntityQuery studioQuery;
        private EntityQuery performerQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            studioQuery = GetEntityQuery(ComponentType.ReadWrite<RhythmStudioComponent>());
            performerQuery = GetEntityQuery(ComponentType.ReadWrite<MusicPerformerComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update rhythm studios
            var studioUpdateJob = new StudioUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb,
                random = Unity.Mathematics.Random.CreateFromIndex((uint)(System.DateTime.Now.Ticks))
            };
            Dependency = studioUpdateJob.ScheduleParallel(studioQuery, Dependency);

            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }


    public partial struct StudioUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public Unity.Mathematics.Random random;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref RhythmStudioComponent studio)
        {
            switch (studio.Status)
            {
                case StudioStatus.Open:
                    // Wait for performers
                    if (studio.CurrentPerformers > 0)
                    {
                        studio.Status = StudioStatus.Session_Active;
                        studio.SessionTimer = 0f;
                    }
                    break;

                case StudioStatus.Session_Active:
                    UpdateMusicSession(ref studio, DeltaTime);
                    break;

                case StudioStatus.Recording:
                    UpdateRecordingSession(ref studio, DeltaTime);
                    break;

                case StudioStatus.Performance:
                    UpdatePerformanceSession(ref studio, DeltaTime);
                    break;

                case StudioStatus.Composition:
                    UpdateCompositionSession(ref studio, DeltaTime);
                    break;

                case StudioStatus.Rhythm_Challenge:
                    UpdateRhythmChallenge(ref studio, DeltaTime);
                    break;
            }

            // Update audience engagement
            UpdateAudienceMetrics(ref studio, DeltaTime);
        }


        private void UpdateMusicSession(ref RhythmStudioComponent studio, float deltaTime)
        {
            studio.SessionTimer += deltaTime;

            // Session complete check
            if (studio.SessionTimer >= studio.SessionDuration || studio.CurrentPerformers == 0)
            {
                CompleteSession(ref studio);
            }

            // Update session statistics
            if (studio.CurrentPerformers > 0)
            {
                studio.AverageRhythmAccuracy = CalculateAverageAccuracy(studio);
            }
        }


        private void UpdateRecordingSession(ref RhythmStudioComponent studio, float deltaTime)
        {
            studio.SessionTimer += deltaTime;
            studio.IsRecording = true;

            // Recording sessions are longer
            if (studio.SessionTimer >= studio.SessionDuration * 2f)
            {
                studio.SongsComposed++;
                CompleteSession(ref studio);
            }
        }


        private void UpdatePerformanceSession(ref RhythmStudioComponent studio, float deltaTime)
        {
            studio.SessionTimer += deltaTime;

            // Performance sessions include audience
            studio.AudienceSize = math.max(1, studio.CurrentPerformers * 10);

            if (studio.SessionTimer >= studio.SessionDuration)
            {
                studio.PerformancesCompleted++;
                CompleteSession(ref studio);
            }
        }


        private void UpdateCompositionSession(ref RhythmStudioComponent studio, float deltaTime)
        {
            studio.SessionTimer += deltaTime;

            // Composition takes time
            if (studio.SessionTimer >= studio.SessionDuration * 1.5f)
            {
                studio.SongsComposed++;
                CompleteSession(ref studio);
            }
        }


        private void UpdateRhythmChallenge(ref RhythmStudioComponent studio, float deltaTime)
        {
            studio.SessionTimer += deltaTime;

            // Rhythm challenges are shorter but more intense
            if (studio.SessionTimer >= studio.SessionDuration * 0.5f)
            {
                CompleteSession(ref studio);
            }
        }


        private void UpdateAudienceMetrics(ref RhythmStudioComponent studio, float deltaTime)
        {
            // Audience grows with successful performances
            if (studio.AverageRhythmAccuracy > 0.8f)
            {
                studio.AudienceSize = (int)(studio.AudienceSize * 1.05f);
            }
            else if (studio.AverageRhythmAccuracy < 0.5f)
            {
                studio.AudienceSize = (int)(studio.AudienceSize * 0.95f);
            }

            studio.AudienceSize = math.clamp(studio.AudienceSize, 0, 1000);
        }


        private void CompleteSession(ref RhythmStudioComponent studio)
        {
            studio.Status = StudioStatus.Open;
            studio.CurrentPerformers = 0;
            studio.SessionTimer = 0f;
            studio.IsRecording = false;
        }


        private float CalculateAverageAccuracy(RhythmStudioComponent studio)
        {
            // This would normally query all performers in the studio
            // For now, simplified calculation
            return random.NextFloat(0.6f, 0.95f);
        }
    }

    /// <summary>
    /// Musical performance and rhythm game system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RhythmStudioManagementSystem))]
    public partial class MusicPerformanceSystem : SystemBase
    {
        private EntityQuery performanceQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            performanceQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<MusicPerformerComponent>(),
                ComponentType.ReadOnly<MusicPerformanceComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>()
            });
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var performanceJob = new MusicPerformanceJob
            {
                DeltaTime = deltaTime,
                Time = (float)SystemAPI.Time.ElapsedTime,
                random = Unity.Mathematics.Random.CreateFromIndex((uint)(System.DateTime.Now.Ticks + 456))
            };

            Dependency = performanceJob.ScheduleParallel(performanceQuery, Dependency);
        }
    }


    public partial struct MusicPerformanceJob : IJobEntity
    {
        public float DeltaTime;
        public float Time;
        public Unity.Mathematics.Random random;

        public void Execute(ref MusicPerformerComponent performer,
            in MusicPerformanceComponent performance,
            RefRO<GeneticDataComponent> genetics)
        {
            if (performer.Status != PerformerStatus.Performing)
                return;

            // Update musical performance based on genetics and skill
            UpdateRhythmAccuracy(ref performer, performance, genetics.ValueRO);
            UpdatePerformanceScore(ref performer, performance, genetics.ValueRO);
            UpdateFlowState(ref performer, performance, DeltaTime);
            UpdateMusicalMood(ref performer, genetics.ValueRO, Time);
        }


        private void UpdateRhythmAccuracy(ref MusicPerformerComponent performer, MusicPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Calculate rhythm accuracy based on genetics and skill
            float rhythmSkill = genetics.Agility * performance.RhythmSense * performance.TimingPrecision;
            float baseAccuracy = rhythmSkill * 0.9f; // Max 90% base accuracy

            // Add some randomness for realism
            float randomFactor = random.NextFloat(-0.1f, 0.1f);
            float currentAccuracy = math.clamp(baseAccuracy + randomFactor, 0.1f, 1.0f);

            performer.RhythmAccuracy = currentAccuracy;

            // Update best accuracy
            if (currentAccuracy > performer.BestAccuracy)
            {
                performer.BestAccuracy = currentAccuracy;
            }

            // Update hit/miss counts
            if (currentAccuracy > 0.8f)
            {
                performer.NotesHit++;
                if (currentAccuracy > 0.95f)
                {
                    performer.PerfectHits++;
                    UpdateCombo(ref performer, true);
                }
            }
            else
            {
                performer.NotesMissed++;
                UpdateCombo(ref performer, false);
            }
        }


        private void UpdateCombo(ref MusicPerformerComponent performer, bool hit)
        {
            if (hit)
            {
                performer.ComboCount++;
                if (performer.ComboCount > performer.MaxCombo)
                {
                    performer.MaxCombo = performer.ComboCount;
                }
            }
            else
            {
                performer.ComboCount = 0;
            }
        }


        private void UpdatePerformanceScore(ref MusicPerformerComponent performer, MusicPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Calculate performance score based on multiple factors
            float accuracyScore = performer.RhythmAccuracy * 1000f;
            float comboBonus = performer.ComboCount * 10f;
            float perfectionBonus = performer.PerfectHits * 50f;
            float charismaBonus = genetics.Sociability * performance.PerformanceCharisma * 200f;

            performer.PerformanceScore = accuracyScore + comboBonus + perfectionBonus + charismaBonus;
        }


        private void UpdateFlowState(ref MusicPerformerComponent performer, MusicPerformanceComponent performance, float deltaTime)
        {
            // Flow state occurs when accuracy is consistently high
            if (performer.RhythmAccuracy > 0.9f && performer.ComboCount > 10)
            {
                if (!performer.IsInFlow)
                {
                    performer.IsInFlow = true;
                    performer.FlowDuration = 0f;
                }
                else
                {
                    performer.FlowDuration += deltaTime;
                }
            }
            else
            {
                performer.IsInFlow = false;
                performer.FlowDuration = 0f;
            }
        }


        private void UpdateMusicalMood(ref MusicPerformerComponent performer, GeneticDataComponent genetics, float time)
        {
            // Musical mood changes based on genetics and performance
            float moodValue = genetics.Sociability + genetics.Intelligence * 0.5f + performer.RhythmAccuracy;

            // Cycle through moods based on genetics
            int moodIndex = (int)(moodValue * 10f + time * 0.1f) % 10;
            performer.CurrentMood = (MusicMood)moodIndex;
        }
    }

    /// <summary>
    /// Genetic music signature generation system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MusicPerformanceSystem))]
    public partial class GeneticMusicSignatureSystem : SystemBase
    {
        private EntityQuery signatureQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            signatureQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<GeneticMusicSignatureComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>(),
                ComponentType.ReadOnly<CreatureIdentityComponent>()
            });
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            foreach (var (signature, genetics, identity) in
                SystemAPI.Query<RefRW<GeneticMusicSignatureComponent>, RefRO<GeneticDataComponent>, RefRO<CreatureIdentityComponent>>())
            {
                if (!signature.ValueRO.IsSignatureGenerated)
                {
                    GenerateGeneticMusicSignature(ref signature.ValueRW, genetics.ValueRO, identity.ValueRO);
                }
            }
        }

        private void GenerateGeneticMusicSignature(ref GeneticMusicSignatureComponent signature, GeneticDataComponent genetics, CreatureIdentityComponent identity)
        {
            // Generate unique musical signature from genetics
            var random = new Unity.Mathematics.Random((uint)identity.CreatureID);

            // Base frequency derived from genetics
            signature.BaseFrequency = 220f + (genetics.Intelligence * 440f); // A3 to A4 range

            // Harmonic complexity from intelligence and creativity
            signature.HarmonicComplexity = genetics.Intelligence * genetics.Curiosity;

            // Rhythmic pattern from agility and social traits
            signature.RhythmicPattern = genetics.Agility * genetics.Sociability;

            // Melodic curvature from overall genetic diversity
            signature.MelodicCurvature = (genetics.Speed + genetics.Adaptability) * 0.5f;

            // Tempo preference from energy traits
            signature.TempoPreference = 60f + (genetics.Stamina * 120f); // 60-180 BPM

            // Dynamic range from genetic variation
            signature.DynamicRange = math.abs(genetics.Aggression - genetics.Caution);

            // Timbre characteristics from size and social traits
            signature.TimbreCharacteristics = genetics.Size * genetics.Sociability;

            // Musical preferences
            signature.PreferredScale = DeterminePreferredScale(genetics, random);
            signature.PreferredMode = DeterminePreferredMode(genetics, random);
            signature.PreferredTimeSignature = DetermineTimeSignature(genetics, random);
            signature.PreferredKey = random.NextInt(0, 12); // 12 chromatic keys

            // Emotional expression
            signature.MusicalEmotion = genetics.Sociability;
            signature.EnergyLevel = genetics.Stamina;
            signature.ComplexityLevel = genetics.Intelligence;
            signature.SocialHarmony = genetics.Sociability;

            // Generate unique hash
            signature.GeneticMusicHash = GenerateMusicHash(genetics, identity);
            signature.IsSignatureGenerated = true;
            signature.SignatureStrength = CalculateSignatureStrength(genetics);
        }

        private MusicalScale DeterminePreferredScale(GeneticDataComponent genetics, Unity.Mathematics.Random random)
        {
            if (genetics.Intelligence > 0.7f)
                return MusicalScale.Chromatic; // Complex creatures prefer chromatic
            else if (genetics.Sociability > 0.7f)
                return MusicalScale.Major; // Social creatures prefer major
            else if (genetics.Aggression > 0.7f)
                return MusicalScale.Minor; // Aggressive creatures prefer minor
            else
                return MusicalScale.Pentatonic; // Default to pentatonic
        }

        private MusicalMode DeterminePreferredMode(GeneticDataComponent genetics, Unity.Mathematics.Random random)
        {
            int modeIndex = (int)(genetics.Curiosity * 7f) % 7;
            return (MusicalMode)modeIndex;
        }

        private TimeSignature DetermineTimeSignature(GeneticDataComponent genetics, Unity.Mathematics.Random random)
        {
            if (genetics.Agility > 0.8f)
                return TimeSignature.SevenEight; // Agile creatures like complex rhythms
            else if (genetics.Stamina > 0.7f)
                return TimeSignature.SixEight; // Endurance creatures like flowing rhythms
            else if (genetics.Sociability > 0.6f)
                return TimeSignature.FourFour; // Social creatures like common time
            else
                return TimeSignature.ThreeFour; // Default waltz time
        }

        private uint GenerateMusicHash(GeneticDataComponent genetics, CreatureIdentityComponent identity)
        {
            // Create unique hash from genetic traits
            uint hash = (uint)identity.CreatureID;
            hash ^= (uint)(genetics.Intelligence * 1000000);
            hash ^= (uint)(genetics.Agility * 2000000);
            hash ^= (uint)(genetics.Sociability * 3000000);
            hash ^= (uint)(genetics.Speed * 4000000);
            return hash;
        }

        private float CalculateSignatureStrength(GeneticDataComponent genetics)
        {
            // Strength based on genetic diversity and extremes
            float diversity = math.abs(genetics.Intelligence - genetics.Agility) +
                             math.abs(genetics.Sociability - genetics.Aggression) +
                             math.abs(genetics.Speed - genetics.Stamina);
            return math.clamp(diversity, 0.3f, 1.0f);
        }
    }

    /// <summary>
    /// Rhythm game mechanics system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GeneticMusicSignatureSystem))]
    public partial class RhythmGameMechanicsSystem : SystemBase
    {
        private EntityQuery rhythmGameQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            rhythmGameQuery = GetEntityQuery(ComponentType.ReadWrite<RhythmGameComponent>());
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var rhythmGame in SystemAPI.Query<RefRW<RhythmGameComponent>>())
            {
                if (!rhythmGame.ValueRO.IsPaused)
                {
                    UpdateRhythmGame(ref rhythmGame.ValueRW, deltaTime);
                }
            }
        }

        private void UpdateRhythmGame(ref RhythmGameComponent rhythmGame, float deltaTime)
        {
            // Update beat timing
            rhythmGame.BeatTimer += deltaTime;
            float beatDuration = 60f / rhythmGame.BPM; // Seconds per beat

            if (rhythmGame.BeatTimer >= beatDuration)
            {
                rhythmGame.CurrentBeat++;
                rhythmGame.BeatTimer = 0f;

                // Wrap around after a measure
                if (rhythmGame.CurrentBeat >= rhythmGame.BeatsPerMeasure)
                {
                    rhythmGame.CurrentBeat = 0;
                }
            }

            // Update song progress
            float totalBeats = rhythmGame.TotalNotes * (rhythmGame.BeatsPerMeasure / 4f); // Assume quarter notes
            rhythmGame.SongProgress = rhythmGame.CurrentBeat / totalBeats;

            // Update note scrolling
            rhythmGame.CurrentNoteIndex = (int)(rhythmGame.SongProgress * rhythmGame.TotalNotes);

            // Adjust accuracy window based on difficulty
            rhythmGame.AccuracyWindow = rhythmGame.Difficulty switch
            {
                RhythmDifficulty.Beginner => 0.3f,
                RhythmDifficulty.Easy => 0.25f,
                RhythmDifficulty.Medium => 0.2f,
                RhythmDifficulty.Hard => 0.15f,
                RhythmDifficulty.Expert => 0.1f,
                RhythmDifficulty.Master => 0.05f,
                _ => 0.2f
            };
        }
    }

    #endregion

    #region Music Authoring

    /// <summary>
    /// MonoBehaviour authoring for rhythm studios
    /// </summary>
    public class RhythmStudioAuthoring : MonoBehaviour
    {
        [Header("Studio Configuration")]
        public StudioType studioType = StudioType.Performance_Hall;
        [Range(1, 20)] public int maxPerformers = 8;
        [Range(60f, 1800f)] public float sessionDuration = 300f;
        public MusicGenre[] supportedGenres = { MusicGenre.Jazz, MusicGenre.Rock, MusicGenre.Classical };

        [Header("Equipment and Features")]
        public InstrumentType[] availableInstruments;
        public bool hasRecordingCapabilities = true;
        public bool supportsRhythmGames = true;
        public bool allowsComposition = true;
        [Range(0f, 1f)] public float acousticQuality = 0.8f;

        [Header("Audience and Performance")]
        [Range(10, 1000)] public int maxAudienceSize = 200;
        public bool isPublicVenue = true;
        public Transform stage;
        public Transform[] instrumentPositions;
        public Transform audienceArea;

        [ContextMenu("Create Rhythm Studio Entity")]
        public void CreateRhythmStudioEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            // Add rhythm studio component
            entityManager.AddComponentData(entity, new RhythmStudioComponent
            {
                Type = studioType,
                Status = StudioStatus.Open,
                MaxPerformers = maxPerformers,
                CurrentPerformers = 0,
                SessionTimer = 0f,
                SessionDuration = sessionDuration,
                SongsComposed = 0,
                PerformancesCompleted = 0,
                AverageRhythmAccuracy = 0f,
                CurrentGenre = supportedGenres.Length > 0 ? supportedGenres[0] : MusicGenre.Classical,
                DifficultyLevel = 3,
                IsRecording = false,
                MasterVolume = acousticQuality,
                AudienceSize = 0
            });

            // Add activity center component
            entityManager.AddComponentData(entity, new ActivityCenterComponent
            {
                ActivityType = ActivityType.Music,
                MaxParticipants = maxPerformers,
                CurrentParticipants = 0,
                ActivityDuration = sessionDuration,
                DifficultyLevel = 3f,
                IsActive = true,
                QualityRating = acousticQuality
            });

            // Link to transform
            entityManager.AddComponentData(entity, Unity.Transforms.LocalTransform.FromPositionRotation(transform.position, transform.rotation));

            // Link to GameObject
            entityManager.AddComponentData(entity, new GameObjectLinkComponent
            {
                InstanceID = gameObject.GetInstanceID(),
                IsActive = gameObject.activeInHierarchy
            });

            Debug.Log($"✅ Created {studioType} with {supportedGenres.Length} supported genres");
        }

        private void OnDrawGizmos()
        {
            // Draw studio bounds
            var color = studioType switch
            {
                StudioType.Recording_Studio => Color.red,
                StudioType.Performance_Hall => Color.yellow,
                StudioType.Practice_Room => Color.blue,
                StudioType.Composition_Suite => Color.green,
                StudioType.Rhythm_Game_Arena => Color.magenta,
                StudioType.Concert_Venue => Color.white,
                _ => Color.gray
            };

            Gizmos.color = color;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 10f);

            // Draw stage
            if (stage != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(stage.position, Vector3.one * 4f);
            }

            // Draw instrument positions
            if (instrumentPositions != null)
            {
                Gizmos.color = Color.green;
                foreach (var pos in instrumentPositions)
                {
                    if (pos != null)
                    {
                        Gizmos.DrawWireSphere(pos.position, 1f);
                        if (stage != null)
                        {
                            Gizmos.DrawLine(stage.position, pos.position);
                        }
                    }
                }
            }

            // Draw audience area
            if (audienceArea != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(audienceArea.position, Vector3.one * 6f);
            }

            // Draw quality indicator
            Gizmos.color = Color.cyan;
            for (int i = 0; i < (int)(acousticQuality * 10); i++)
            {
                Gizmos.DrawLine(
                    transform.position + Vector3.up * (3f + i * 0.3f),
                    transform.position + Vector3.up * (3f + i * 0.3f) + Vector3.right * 1f
                );
            }
        }
    }

    #endregion
}