using UnityEngine;
using Laboratory.Subsystems.Player;

namespace Laboratory.Gameplay
{
    /// <summary>
    /// Door that can be opened with keys or switches
    /// </summary>
    public class Door : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        [SerializeField] private bool requiresKey = false;
        [SerializeField] private string requiredKeyId = "RedKey";
        [SerializeField] private bool isLocked = true;
        [SerializeField] private float openSpeed = 2f;
        [SerializeField] private Vector3 openOffset = new Vector3(0, 3, 0);

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip lockedSound;

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isOpen = false;
        private bool isMoving = false;

        private void Awake()
        {
            closedPosition = transform.position;
            openPosition = closedPosition + openOffset;
            audioSource = GetComponent<AudioSource>();
        }

        public void Interact(PlayerController player)
        {
            if (isMoving) return;

            if (isLocked && requiresKey)
            {
                // Check if player has required key
                // This would require an inventory system
                Debug.Log($"Door requires {requiredKeyId} to open");
                PlaySound(lockedSound);
                return;
            }

            if (isOpen)
            {
                CloseDoor();
            }
            else
            {
                OpenDoor();
            }
        }

        public string GetInteractionPrompt()
        {
            if (isLocked && requiresKey)
                return $"Locked - Need {requiredKeyId}";
            
            return isOpen ? "Close Door" : "Open Door";
        }

        public bool CanInteract(PlayerController player)
        {
            return !isMoving;
        }

        private void OpenDoor()
        {
            if (isOpen || isMoving) return;

            StartCoroutine(MoveDoor(openPosition, true));
            PlaySound(openSound);
        }

        private void CloseDoor()
        {
            if (!isOpen || isMoving) return;

            StartCoroutine(MoveDoor(closedPosition, false));
            PlaySound(closeSound);
        }

        private System.Collections.IEnumerator MoveDoor(Vector3 targetPosition, bool opening)
        {
            isMoving = true;
            Vector3 startPosition = transform.position;
            float elapsed = 0f;
            float duration = Vector3.Distance(startPosition, targetPosition) / openSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
                yield return null;
            }

            transform.position = targetPosition;
            isOpen = opening;
            isMoving = false;
        }

        public void Unlock()
        {
            isLocked = false;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource && clip)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    /// <summary>
    /// Switch that can activate other objects
    /// </summary>
    public class Switch : MonoBehaviour, IInteractable
    {
        [Header("Switch Settings")]
        [SerializeField] private bool isToggle = true;
        [SerializeField] private float activationDuration = 5f;
        [SerializeField] private GameObject[] targetObjects;

        [Header("Visual")]
        [SerializeField] private GameObject onIndicator;
        [SerializeField] private GameObject offIndicator;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip activateSound;
        [SerializeField] private AudioClip deactivateSound;

        private bool isActivated = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            UpdateVisuals();
        }

        public void Interact(PlayerController player)
        {
            if (isToggle)
            {
                ToggleSwitch();
            }
            else
            {
                ActivateTemporary();
            }
        }

        public string GetInteractionPrompt()
        {
            return isActivated ? "Turn Off" : "Turn On";
        }

        public bool CanInteract(PlayerController player)
        {
            return true;
        }

        private void ToggleSwitch()
        {
            isActivated = !isActivated;
            UpdateTargets();
            UpdateVisuals();
            PlaySound(isActivated ? activateSound : deactivateSound);
        }

        private void ActivateTemporary()
        {
            if (isActivated) return;

            isActivated = true;
            UpdateTargets();
            UpdateVisuals();
            PlaySound(activateSound);

            // Deactivate after duration
            Invoke(nameof(Deactivate), activationDuration);
        }

        private void Deactivate()
        {
            isActivated = false;
            UpdateTargets();
            UpdateVisuals();
            PlaySound(deactivateSound);
        }

        private void UpdateTargets()
        {
            foreach (GameObject target in targetObjects)
            {
                if (target)
                {
                    // Try to activate/deactivate different components
                    Door door = target.GetComponent<Door>();
                    if (door)
                    {
                        if (isActivated)
                            door.Unlock();
                    }
                    else
                    {
                        target.SetActive(isActivated);
                    }
                }
            }
        }

        private void UpdateVisuals()
        {
            if (onIndicator)
                onIndicator.SetActive(isActivated);
            
            if (offIndicator)
                offIndicator.SetActive(!isActivated);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource && clip)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    /// <summary>
    /// Treasure chest that can be opened for rewards
    /// </summary>
    public class TreasureChest : MonoBehaviour, IInteractable
    {
        [Header("Chest Settings")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private string requiredKeyId = "";
        [SerializeField] private GameObject[] rewards;
        [SerializeField] private int scoreReward = 500;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string openTrigger = "Open";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip lockedSound;

        private bool isOpened = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (!animator)
                animator = GetComponent<Animator>();
        }

        public void Interact(PlayerController player)
        {
            if (isOpened) return;

            if (isLocked)
            {
                // Check for key (would require inventory system)
                Debug.Log($"Chest is locked - need {requiredKeyId}");
                PlaySound(lockedSound);
                return;
            }

            OpenChest(player);
        }

        public string GetInteractionPrompt()
        {
            if (isOpened)
                return "Empty Chest";
            
            if (isLocked)
                return $"Locked Chest - Need {requiredKeyId}";
            
            return "Open Chest";
        }

        public bool CanInteract(PlayerController player)
        {
            return !isOpened;
        }

        private void OpenChest(PlayerController player)
        {
            isOpened = true;
            
            // Play animation
            if (animator)
                animator.SetTrigger(openTrigger);
            
            PlaySound(openSound);

            // Give rewards
            GiveRewards(player);

            // Add score
            if (Laboratory.Core.GameManager.Instance)
            {
                Laboratory.Core.GameManager.Instance.AddScore(scoreReward);
            }
        }

        private void GiveRewards(PlayerController player)
        {
            foreach (GameObject reward in rewards)
            {
                if (reward)
                {
                    // Spawn reward near chest
                    Vector3 spawnPosition = transform.position + Vector3.up * 1f + Random.insideUnitSphere * 0.5f;
                    Instantiate(reward, spawnPosition, Quaternion.identity);
                }
            }
        }

        public void Unlock()
        {
            isLocked = false;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource && clip)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    /// <summary>
    /// Teleporter that moves player to another location
    /// </summary>
    public class Teleporter : MonoBehaviour, IInteractable
    {
        [Header("Teleporter Settings")]
        [SerializeField] private Transform destination;
        [SerializeField] private bool requiresActivation = true;
        [SerializeField] private float cooldownTime = 2f;

        [Header("Effects")]
        [SerializeField] private GameObject teleportEffect;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip teleportSound;

        private float lastTeleportTime;
        private bool isActivated = true;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            
            if (requiresActivation)
                isActivated = false;
        }

        public void Interact(PlayerController player)
        {
            if (!isActivated || !destination)
            {
                Debug.Log("Teleporter is not activated or has no destination");
                return;
            }

            if (Time.time < lastTeleportTime + cooldownTime)
            {
                Debug.Log("Teleporter is on cooldown");
                return;
            }

            TeleportPlayer(player);
        }

        public string GetInteractionPrompt()
        {
            if (!isActivated)
                return "Inactive Teleporter";
            
            if (!destination)
                return "Broken Teleporter";
            
            if (Time.time < lastTeleportTime + cooldownTime)
                return "Teleporter Charging...";
            
            return "Use Teleporter";
        }

        public bool CanInteract(PlayerController player)
        {
            return isActivated && destination && Time.time >= lastTeleportTime + cooldownTime;
        }

        private void TeleportPlayer(PlayerController player)
        {
            lastTeleportTime = Time.time;

            // Show effect at current position
            if (teleportEffect)
            {
                Instantiate(teleportEffect, player.transform.position, Quaternion.identity);
            }

            PlaySound(teleportSound);

            // Move player
            player.transform.position = destination.position;
            player.transform.rotation = destination.rotation;

            // Show effect at destination
            if (teleportEffect)
            {
                Instantiate(teleportEffect, destination.position, Quaternion.identity);
            }
        }

        public void Activate()
        {
            isActivated = true;
        }

        public void Deactivate()
        {
            isActivated = false;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource && clip)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (destination)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, destination.position);
                Gizmos.DrawWireCube(destination.position, Vector3.one);
            }
        }
    }
}
