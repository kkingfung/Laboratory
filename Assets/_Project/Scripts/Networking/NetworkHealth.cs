using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();
    public int MaxHealth = 100;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = MaxHealth;
        }
        CurrentHealth.OnValueChanged += OnHealthChanged;
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        // You can hook UI update or effects here on clients
        Debug.Log($"Health changed from {oldValue} to {newValue}");
    }

    public void ApplyDamage(int amount)
    {
        if (!IsServer) return;

        CurrentHealth.Value = math.clamp(CurrentHealth.Value - amount, 0, MaxHealth);

        if (CurrentHealth.Value == 0)
        {
            // Trigger death logic, broadcast event, etc.
        }
    }
}
