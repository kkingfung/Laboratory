using UnityEngine;

namespace Laboratory.Core.Health.Components
{
    /// <summary>
    /// AI-specific health component implementation.
    /// Provides health management for NPCs and AI entities with additional
    /// AI-specific behaviors and integrations.
    /// </summary>
    public class AIHealthComponent : HealthComponentBase
    {
        #region Serialized Fields

        [Header("AI Health Configuration")]
        [SerializeField] private bool _notifyAIOnDamage = true;
        [SerializeField] private bool _notifyAIOnDeath = true;
        [SerializeField] private float _deathDelay = 2f;
        [SerializeField] private bool _destroyOnDeath = true;

        [Header("AI Integration")]
        [SerializeField] private MonoBehaviour[] _aiComponentsToNotify = new MonoBehaviour[0];
        [SerializeField] private GameObject[] _objectsToDisableOnDeath = new GameObject[0];

        #endregion

        #region Health Component Overrides

        public override bool TakeDamage(DamageRequest damageRequest)
        {
            bool damageApplied = base.TakeDamage(damageRequest);

            if (damageApplied && _notifyAIOnDamage)
            {
                NotifyAIComponents("OnTookDamage", damageRequest);
            }

            return damageApplied;
        }

        #endregion

        #region Protected Overrides

        protected override void OnDeathBehavior()
        {
            base.OnDeathBehavior();

            if (_notifyAIOnDeath)
            {
                NotifyAIComponents("OnDeath", null);
            }

            // Disable specified objects
            foreach (var obj in _objectsToDisableOnDeath)
            {
                if (obj != null)
                    obj.SetActive(false);
            }

            // Handle destruction after delay
            if (_destroyOnDeath)
            {
                if (_deathDelay > 0f)
                {
                    Destroy(gameObject, _deathDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        #endregion

        #region Private Methods

        private void NotifyAIComponents(string methodName, object parameter)
        {
            foreach (var aiComponent in _aiComponentsToNotify)
            {
                if (aiComponent == null) continue;

                try
                {
                    var method = aiComponent.GetType().GetMethod(methodName);
                    if (method != null)
                    {
                        if (method.GetParameters().Length == 0)
                        {
                            method.Invoke(aiComponent, null);
                        }
                        else if (parameter != null)
                        {
                            method.Invoke(aiComponent, new object[] { parameter });
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to notify AI component {aiComponent.GetType().Name}: {ex.Message}");
                }
            }
        }

        #endregion

        #region Public API for AI

        /// <summary>
        /// Sets health percentage (0.0 to 1.0). Useful for AI initialization.
        /// </summary>
        public void SetHealthPercentage(float percentage)
        {
            percentage = Mathf.Clamp01(percentage);
            int newHealth = Mathf.RoundToInt(_maxHealth * percentage);
            
            int oldHealth = _currentHealth;
            _currentHealth = newHealth;

            if (oldHealth != newHealth)
            {
                var healthChangedArgs = new HealthChangedEventArgs(oldHealth, newHealth, this);
                OnHealthChanged?.Invoke(healthChangedArgs);
            }
        }

        /// <summary>
        /// Checks if health is below a certain percentage threshold.
        /// </summary>
        public bool IsHealthBelowPercentage(float threshold)
        {
            return HealthPercentage < Mathf.Clamp01(threshold);
        }

        /// <summary>
        /// Gets the AI components that will be notified of health events.
        /// </summary>
        public MonoBehaviour[] GetNotifiedAIComponents()
        {
            return _aiComponentsToNotify;
        }

        #endregion
    }
}
