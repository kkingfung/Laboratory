// RespawnManager.cs
using System.Collections.Generic;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [SerializeField] private List<Transform> respawnPoints = new List<Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public Vector3 GetRespawnPosition(ulong clientId)
    {
        // Basic strategy: round-robin or random spawn. Use clientId to vary.
        if (respawnPoints == null || respawnPoints.Count == 0) return Vector3.zero;
        int idx = (int)(clientId % (ulong)respawnPoints.Count);
        return respawnPoints[idx].position;
    }
}
