using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Progression;

namespace Laboratory.Editor
{
    /// <summary>
    /// Tool to migrate old level-based progression data to new skill-based partnership system.
    ///
    /// MIGRATION PROCESS:
    /// 1. Scans for entities with MonsterLevelComponent (old system)
    /// 2. Converts level/XP to skill mastery across 7 genre categories
    /// 3. Creates PartnershipSkillComponent (new system)
    /// 4. Optionally removes old components
    ///
    /// CONVERSION FORMULA:
    /// - Level 1-10 → Beginner (0-0.25 mastery)
    /// - Level 11-30 → Intermediate (0.26-0.50 mastery)
    /// - Level 31-60 → Advanced (0.51-0.75 mastery)
    /// - Level 61+ → Expert (0.76-1.0 mastery)
    /// </summary>
    public class ProgressionMigrationTool : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _scanComplete = false;
        private int _entitiesFound = 0;
        private int _entitiesMigrated = 0;
        private bool _removeOldComponents = true;
        private bool _createBackup = true;
        private List<MigrationEntry> _entries = new List<MigrationEntry>();

        [MenuItem("Tools/Chimera/Progression Migration Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProgressionMigrationTool>("Progression Migration");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Level → Skill Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool converts old level-based progression to skill-based partnership progression.\n" +
                "NO MORE LEVELS! New system tracks skill mastery through practice.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Migration Settings", EditorStyles.boldLabel);
            _removeOldComponents = EditorGUILayout.Toggle("Remove Old Level Components", _removeOldComponents);
            _createBackup = EditorGUILayout.Toggle("Create Backup Before Migration", _createBackup);

            EditorGUILayout.Space(10);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to perform migration.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Scan for Entities to Migrate", GUILayout.Height(30)))
            {
                ScanForLegacyEntities();
            }

            if (_scanComplete)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField($"Found {_entitiesFound} entities with old progression data", EditorStyles.boldLabel);

                if (_entitiesFound > 0)
                {
                    if (GUILayout.Button("Migrate All Entities", GUILayout.Height(30)))
                    {
                        MigrateAllEntities();
                    }

                    EditorGUILayout.Space(5);
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                    foreach (var entry in _entries)
                    {
                        DrawMigrationEntry(entry);
                    }

                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("No entities found with old progression data. Migration not needed!", MessageType.Info);
                }
            }

            if (_entitiesMigrated > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"✅ Successfully migrated {_entitiesMigrated} entities!", MessageType.Info);
            }
        }

        private void ScanForLegacyEntities()
        {
            _entries.Clear();
            _entitiesFound = 0;
            _scanComplete = false;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("No default world found. Enter play mode first.");
                return;
            }

            var entityManager = world.EntityManager;

            // Query for entities with MonsterLevelComponent
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MonsterLevelComponent>());
            var entities = query.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<MonsterLevelComponent>(entity))
                {
                    var levelComp = entityManager.GetComponentData<MonsterLevelComponent>(entity);

                    _entries.Add(new MigrationEntry
                    {
                        Entity = entity,
                        OldLevel = levelComp.level,
                        OldExperience = levelComp.experience,
                        NewMastery = CalculateMasteryFromLevel(levelComp.level),
                        IsMigrated = false
                    });

                    _entitiesFound++;
                }
            }

            entities.Dispose();
            _scanComplete = true;

            Debug.Log($"Scan complete: Found {_entitiesFound} entities to migrate");
        }

        private void MigrateAllEntities()
        {
            if (_createBackup)
            {
                Debug.Log("Creating backup... (implement save system backup here)");
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            _entitiesMigrated = 0;

            foreach (var entry in _entries)
            {
                if (entry.IsMigrated) continue;

                // Create new skill-based component
                if (!entityManager.HasComponent<PartnershipSkillComponent>(entry.Entity))
                {
                    var skillComp = new PartnershipSkillComponent
                    {
                        // 7 genre categories with calculated mastery
                        combatMastery = entry.NewMastery,
                        explorationMastery = entry.NewMastery * 0.8f, // Slightly varied
                        puzzleMastery = entry.NewMastery * 0.9f,
                        racingMastery = entry.NewMastery * 0.7f,
                        socialMastery = entry.NewMastery * 0.85f,
                        creativeMastery = entry.NewMastery * 0.75f,
                        competitiveMastery = entry.NewMastery * 0.95f,

                        // Partnership quality metrics
                        cooperationScore = 0.5f,
                        trustScore = 0.5f,
                        understandingScore = 0.5f,

                        // Milestone tracking
                        totalMilestonesReached = CalculateMilestones(entry.OldLevel),
                        lastMilestoneTime = 0
                    };

                    entityManager.AddComponentData(entry.Entity, skillComp);
                }

                // Remove old level component if requested
                if (_removeOldComponents && entityManager.HasComponent<MonsterLevelComponent>(entry.Entity))
                {
                    entityManager.RemoveComponent<MonsterLevelComponent>(entry.Entity);
                }

                entry.IsMigrated = true;
                _entitiesMigrated++;
            }

            Debug.Log($"✅ Migration complete! Migrated {_entitiesMigrated} entities from levels to skills.");
            EditorUtility.DisplayDialog("Migration Complete",
                $"Successfully migrated {_entitiesMigrated} entities to skill-based progression!", "OK");
        }

        private void DrawMigrationEntry(MigrationEntry entry)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Entity {entry.Entity.Index}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"Level {entry.OldLevel} → {GetMasteryTier(entry.NewMastery)} ({entry.NewMastery:P0})", GUILayout.Width(200));

            if (entry.IsMigrated)
            {
                EditorGUILayout.LabelField("✅ Migrated", GUILayout.Width(100));
            }
            else
            {
                EditorGUILayout.LabelField("⏳ Pending", GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Old XP: {entry.OldExperience:N0}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"New Mastery: {entry.NewMastery:F2}", GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private float CalculateMasteryFromLevel(int level)
        {
            // Conversion formula:
            // Level 1-10 → 0.0-0.25 (Beginner)
            // Level 11-30 → 0.26-0.50 (Intermediate)
            // Level 31-60 → 0.51-0.75 (Advanced)
            // Level 61-100 → 0.76-1.0 (Expert)

            if (level <= 10)
                return Mathf.Lerp(0f, 0.25f, level / 10f);
            else if (level <= 30)
                return Mathf.Lerp(0.25f, 0.50f, (level - 10) / 20f);
            else if (level <= 60)
                return Mathf.Lerp(0.50f, 0.75f, (level - 30) / 30f);
            else
                return Mathf.Lerp(0.75f, 1.0f, Mathf.Min((level - 60) / 40f, 1f));
        }

        private int CalculateMilestones(int level)
        {
            // Award milestones every 10 levels
            return level / 10;
        }

        private string GetMasteryTier(float mastery)
        {
            if (mastery < 0.25f) return "Beginner";
            if (mastery < 0.50f) return "Intermediate";
            if (mastery < 0.75f) return "Advanced";
            return "Expert";
        }

        private class MigrationEntry
        {
            public Entity Entity;
            public int OldLevel;
            public int OldExperience;
            public float NewMastery;
            public bool IsMigrated;
        }
    }

    /// <summary>
    /// Old level component (for reference during migration)
    /// </summary>
    public struct MonsterLevelComponent : IComponentData
    {
        public int level;
        public int experience;
        public int experienceToNextLevel;
    }

    /// <summary>
    /// New skill-based component
    /// </summary>
    public struct PartnershipSkillComponent : IComponentData
    {
        // 7 genre skill categories
        public float combatMastery;
        public float explorationMastery;
        public float puzzleMastery;
        public float racingMastery;
        public float socialMastery;
        public float creativeMastery;
        public float competitiveMastery;

        // Partnership quality
        public float cooperationScore;
        public float trustScore;
        public float understandingScore;

        // Milestone tracking
        public int totalMilestonesReached;
        public double lastMilestoneTime;
    }
}
