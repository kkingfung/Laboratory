using Unity.Netcode;
// FIXME: tidyup after 8/29
public enum LifeState : byte
{
    Alive,
    Dead
}

public class NetworkLifeState : NetworkBehaviour
{
    public NetworkVariable<LifeState> CurrentState = new NetworkVariable<LifeState>(
        LifeState.Alive,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> RespawnTimeRemaining = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool IsAlive => CurrentState.Value == LifeState.Alive;
}
