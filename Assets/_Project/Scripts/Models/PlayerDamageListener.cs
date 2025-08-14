using UnityEngine;
using Laboratory.Networking;
using Laboratory.UI.Helper;

namespace Laboratory.Models
{
    /// <summary>
    /// Listens to player damage events and triggers appropriate UI feedback.
    /// </summary>
    public class PlayerDamageListener : MonoBehaviour
    {
        #region Fields

        [SerializeField] private DamageIndicatorUI damageIndicatorUI = null!;
        private NetworkHealth networkHealth = null!;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            networkHealth = GetComponent<NetworkHealth>();
            if (networkHealth == null)
            {
                Debug.LogError($"NetworkHealth component not found on {gameObject.name}");
            }
        }

        private void OnEnable()
        {
            if (networkHealth != null)
                networkHealth.CurrentHealth.OnValueChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            if (networkHealth != null)
                networkHealth.CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles health value changes and triggers damage indicators.
        /// </summary>
        /// <param name="oldVal">Previous health value.</param>
        /// <param name="newVal">New health value.</param>
        private void OnHealthChanged(int oldVal, int newVal)
        {
            int damageTaken = oldVal - newVal;
            if (damageTaken > 0 && damageIndicatorUI != null)
            {
                damageIndicatorUI.SpawnIndicator(
                    sourcePosition: transform.position,
                    damageAmount: damageTaken,
                    damageType: DamageType.Normal,
                    playSound: true,
                    vibrate: true);
            }
        }

        #endregion

        #region Inner Classes, Enums

        // No inner classes or enums currently.

        #endregion
    }
}
