using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Laboratory.Chimera;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.UI.Components;
using System;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// Complete notification system for breeding events, creature status changes,
    /// achievements, and important game events. Provides toast notifications, 
    /// breeding alerts, milestone achievements, and contextual feedback.
    /// </summary>
    public class BreedingNotificationSystem : MonoBehaviour
    {
        [Header("Notification UI")]
        [SerializeField] private Transform notificationParent;
        [SerializeField] private GameObject toastNotificationPrefab;
        [SerializeField] private GameObject breedingAlertPrefab;
        [SerializeField] private GameObject achievementNotificationPrefab;
        [SerializeField] private GameObject statusUpdatePrefab;
        
        [Header("Notification Settings")]
        [SerializeField] private float defaultDuration = 3f;
        [SerializeField] private float longDuration = 5f;
        [SerializeField] private float quickDuration = 2f;
        [SerializeField] private int maxSimultaneousNotifications = 5;
        [SerializeField] private float notificationSpacing = 10f;
        
        [Header("Sound Effects")]
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip failureSound;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioClip achievementSound;
        [SerializeField] private AudioClip infoSound;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem successParticles;
        [SerializeField] private ParticleSystem achievementParticles;
        [SerializeField] private Animation notificationAreaAnimation;
        
        // Notification management
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private List<ActiveNotification> activeNotifications = new List<ActiveNotification>();
        
        // Audio system
        private AudioSource audioSource;
        
        // Settings
        private NotificationSettings settings = new NotificationSettings();
        
        private void Awake()
        {
            InitializeAudioSource();
            LoadNotificationSettings();
        }
        
        private void Start()
        {
            SetupEventListeners();
            StartCoroutine(ProcessNotificationQueue());
        }
        
        #region Initialization
        
        private void InitializeAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
        }
        
        private void LoadNotificationSettings()
        {
            // Load from PlayerPrefs or settings file
            settings.enableSounds = PlayerPrefs.GetInt("NotificationSounds", 1) == 1;
            settings.enableVisualEffects = PlayerPrefs.GetInt("NotificationEffects", 1) == 1;
            settings.notificationDuration = PlayerPrefs.GetFloat("NotificationDuration", 3f);
        }
        
        private void SetupEventListeners()
        {
            Debug.Log("BreedingNotificationSystem: Setting up event listeners");

            // Subscribe to breeding events
            GameEvents.OnBreedingStarted += HandleBreedingStarted;
            GameEvents.OnBreedingCompleted += HandleBreedingCompleted;
            GameEvents.OnBreedingFailed += HandleBreedingFailed;

            // Subscribe to creature events
            GameEvents.OnCreatureSpawned += HandleCreatureSpawned;
            GameEvents.OnCreatureHealthChanged += HandleCreatureHealthChanged;
            GameEvents.OnCreatureLevelUp += HandleCreatureLevelUp;
            GameEvents.OnCreatureFavorited += HandleCreatureFavorited;
            GameEvents.OnRareCreatureDiscovered += HandleRareCreatureDiscovered;

            // Subscribe to achievement events
            GameEvents.OnAchievementUnlocked += HandleAchievementUnlocked;
            GameEvents.OnMilestoneReached += HandleMilestoneReached;

            Debug.Log("BreedingNotificationSystem: All event listeners registered");
        }
        
        #endregion
        
        #region Public Notification Methods
        
        public void ShowBreedingSuccess(string parent1Name, string parent2Name, string offspringName, GeneticProfile genetics)
        {
            var rarity = CalculateGeneticRarity(genetics);
            var isRare = rarity > 0.7f;
            
            var notification = new NotificationData
            {
                Type = NotificationType.BreedingSuccess,
                Title = isRare ? "Rare Breeding Success!" : "Breeding Success!",
                Message = $"{parent1Name} + {parent2Name} = {offspringName}",
                Duration = isRare ? longDuration : defaultDuration,
                Priority = isRare ? NotificationPriority.High : NotificationPriority.Medium,
                Icon = isRare ? "rare_success" : "breeding_success",
                AdditionalData = new BreedingResultData
                {
                    OffspringName = offspringName,
                    Rarity = rarity,
                    Genetics = genetics,
                    IsRare = isRare
                }
            };
            
            QueueNotification(notification);
            
            // Special effects for rare creatures
            if (isRare)
            {
                TriggerSpecialEffects();
            }
        }
        
        public void ShowBreedingFailure(string parent1Name, string parent2Name, string reason)
        {
            var notification = new NotificationData
            {
                Type = NotificationType.BreedingFailure,
                Title = "Breeding Failed",
                Message = $"{parent1Name} + {parent2Name}\nReason: {reason}",
                Duration = defaultDuration,
                Priority = NotificationPriority.Medium,
                Icon = "breeding_failure"
            };
            
            QueueNotification(notification);
        }
        
        public void ShowBreedingStarted(string parent1Name, string parent2Name)
        {
            var notification = new NotificationData
            {
                Type = NotificationType.BreedingStarted,
                Title = "Breeding Started",
                Message = $"{parent1Name} + {parent2Name}",
                Duration = quickDuration,
                Priority = NotificationPriority.Low,
                Icon = "breeding_started"
            };
            
            QueueNotification(notification);
        }
        
        public void ShowCreatureSpawned(CreatureInstanceComponent creature)
        {
            var rarity = CalculateGeneticRarity(creature.CreatureData?.GeneticProfile);
            var isRare = rarity > 0.6f;
            
            var notification = new NotificationData
            {
                Type = NotificationType.CreatureSpawned,
                Title = isRare ? "Rare Creature Appeared!" : "New Creature",
                Message = $"{creature.name} has been added to your collection",
                Duration = isRare ? longDuration : defaultDuration,
                Priority = isRare ? NotificationPriority.High : NotificationPriority.Low,
                Icon = isRare ? "rare_creature" : "new_creature"
            };
            
            QueueNotification(notification);
        }
        
        public void ShowHealthWarning(string creatureName, int currentHealth)
        {
            if (currentHealth < 20)
            {
                var notification = new NotificationData
                {
                    Type = NotificationType.HealthWarning,
                    Title = "Health Critical!",
                    Message = $"{creatureName} needs immediate attention (Health: {currentHealth}%)",
                    Duration = longDuration,
                    Priority = NotificationPriority.High,
                    Icon = "health_critical"
                };
                
                QueueNotification(notification);
            }
            else if (currentHealth < 50)
            {
                var notification = new NotificationData
                {
                    Type = NotificationType.HealthWarning,
                    Title = "Health Low",
                    Message = $"{creatureName} health is getting low ({currentHealth}%)",
                    Duration = defaultDuration,
                    Priority = NotificationPriority.Medium,
                    Icon = "health_low"
                };
                
                QueueNotification(notification);
            }
        }
        
        public void ShowAchievementUnlocked(string achievementName, string description)
        {
            var notification = new NotificationData
            {
                Type = NotificationType.Achievement,
                Title = "Achievement Unlocked!",
                Message = $"{achievementName}\n{description}",
                Duration = longDuration,
                Priority = NotificationPriority.High,
                Icon = "achievement",
                AdditionalData = new AchievementData
                {
                    Name = achievementName,
                    Description = description
                }
            };
            
            QueueNotification(notification);
            TriggerAchievementEffects();
        }
        
        public void ShowSuccess(string title, string message)
        {
            var notification = new NotificationData
            {
                Type = NotificationType.Success,
                Title = title,
                Message = message,
                Duration = defaultDuration,
                Priority = NotificationPriority.Medium,
                Icon = "success"
            };
            
            QueueNotification(notification);
        }
        
        public void ShowWarning(string title, string message)
        {
            var notification = new NotificationData
            {
                Type = NotificationType.Warning,
                Title = title,
                Message = message,
                Duration = defaultDuration,
                Priority = NotificationPriority.Medium,
                Icon = "warning"
            };
            
            QueueNotification(notification);
        }
        
        public void ShowError(string title, string message)
        {
            var notification = new NotificationData
            {
                Type = NotificationType.Error,
                Title = title,
                Message = message,
                Duration = longDuration,
                Priority = NotificationPriority.High,
                Icon = "error"
            };
            
            QueueNotification(notification);
        }
        
        public void ShowInfo(string title, string message)
        {
            var notification = new NotificationData
            {
                Type = NotificationType.Info,
                Title = title,
                Message = message,
                Duration = defaultDuration,
                Priority = NotificationPriority.Low,
                Icon = "info"
            };
            
            QueueNotification(notification);
        }
        
        #endregion
        
        #region Notification Queue Management
        
        private void QueueNotification(NotificationData notification)
        {
            // Check for duplicate notifications
            if (IsDuplicateNotification(notification))
                return;
                
            notificationQueue.Enqueue(notification);
        }
        
        private bool IsDuplicateNotification(NotificationData newNotification)
        {
            // Check active notifications
            foreach (var active in activeNotifications)
            {
                if (active.Data.Type == newNotification.Type && 
                    active.Data.Title == newNotification.Title &&
                    (Time.time - active.StartTime) < 1f) // Within 1 second
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private IEnumerator ProcessNotificationQueue()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                
                if (notificationQueue.Count > 0 && activeNotifications.Count < maxSimultaneousNotifications)
                {
                    var notification = notificationQueue.Dequeue();
                    StartCoroutine(DisplayNotification(notification));
                }
            }
        }
        
        private IEnumerator DisplayNotification(NotificationData data)
        {
            // Create notification GameObject
            var notificationObj = CreateNotificationObject(data);
            if (notificationObj == null) yield break;
            
            var activeNotification = new ActiveNotification
            {
                GameObject = notificationObj,
                Data = data,
                StartTime = Time.time
            };
            
            activeNotifications.Add(activeNotification);
            
            // Position notification
            PositionNotification(notificationObj);
            
            // Play sound effect
            PlayNotificationSound(data.Type);
            
            // Animate in
            yield return StartCoroutine(AnimateNotificationIn(notificationObj));
            
            // Wait for duration
            yield return new WaitForSeconds(data.Duration);
            
            // Animate out
            yield return StartCoroutine(AnimateNotificationOut(notificationObj));
            
            // Clean up
            activeNotifications.Remove(activeNotification);
            Destroy(notificationObj);
            
            // Reposition remaining notifications
            RepositionNotifications();
        }
        
        private GameObject CreateNotificationObject(NotificationData data)
        {
            GameObject prefab = data.Type switch
            {
                NotificationType.BreedingSuccess or NotificationType.BreedingFailure or NotificationType.BreedingStarted => breedingAlertPrefab,
                NotificationType.Achievement or NotificationType.Milestone => achievementNotificationPrefab,
                NotificationType.HealthWarning => statusUpdatePrefab,
                _ => toastNotificationPrefab
            };
            
            if (prefab == null) prefab = toastNotificationPrefab;
            
            var notificationObj = Instantiate(prefab, notificationParent);
            var component = notificationObj.GetComponent<NotificationComponent>();
            
            if (component != null)
            {
                component.Setup(data);
            }
            
            return notificationObj;
        }
        
        private void PositionNotification(GameObject notification)
        {
            var rectTransform = notification.GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
            float yOffset = activeNotifications.Count * (rectTransform.rect.height + notificationSpacing);
            rectTransform.anchoredPosition = new Vector2(0, -yOffset);
        }
        
        private void RepositionNotifications()
        {
            for (int i = 0; i < activeNotifications.Count; i++)
            {
                var rectTransform = activeNotifications[i].GameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float targetY = -i * (rectTransform.rect.height + notificationSpacing);
                    StartCoroutine(SmoothMove(rectTransform, new Vector2(0, targetY), 0.3f));
                }
            }
        }
        
        private IEnumerator SmoothMove(RectTransform rectTransform, Vector2 targetPosition, float duration)
        {
            Vector2 startPosition = rectTransform.anchoredPosition;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                yield return null;
            }
            
            rectTransform.anchoredPosition = targetPosition;
        }
        
        #endregion
        
        #region Animation and Effects
        
        private IEnumerator AnimateNotificationIn(GameObject notification)
        {
            var canvasGroup = notification.GetComponent<CanvasGroup>();
            var rectTransform = notification.GetComponent<RectTransform>();
            
            if (canvasGroup == null)
                canvasGroup = notification.AddComponent<CanvasGroup>();
            
            // Start invisible and off-screen
            canvasGroup.alpha = 0f;
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 offScreenPos = startPos + Vector2.right * 400f;
            rectTransform.anchoredPosition = offScreenPos;
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                
                canvasGroup.alpha = t;
                rectTransform.anchoredPosition = Vector2.Lerp(offScreenPos, startPos, t);
                
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
            rectTransform.anchoredPosition = startPos;
        }
        
        private IEnumerator AnimateNotificationOut(GameObject notification)
        {
            var canvasGroup = notification.GetComponent<CanvasGroup>();
            var rectTransform = notification.GetComponent<RectTransform>();
            
            if (canvasGroup == null) yield break;
            
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 offScreenPos = startPos + Vector2.right * 400f;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                canvasGroup.alpha = 1f - t;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, offScreenPos, t);
                
                yield return null;
            }
        }
        
        private void TriggerSpecialEffects()
        {
            if (settings.enableVisualEffects && successParticles != null)
            {
                successParticles.Play();
            }
            
            if (notificationAreaAnimation != null)
            {
                notificationAreaAnimation.Play("SpecialEffect");
            }
        }
        
        private void TriggerAchievementEffects()
        {
            if (settings.enableVisualEffects && achievementParticles != null)
            {
                achievementParticles.Play();
            }
            
            // Screen flash or other special effects
            StartCoroutine(AchievementScreenFlash());
        }
        
        private IEnumerator AchievementScreenFlash()
        {
            // Create a temporary overlay for screen flash
            var overlay = new GameObject("AchievementFlash");
            overlay.transform.SetParent(transform.parent, false);
            
            var image = overlay.AddComponent<Image>();
            image.color = new Color(1f, 1f, 0f, 0f); // Yellow flash
            image.raycastTarget = false;
            
            var rectTransform = overlay.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Flash animation
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.3f;
                image.color = new Color(1f, 1f, 0f, alpha);
                yield return null;
            }
            
            Destroy(overlay);
        }
        
        #endregion
        
        #region Audio Management
        
        private void PlayNotificationSound(NotificationType type)
        {
            if (!settings.enableSounds || audioSource == null) return;
            
            AudioClip clip = type switch
            {
                NotificationType.BreedingSuccess or NotificationType.Success => successSound,
                NotificationType.BreedingFailure or NotificationType.Error => failureSound,
                NotificationType.Warning or NotificationType.HealthWarning => warningSound,
                NotificationType.Achievement or NotificationType.Milestone => achievementSound,
                _ => infoSound
            };
            
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleBreedingStarted(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2)
        {
            ShowBreedingStarted(parent1.name, parent2.name);
        }
        
        private void HandleBreedingCompleted(CreatureInstanceComponent offspring)
        {
            // This would need parent information from the breeding system
            ShowSuccess("Breeding Complete", $"{offspring.name} has been born!");
        }
        
        private void HandleBreedingFailed(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2, string reason)
        {
            ShowBreedingFailure(parent1.name, parent2.name, reason);
        }
        
        private void HandleCreatureSpawned(CreatureInstanceComponent creature)
        {
            ShowCreatureSpawned(creature);
        }
        
        private void HandleCreatureHealthChanged(CreatureInstanceComponent creature, int oldHealth, int newHealth)
        {
            if (newHealth < oldHealth && newHealth < 50)
            {
                ShowHealthWarning(creature.name, newHealth);
            }
        }
        
        private void HandleCreatureLevelUp(CreatureInstanceComponent creature, int newLevel)
        {
            ShowSuccess("Level Up!", $"{creature.name} reached level {newLevel}!");
        }
        
        private void HandleAchievementUnlocked(Achievement achievement)
        {
            ShowAchievementUnlocked(achievement.Name, achievement.Description);
        }
        
        private void HandleMilestoneReached(Milestone milestone)
        {
            ShowSuccess("Milestone Reached!", $"{milestone.Name}: {milestone.Description}");
        }
        
        private void HandleCreatureFavorited(CreatureInstanceComponent creature)
        {
            ShowInfo("Favorited", $"{creature.name} added to favorites!");
        }
        
        private void HandleRareCreatureDiscovered(CreatureInstanceComponent creature)
        {
            ShowSuccess("Rare Discovery!", $"You discovered {creature.name}!");
        }
        
        #endregion
        
        #region Utility Methods
        
        private float CalculateGeneticRarity(GeneticProfile genetics)
        {
            if (genetics?.Mutations == null) return 0f;
            
            float rarityScore = 0f;
            foreach (var mutation in genetics.Mutations)
            {
                rarityScore += mutation.severity * 0.25f;
            }
            
            return Mathf.Clamp01(rarityScore);
        }
        
        public void ClearAllNotifications()
        {
            // Clear queue
            notificationQueue.Clear();
            
            // Remove active notifications
            foreach (var active in activeNotifications)
            {
                if (active.GameObject != null)
                    Destroy(active.GameObject);
            }
            activeNotifications.Clear();
        }
        
        public void SetNotificationSettings(NotificationSettings newSettings)
        {
            settings = newSettings;
            SaveNotificationSettings();
        }
        
        private void SaveNotificationSettings()
        {
            PlayerPrefs.SetInt("NotificationSounds", settings.enableSounds ? 1 : 0);
            PlayerPrefs.SetInt("NotificationEffects", settings.enableVisualEffects ? 1 : 0);
            PlayerPrefs.SetFloat("NotificationDuration", settings.notificationDuration);
            PlayerPrefs.Save();
        }
        
        #endregion
        
        private void OnDestroy()
        {
            Debug.Log("BreedingNotificationSystem: Cleaning up and unsubscribing from events");

            // Unsubscribe from breeding events
            GameEvents.OnBreedingStarted -= HandleBreedingStarted;
            GameEvents.OnBreedingCompleted -= HandleBreedingCompleted;
            GameEvents.OnBreedingFailed -= HandleBreedingFailed;

            // Unsubscribe from creature events
            GameEvents.OnCreatureSpawned -= HandleCreatureSpawned;
            GameEvents.OnCreatureHealthChanged -= HandleCreatureHealthChanged;
            GameEvents.OnCreatureLevelUp -= HandleCreatureLevelUp;
            GameEvents.OnCreatureFavorited -= HandleCreatureFavorited;
            GameEvents.OnRareCreatureDiscovered -= HandleRareCreatureDiscovered;

            // Unsubscribe from achievement events
            GameEvents.OnAchievementUnlocked -= HandleAchievementUnlocked;
            GameEvents.OnMilestoneReached -= HandleMilestoneReached;

            // Clear any remaining notifications
            ClearAllNotifications();

            Debug.Log("BreedingNotificationSystem: Cleanup complete");
        }
    }
    
    #region Supporting Data Structures
    
    [System.Serializable]
    public class NotificationData
    {
        public NotificationType Type;
        public string Title;
        public string Message;
        public float Duration;
        public NotificationPriority Priority;
        public string Icon;
        public object AdditionalData;
    }
    
    [System.Serializable]
    public class ActiveNotification
    {
        public GameObject GameObject;
        public NotificationData Data;
        public float StartTime;
    }
    
    [System.Serializable]
    public class NotificationSettings
    {
        public bool enableSounds = true;
        public bool enableVisualEffects = true;
        public float notificationDuration = 3f;
        public bool enableBreedingNotifications = true;
        public bool enableAchievementNotifications = true;
        public bool enableHealthWarnings = true;
    }
    
    [System.Serializable]
    public class BreedingResultData
    {
        public string OffspringName;
        public float Rarity;
        public GeneticProfile Genetics;
        public bool IsRare;
    }
    
    [System.Serializable]
    public class AchievementData
    {
        public string Name;
        public string Description;
    }
    
    public enum NotificationType
    {
        BreedingSuccess,
        BreedingFailure,
        BreedingStarted,
        CreatureSpawned,
        HealthWarning,
        Achievement,
        Milestone,
        Success,
        Warning,
        Error,
        Info,
        Custom
    }
    
    public enum NotificationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    // Placeholder classes for compilation
    public class Achievement
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
    
    public class Milestone
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Static event system for game-wide events that the notification system can listen to
    /// </summary>
    public static class GameEvents
    {
        // Breeding Events
        public static System.Action<CreatureInstanceComponent, CreatureInstanceComponent> OnBreedingStarted;
        public static System.Action<CreatureInstanceComponent> OnBreedingCompleted;
        public static System.Action<CreatureInstanceComponent, CreatureInstanceComponent, string> OnBreedingFailed;

        // Creature Events
        public static System.Action<CreatureInstanceComponent> OnCreatureSpawned;
        public static System.Action<CreatureInstanceComponent, int, int> OnCreatureHealthChanged;
        public static System.Action<CreatureInstanceComponent, int> OnCreatureLevelUp;
        public static System.Action<CreatureInstanceComponent> OnCreatureFavorited;
        public static System.Action<CreatureInstanceComponent> OnRareCreatureDiscovered;

        // Achievement Events
        public static System.Action<Achievement> OnAchievementUnlocked;
        public static System.Action<Milestone> OnMilestoneReached;

        // Helper methods to trigger events safely
        public static void TriggerBreedingStarted(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2)
        {
            OnBreedingStarted?.Invoke(parent1, parent2);
        }

        public static void TriggerBreedingCompleted(CreatureInstanceComponent offspring)
        {
            OnBreedingCompleted?.Invoke(offspring);
        }

        public static void TriggerBreedingFailed(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2, string reason)
        {
            OnBreedingFailed?.Invoke(parent1, parent2, reason);
        }

        public static void TriggerCreatureSpawned(CreatureInstanceComponent creature)
        {
            OnCreatureSpawned?.Invoke(creature);
        }

        public static void TriggerCreatureHealthChanged(CreatureInstanceComponent creature, int oldHealth, int newHealth)
        {
            OnCreatureHealthChanged?.Invoke(creature, oldHealth, newHealth);
        }

        public static void TriggerCreatureLevelUp(CreatureInstanceComponent creature, int newLevel)
        {
            OnCreatureLevelUp?.Invoke(creature, newLevel);
        }

        public static void TriggerCreatureFavorited(CreatureInstanceComponent creature)
        {
            OnCreatureFavorited?.Invoke(creature);
        }

        public static void TriggerRareCreatureDiscovered(CreatureInstanceComponent creature)
        {
            OnRareCreatureDiscovered?.Invoke(creature);
        }

        public static void TriggerAchievementUnlocked(Achievement achievement)
        {
            OnAchievementUnlocked?.Invoke(achievement);
        }

        public static void TriggerMilestoneReached(Milestone milestone)
        {
            OnMilestoneReached?.Invoke(milestone);
        }

        /// <summary>
        /// Clear all event subscriptions (useful for cleanup or testing)
        /// </summary>
        public static void ClearAllEvents()
        {
            OnBreedingStarted = null;
            OnBreedingCompleted = null;
            OnBreedingFailed = null;
            OnCreatureSpawned = null;
            OnCreatureHealthChanged = null;
            OnCreatureLevelUp = null;
            OnCreatureFavorited = null;
            OnRareCreatureDiscovered = null;
            OnAchievementUnlocked = null;
            OnMilestoneReached = null;
        }
    }

    #endregion
}