// FIXME: tidyup after 8/29
public class PlayerDamageListener : MonoBehaviour
{
    [SerializeField] private DamageIndicatorUI damageIndicatorUI = null!;
    private NetworkHealth networkHealth = null!;

    private void Awake()
    {
        networkHealth = GetComponent<NetworkHealth>();
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

    private void OnHealthChanged(int oldVal, int newVal)
    {
        int damageTaken = oldVal - newVal;
        if (damageTaken > 0)
        {
            // Spawn damage indicator with optional parameters
            damageIndicatorUI.SpawnIndicator(
                sourcePosition: transform.position, // Adjust if you have real damage source
                damageAmount: damageTaken,
                damageType: DamageType.Normal,
                playSound: true,
                vibrate: true);
        }
    }
}
