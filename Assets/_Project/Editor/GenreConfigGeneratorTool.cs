using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Laboratory.Chimera.Activities;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Unity Editor tool for generating genre configurations
    /// Automatically creates all 47 GenreConfiguration and ActivityConfig assets
    /// Populates GenreLibrary with sensible defaults based on genre type
    /// </summary>
    public class GenreConfigGeneratorTool : EditorWindow
    {
        private const string GENRE_CONFIG_PATH = "Assets/_Project/Resources/Configs/GenreConfigurations";
        private const string ACTIVITY_CONFIG_PATH = "Assets/_Project/Resources/Configs/Activities";
        private const string GENRE_LIBRARY_PATH = "Assets/_Project/Resources/Configs/GenreLibrary.asset";

        private GenreLibrary _genreLibrary;
        private bool _generateActivityConfigs = true;
        private bool _overwriteExisting = false;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Chimera/Genre Configuration Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<GenreConfigGeneratorTool>("Genre Config Generator");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnEnable()
        {
            // Try to load existing genre library
            _genreLibrary = AssetDatabase.LoadAssetAtPath<GenreLibrary>(GENRE_LIBRARY_PATH);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Genre Configuration Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generate all 47 genre configurations with sensible defaults.\n" +
                "This will create GenreConfiguration assets and optionally ActivityConfig assets.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Settings
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            _generateActivityConfigs = EditorGUILayout.Toggle("Generate ActivityConfigs", _generateActivityConfigs);
            _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", _overwriteExisting);

            EditorGUILayout.Space(10);

            // Genre Library
            EditorGUILayout.LabelField("Genre Library", EditorStyles.boldLabel);
            _genreLibrary = EditorGUILayout.ObjectField("Genre Library", _genreLibrary, typeof(GenreLibrary), false) as GenreLibrary;

            if (_genreLibrary == null)
            {
                if (GUILayout.Button("Create Genre Library", GUILayout.Height(30)))
                {
                    CreateGenreLibrary();
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Genre Library found: {_genreLibrary.allGenres?.Length ?? 0} slots", MessageType.Info);

                if (GUILayout.Button("Validate Genre Library", GUILayout.Height(25)))
                {
                    _genreLibrary.ValidateCompleteness();
                    _genreLibrary.PrintStatistics();
                }
            }

            EditorGUILayout.Space(10);

            // Generation buttons
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate All 47 Genre Configurations", GUILayout.Height(40)))
            {
                GenerateAllGenreConfigs();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Generate Action Genres (7)", GUILayout.Height(30)))
            {
                GenerateActionGenres();
            }

            if (GUILayout.Button("Generate Strategy Genres (5)", GUILayout.Height(30)))
            {
                GenerateStrategyGenres();
            }

            if (GUILayout.Button("Generate Puzzle Genres (5)", GUILayout.Height(30)))
            {
                GeneratePuzzleGenres();
            }

            if (GUILayout.Button("Generate Remaining Genres (30)", GUILayout.Height(30)))
            {
                GenerateRemainingGenres();
            }

            EditorGUILayout.Space(10);

            // Statistics
            if (_genreLibrary != null)
            {
                EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));

                int configured = 0;
                foreach (var genre in _genreLibrary.allGenres)
                {
                    if (genre != null) configured++;
                }

                EditorGUILayout.LabelField($"Configured Genres: {configured} / 47");
                EditorGUILayout.LabelField($"Missing: {47 - configured}");

                EditorGUILayout.EndScrollView();
            }
        }

        private void CreateGenreLibrary()
        {
            EnsureDirectoryExists(Path.GetDirectoryName(GENRE_LIBRARY_PATH));

            _genreLibrary = CreateInstance<GenreLibrary>();
            _genreLibrary.allGenres = new GenreConfiguration[47];

            AssetDatabase.CreateAsset(_genreLibrary, GENRE_LIBRARY_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log($"Created Genre Library at {GENRE_LIBRARY_PATH}");
        }

        private void GenerateAllGenreConfigs()
        {
            if (_genreLibrary == null)
            {
                Debug.LogError("Genre Library not found! Create it first.");
                return;
            }

            EnsureDirectoryExists(GENRE_CONFIG_PATH);
            if (_generateActivityConfigs)
            {
                EnsureDirectoryExists(ACTIVITY_CONFIG_PATH);
            }

            int generated = 0;

            // Generate all action genres
            generated += GenerateActionGenres();
            generated += GenerateStrategyGenres();
            generated += GeneratePuzzleGenres();
            generated += GenerateAdventureGenres();
            generated += GeneratePlatformGenres();
            generated += GenerateSimulationGenres();
            generated += GenerateArcadeGenres();
            generated += GenerateBoardCardGenres();
            generated += GenerateCoreActivityGenres();
            generated += GenerateMusicGenres();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Generated {generated} genre configurations!");
            EditorUtility.DisplayDialog("Success", $"Generated {generated} genre configurations!", "OK");
        }

        private int GenerateActionGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.FPS, "First Person Shooter", PlayerSkill.Aiming, ChimeraTrait.Focus, 60f, 1.2f);
            count += CreateGenreConfig(ActivityType.ThirdPersonShooter, "Third Person Shooter", PlayerSkill.Aiming, ChimeraTrait.Focus, 60f, 1.2f);
            count += CreateGenreConfig(ActivityType.Fighting, "Fighting", PlayerSkill.Timing, ChimeraTrait.Agility, 90f, 1.3f);
            count += CreateGenreConfig(ActivityType.BeatEmUp, "Beat 'Em Up", PlayerSkill.Reaction, ChimeraTrait.Strength, 120f, 1.1f);
            count += CreateGenreConfig(ActivityType.HackAndSlash, "Hack and Slash", PlayerSkill.Reaction, ChimeraTrait.Strength, 180f, 1.2f);
            count += CreateGenreConfig(ActivityType.Stealth, "Stealth", PlayerSkill.Reflexes, ChimeraTrait.Agility, 240f, 1.4f);
            count += CreateGenreConfig(ActivityType.SurvivalHorror, "Survival Horror", PlayerSkill.Reflexes, ChimeraTrait.Bravery, 300f, 1.3f);

            return count;
        }

        private int GenerateStrategyGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.RealTimeStrategy, "Real-Time Strategy", PlayerSkill.Strategy, ChimeraTrait.Intelligence, 300f, 1.5f);
            count += CreateGenreConfig(ActivityType.TurnBasedStrategy, "Turn-Based Strategy", PlayerSkill.Strategy, ChimeraTrait.Intelligence, 240f, 1.3f);
            count += CreateGenreConfig(ActivityType.FourXStrategy, "4X Strategy", PlayerSkill.Planning, ChimeraTrait.Intelligence, 360f, 1.6f);
            count += CreateGenreConfig(ActivityType.GrandStrategy, "Grand Strategy", PlayerSkill.Planning, ChimeraTrait.Leadership, 480f, 1.7f);
            count += CreateGenreConfig(ActivityType.AutoBattler, "Auto Battler", PlayerSkill.Strategy, ChimeraTrait.Adaptability, 180f, 1.2f);

            return count;
        }

        private int GeneratePuzzleGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.Match3, "Match-3 Puzzle", PlayerSkill.ProblemSolving, ChimeraTrait.Patience, 120f, 1.0f);
            count += CreateGenreConfig(ActivityType.TetrisLike, "Tetris-Like", PlayerSkill.Reflexes, ChimeraTrait.Adaptability, 180f, 1.1f);
            count += CreateGenreConfig(ActivityType.PhysicsPuzzle, "Physics Puzzle", PlayerSkill.ProblemSolving, ChimeraTrait.Intelligence, 240f, 1.3f);
            count += CreateGenreConfig(ActivityType.HiddenObject, "Hidden Object", PlayerSkill.Observation, ChimeraTrait.Focus, 300f, 1.0f);
            count += CreateGenreConfig(ActivityType.WordGame, "Word Game", PlayerSkill.Memory, ChimeraTrait.Intelligence, 180f, 1.0f);

            return count;
        }

        private int GenerateAdventureGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.PointAndClick, "Point and Click", PlayerSkill.Deduction, ChimeraTrait.Curiosity, 360f, 1.2f);
            count += CreateGenreConfig(ActivityType.VisualNovel, "Visual Novel", PlayerSkill.Observation, ChimeraTrait.Curiosity, 600f, 0.8f);
            count += CreateGenreConfig(ActivityType.WalkingSimulator, "Walking Simulator", PlayerSkill.Observation, ChimeraTrait.Curiosity, 240f, 0.7f);
            count += CreateGenreConfig(ActivityType.Metroidvania, "Metroidvania", PlayerSkill.Reflexes, ChimeraTrait.Curiosity, 480f, 1.4f);

            return count;
        }

        private int GeneratePlatformGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.Platformer2D, "2D Platformer", PlayerSkill.Reflexes, ChimeraTrait.Agility, 120f, 1.2f);
            count += CreateGenreConfig(ActivityType.Platformer3D, "3D Platformer", PlayerSkill.Reflexes, ChimeraTrait.Agility, 180f, 1.3f);
            count += CreateGenreConfig(ActivityType.EndlessRunner, "Endless Runner", PlayerSkill.Reflexes, ChimeraTrait.Speed, 240f, 1.1f);

            return count;
        }

        private int GenerateSimulationGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.VehicleSimulation, "Vehicle Simulation", PlayerSkill.Precision, ChimeraTrait.Precision, 300f, 1.3f);
            count += CreateGenreConfig(ActivityType.FlightSimulator, "Flight Simulator", PlayerSkill.Precision, ChimeraTrait.Precision, 360f, 1.5f);
            count += CreateGenreConfig(ActivityType.FarmingSimulator, "Farming Simulator", PlayerSkill.Planning, ChimeraTrait.Patience, 480f, 0.9f);
            count += CreateGenreConfig(ActivityType.ConstructionSimulator, "Construction Simulator", PlayerSkill.Planning, ChimeraTrait.Patience, 360f, 1.0f);

            return count;
        }

        private int GenerateArcadeGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.Roguelike, "Roguelike", PlayerSkill.Adaptation, ChimeraTrait.Adaptability, 480f, 1.6f);
            count += CreateGenreConfig(ActivityType.Roguelite, "Roguelite", PlayerSkill.Adaptation, ChimeraTrait.Adaptability, 300f, 1.4f);
            count += CreateGenreConfig(ActivityType.BulletHell, "Bullet Hell", PlayerSkill.Reflexes, ChimeraTrait.Focus, 180f, 1.8f);
            count += CreateGenreConfig(ActivityType.ClassicArcade, "Classic Arcade", PlayerSkill.Reaction, ChimeraTrait.Speed, 120f, 1.1f);

            return count;
        }

        private int GenerateBoardCardGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.BoardGame, "Board Game", PlayerSkill.Strategy, ChimeraTrait.Sociability, 360f, 1.0f);
            count += CreateGenreConfig(ActivityType.CardGame, "Card Game", PlayerSkill.Memory, ChimeraTrait.Intelligence, 240f, 1.1f);
            count += CreateGenreConfig(ActivityType.ChessLike, "Chess-Like", PlayerSkill.Strategy, ChimeraTrait.Intelligence, 480f, 1.4f);

            return count;
        }

        private int GenerateCoreActivityGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.Exploration, "Exploration", PlayerSkill.Observation, ChimeraTrait.Curiosity, 360f, 1.0f);
            count += CreateGenreConfig(ActivityType.Racing, "Racing", PlayerSkill.Reflexes, ChimeraTrait.Speed, 90f, 1.3f);
            count += CreateGenreConfig(ActivityType.TowerDefense, "Tower Defense", PlayerSkill.Strategy, ChimeraTrait.Intelligence, 300f, 1.2f);
            count += CreateGenreConfig(ActivityType.BattleRoyale, "Battle Royale", PlayerSkill.Adaptation, ChimeraTrait.Bravery, 600f, 1.7f);
            count += CreateGenreConfig(ActivityType.CityBuilder, "City Builder", PlayerSkill.Planning, ChimeraTrait.Intelligence, 480f, 1.1f);
            count += CreateGenreConfig(ActivityType.Detective, "Detective", PlayerSkill.Deduction, ChimeraTrait.Intelligence, 420f, 1.3f);
            count += CreateGenreConfig(ActivityType.Economics, "Economics", PlayerSkill.Negotiation, ChimeraTrait.Sociability, 360f, 1.2f);
            count += CreateGenreConfig(ActivityType.Sports, "Sports", PlayerSkill.Coordination, ChimeraTrait.Strength, 180f, 1.2f);
            count += CreateGenreConfig(ActivityType.Combat, "Combat", PlayerSkill.Reaction, ChimeraTrait.Strength, 120f, 1.3f);
            count += CreateGenreConfig(ActivityType.Puzzle, "Puzzle", PlayerSkill.ProblemSolving, ChimeraTrait.Intelligence, 180f, 1.1f);

            return count;
        }

        private int GenerateMusicGenres()
        {
            int count = 0;

            count += CreateGenreConfig(ActivityType.RhythmGame, "Rhythm Game", PlayerSkill.Timing, ChimeraTrait.Rhythm, 180f, 1.4f);
            count += CreateGenreConfig(ActivityType.MusicCreation, "Music Creation", PlayerSkill.Creativity, ChimeraTrait.Creativity, 480f, 1.0f);

            return count;
        }

        private int GenerateRemainingGenres()
        {
            int count = 0;

            count += GenerateAdventureGenres();
            count += GeneratePlatformGenres();
            count += GenerateSimulationGenres();
            count += GenerateArcadeGenres();
            count += GenerateBoardCardGenres();
            count += GenerateCoreActivityGenres();
            count += GenerateMusicGenres();

            return count;
        }

        private int CreateGenreConfig(ActivityType activityType, string displayName, PlayerSkill primarySkill, ChimeraTrait primaryTrait, float baseDuration, float difficultyScaling)
        {
            string fileName = $"Genre_{activityType}.asset";
            string path = Path.Combine(GENRE_CONFIG_PATH, fileName);

            // Check if exists and skip if not overwriting
            if (!_overwriteExisting && File.Exists(path))
            {
                Debug.Log($"Skipping {activityType} - already exists");
                return 0;
            }

            // Create GenreConfiguration
            var genreConfig = CreateInstance<GenreConfiguration>();
            genreConfig.genreType = activityType;
            genreConfig.displayName = displayName;
            genreConfig.description = $"{displayName} activity requiring {primarySkill} skill and {primaryTrait} trait.";
            genreConfig.primaryPlayerSkill = primarySkill;
            genreConfig.primaryChimeraTrait = primaryTrait;
            genreConfig.baseDuration = baseDuration;
            genreConfig.difficultyScaling = difficultyScaling;

            // Set sensible defaults based on difficulty
            genreConfig.scoreMultiplier = 1.0f;
            genreConfig.playerSkillWeight = 0.6f;
            genreConfig.chimeraTraitWeight = 0.4f;
            genreConfig.minimumPassingScore = 50f;
            genreConfig.optimalBondStrength = 0.7f;
            genreConfig.ageSensitivity = 0.5f;

            // Rewards scale with difficulty
            genreConfig.baseCurrencyReward = Mathf.RoundToInt(100 * difficultyScaling);
            genreConfig.baseSkillMasteryGain = 0.01f * difficultyScaling;
            genreConfig.partnershipQualityGain = 0.005f;

            // Personality effects (empty by default)
            genreConfig.personalityEffects = new PersonalityEffect[0];

            AssetDatabase.CreateAsset(genreConfig, path);

            // Add to genre library
            int index = FindGenreLibraryIndex(activityType);
            if (index >= 0 && index < _genreLibrary.allGenres.Length)
            {
                _genreLibrary.allGenres[index] = genreConfig;
                EditorUtility.SetDirty(_genreLibrary);
            }

            // Optionally create ActivityConfig
            if (_generateActivityConfigs)
            {
                CreateActivityConfig(activityType, displayName, baseDuration, difficultyScaling);
            }

            Debug.Log($"Created genre config: {displayName} at {path}");
            return 1;
        }

        private void CreateActivityConfig(ActivityType activityType, string displayName, float baseDuration, float difficultyScaling)
        {
            string fileName = $"Activity_{activityType}.asset";
            string path = Path.Combine(ACTIVITY_CONFIG_PATH, fileName);

            if (!_overwriteExisting && File.Exists(path))
            {
                return;
            }

            var activityConfig = CreateInstance<Laboratory.Chimera.Activities.ActivityConfig>();
            activityConfig.activityType = activityType;
            activityConfig.activityName = displayName;
            activityConfig.description = $"{displayName} activity";

            // Set durations based on base duration
            activityConfig.baseDurations = new Laboratory.Chimera.Activities.DifficultyDurations
            {
                easy = baseDuration * 0.5f,
                normal = baseDuration,
                hard = baseDuration * 1.5f,
                expert = baseDuration * 2f,
                master = baseDuration * 2.5f
            };

            // Set difficulty multipliers
            activityConfig.difficultyMultipliers = new Laboratory.Chimera.Activities.DifficultyMultipliers
            {
                easy = 0.5f,
                normal = 1.0f,
                hard = 1.5f * difficultyScaling,
                expert = 2.0f * difficultyScaling,
                master = 3.0f * difficultyScaling
            };

            AssetDatabase.CreateAsset(activityConfig, path);
        }

        private int FindGenreLibraryIndex(ActivityType activityType)
        {
            // Map activity type to library index (0-46)
            // Action genres: 0-6
            if (activityType >= ActivityType.FPS && activityType <= ActivityType.SurvivalHorror)
                return (int)activityType - 1;

            // Strategy genres: 7-11
            if (activityType >= ActivityType.RealTimeStrategy && activityType <= ActivityType.AutoBattler)
                return (int)activityType - 1;

            // Continue mapping...
            return (int)activityType - 1; // Simplified - adjust as needed
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"Created directory: {path}");
            }
        }
    }
}
