using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class NetworkPlayerData : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(default, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    
    public NetworkVariable<Sprite> Avatar = new NetworkVariable<Sprite>(null, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> Kills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> Deaths = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> Assists = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> Ping = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);

    [ServerRpc(RequireOwnership = false)]
    public void SetScoreServerRpc(int newScore) => Score.Value = newScore;

    [ServerRpc(RequireOwnership = false)]
    public void SetPingServerRpc(int ping) => Ping.Value = ping;

    // Avatar setting usually done on server
}
