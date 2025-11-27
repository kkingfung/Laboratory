# Project Chimera Multiplayer Workflow Guide

**Created:** 2024-11-27
**Purpose:** Complete guide for implementing and using multiplayer features in Project Chimera

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Quick Start](#quick-start)
4. [Network Modes](#network-modes)
5. [Matchmaking & Session Management](#matchmaking--session-management)
6. [Creature Synchronization](#creature-synchronization)
7. [Player Data & State](#player-data--state)
8. [Save Data Synchronization](#save-data-synchronization)
9. [Testing Multiplayer](#testing-multiplayer)
10. [Troubleshooting](#troubleshooting)

---

## Overview

Project Chimera supports multiplayer creature breeding and battles through **Unity Netcode for Entities** (DOTS-based) and **Unity Netcode for GameObjects** (traditional GameObject-based).

### Infrastructure Status

✅ **Implemented:**
- Network bootstrapping and initialization
- Player connection/disconnection handling
- Creature spawn synchronization
- Basic state replication
- Network debugging tools

⚠️ **Requires Implementation:**
- Matchmaking UI
- Breeding synchronization logic
- Full save data sync
- Chat system integration

---

## Architecture

### Two-Layer Network Stack

```
┌─────────────────────────────────────────┐
│         Application Layer               │
│  (Breeding, Combat, Trading)            │
└─────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────┐
│       Network Abstraction Layer         │
│  ChimeraNetworkBootstrapper (ECS)       │
│  ChimeraNetworkManager (GameObject)     │
└─────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────┐
│      Unity Netcode Transport            │
│  (UDP/WebSockets/Relay)                 │
└─────────────────────────────────────────┘
```

### Key Components

| Component | Purpose | Type |
|-----------|---------|------|
| **ChimeraNetworkBootstrapper** | ECS world networking setup | DOTS |
| **ChimeraNetworkManager** | GameObject networking setup | MonoBehaviour |
| **NetworkSyncSystemSimple** | Entity state synchronization | ECS System |
| **NetworkPlayerData** | Player session tracking | Data Structure |
| **NetworkEntityMapper** | Entity ID mapping (local ↔ network) | Service |

---

## Quick Start

### Method 1: ECS-Based Setup (Recommended for Performance)

**For high-performance scenarios (1000+ creatures)**

1. **Add Network Bootstrapper to Scene:**
   ```
   Hierarchy:
   └── ChimeraNetworkBootstrapper
       ├── ChimeraGameConfig (assigned)
       ├── NetcodeConfiguration (assigned)
       └── PlayerProgressionConfig (assigned)
   ```

2. **Configure Settings:**
   - **Auto Start Server:** `true` for host mode
   - **Dedicated Server:** `false` for peer-to-peer, `true` for dedicated
   - **Server Port:** `7979` (default)
   - **Max Players:** `20` (adjust as needed)
   - **Spawn Test Creatures:** `true` for testing

3. **Hit Play:**
   - Server starts automatically
   - Test creatures spawn if enabled
   - Network stats appear in console (if enabled)

---

### Method 2: GameObject-Based Setup

**For traditional Unity workflows**

1. **Add Network Manager to Scene:**
   ```csharp
   GameObject networkManager = new GameObject("NetworkManager");
   networkManager.AddComponent<ChimeraNetworkManager>();
   ```

2. **Configure in Inspector:**
   - **Auto Start Network:** `true`
   - **Max Players:** `50`
   - **Server IP:** `"127.0.0.1"` (localhost) or actual IP
   - **Server Port:** `7777`
   - **Max Monsters Per Player:** `20`

3. **Start Network via Code:**
   ```csharp
   // As host (server + client)
   ChimeraNetworkManager.Instance.StartHost();

   // As dedicated server
   ChimeraNetworkManager.Instance.StartServer();

   // As client
   ChimeraNetworkManager.Instance.StartClient("192.168.1.100", 7777);
   ```

---

## Network Modes

### Host Mode (Peer-to-Peer)

**Use Case:** Small groups, local multiplayer, testing

**Setup:**
```csharp
// ChimeraNetworkBootstrapper
enableDedicatedServer = false;
autoStartServer = true;
```

**Characteristics:**
- One player acts as server + client
- Low latency for host
- Host leaving ends session
- Suitable for 2-10 players

---

### Dedicated Server Mode

**Use Case:** Large-scale multiplayer, public servers

**Setup:**
```csharp
// ChimeraNetworkBootstrapper
enableDedicatedServer = true;
autoStartServer = true;
serverPort = 7979;
maxPlayers = 100;
```

**Characteristics:**
- Dedicated server machine
- All clients have equal latency
- Server can run headless (no graphics)
- Persistent sessions

---

### Client Mode

**Use Case:** Joining existing sessions

**Setup:**
```csharp
// Via code
ChimeraNetworkManager.Instance.StartClient("192.168.1.100", 7979);
```

**Characteristics:**
- Connects to existing server
- Receives state from server
- Sends input to server

---

## Matchmaking & Session Management

### Current Implementation

The infrastructure exists but requires UI hookup:

```csharp
// Listen for player connections
ChimeraNetworkManager.OnPlayerJoined += (playerId) =>
{
    Debug.Log($"Player {playerId} joined!");
    // TODO: Update lobby UI
    // TODO: Sync player data
};

ChimeraNetworkManager.OnPlayerLeft += (playerId) =>
{
    Debug.Log($"Player {playerId} left!");
    // TODO: Update lobby UI
    // TODO: Clean up player data
};
```

### Implementing Matchmaking UI

**Step 1: Create Lobby UI**

Create UI prefab with:
- Server IP input field
- Server port input field
- "Host Game" button
- "Join Game" button
- Player list display

**Step 2: Hook Up UI Events**

```csharp
public class MultiplayerLobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField serverIPInput;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Transform playerListContainer;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        // Subscribe to network events
        ChimeraNetworkManager.OnPlayerJoined += UpdatePlayerList;
        ChimeraNetworkManager.OnPlayerLeft += UpdatePlayerList;
    }

    private void OnHostClicked()
    {
        ChimeraNetworkManager.Instance.StartHost();
        // Transition to game scene or lobby
    }

    private void OnJoinClicked()
    {
        string ip = serverIPInput.text;
        ushort port = ushort.Parse(portInput.text);

        ChimeraNetworkManager.Instance.StartClient(ip, port);
    }

    private void UpdatePlayerList(ulong playerId)
    {
        // Update UI player list
        // TODO: Implement player list UI update
    }
}
```

**Step 3: Session Configuration**

```csharp
// Configure session settings
var sessionConfig = new MultiplayerSessionConfig
{
    MaxPlayers = 20,
    AllowLateJoin = true,
    EnableVoiceChat = false,
    SessionName = "Chimera Breeding World 1",
    Password = "" // Optional
};

ChimeraNetworkManager.Instance.CreateSession(sessionConfig);
```

---

## Creature Synchronization

### ECS Creature Sync (Automatic)

The `NetworkSyncSystemSimple` handles automatic creature synchronization:

**What's Synchronized:**
- Position and rotation
- Health and status effects
- Behavior state (idle, wandering, attacking, etc.)
- Ownership (which player owns the creature)

**Usage:**
```csharp
// Creatures with these components are automatically synced:
// - LocalToWorld (position/rotation)
// - CreatureHealthComponent (health)
// - BehaviorStateComponent (AI state)
// - NetworkEntityComponent (network ID)

// Server spawns creature
Entity creatureEntity = entityManager.CreateEntity();
entityManager.AddComponentData(creatureEntity, new NetworkEntityComponent
{
    NetworkId = GenerateNetworkId(),
    OwnerId = playerId,
    IsServerAuthoritative = true
});

// Clients automatically receive and replicate
```

### Manual Creature Sync

For custom creature data:

```csharp
// Define network message
[System.Serializable]
public struct CreatureGeneticsSyncMessage : INetworkMessage
{
    public ulong NetworkId;
    public int Generation;
    public float[] GeneValues; // Genetic trait values
}

// Server: Broadcast genetics update
public void SyncCreatureGenetics(ulong creatureNetworkId, GeneticProfile genetics)
{
    var message = new CreatureGeneticsSyncMessage
    {
        NetworkId = creatureNetworkId,
        Generation = genetics.Generation,
        GeneValues = genetics.GetGeneValues()
    };

    NetworkMessageHandler.Instance.Broadcast(message);
}

// Client: Receive and apply
NetworkMessageHandler.Instance.Register<CreatureGeneticsSyncMessage>(OnGeneticsSyncReceived);

void OnGeneticsSyncReceived(CreatureGeneticsSyncMessage message)
{
    Entity creature = NetworkEntityMapper.GetLocalEntity(message.NetworkId);
    if (creature != Entity.Null)
    {
        // Apply genetics to local creature
        ApplyGeneticsToCreature(creature, message.GeneValues);
    }
}
```

---

## Player Data & State

### Player Data Structure

```csharp
public class ChimeraPlayerData
{
    public ulong PlayerId;
    public string PlayerName;
    public int PlayerLevel;
    public List<uint> OwnedCreatureIds;
    public int Currency;
    public Vector3 LastPosition;
    public bool IsReady;
}
```

### Syncing Player State

**Server Authority Pattern:**

```csharp
// Server: Update player state
public void UpdatePlayerCurrency(ulong playerId, int newCurrency)
{
    if (!IsServer) return;

    var playerData = GetPlayerData(playerId);
    playerData.Currency = newCurrency;

    // Broadcast to all clients
    BroadcastPlayerStateUpdate(playerData);
}

// Client: Request state change
public void RequestPurchase(int itemCost)
{
    SendMessageToServer(new PurchaseRequestMessage
    {
        PlayerId = LocalPlayerId,
        ItemCost = itemCost
    });
}
```

---

## Save Data Synchronization

### Current Limitation

⚠️ **Save synchronization is NOT fully implemented.**
Each client maintains local save files. Multiplayer sessions are ephemeral.

### Implementing Cloud Save Sync

**Option 1: Server-Side Save (Recommended)**

```csharp
// Server maintains authoritative save
public void SavePlayerDataToServer(ulong playerId, GameStateSaveData saveData)
{
    string savePath = $"Server/Saves/Player_{playerId}.json";
    SaveGameState(savePath, saveData);

    // Broadcast save confirmation
    SendMessageToClient(playerId, new SaveConfirmationMessage());
}

// Client requests save
public void RequestServerSave()
{
    var saveData = GenerateCurrentSaveData();
    SendMessageToServer(new SaveRequestMessage
    {
        PlayerId = LocalPlayerId,
        SaveData = saveData
    });
}
```

**Option 2: Cloud Storage Integration**

```csharp
// Use Unity Cloud Save or third-party
public async Task SaveToCloud(GameStateSaveData saveData)
{
    // Example using Unity Cloud Save
    await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
    {
        { "playerProgress", JsonUtility.ToJson(saveData) }
    });
}
```

---

## Testing Multiplayer

### Local Testing (Same Machine)

**Option 1: ParrelSync (Unity Editor Cloning)**

1. Install **ParrelSync** from Package Manager
2. Create clone project: `Tools > ParrelSync > Clones Manager`
3. Open clone in separate Unity Editor window
4. **Editor 1:** Start as Host
5. **Editor 2:** Start as Client (connect to `127.0.0.1:7979`)

**Option 2: Build + Editor**

1. Build the game (`File > Build and Run`)
2. Run build as Client
3. Editor runs as Host

---

### Network Simulation

Use **Network Performance Simulator** for lag/packet loss testing:

```csharp
// Access via: Tools → Chimera → Network Performance Simulator

// Simulate lag
NetworkPerformanceSimulator.SetLatency(100); // 100ms ping

// Simulate packet loss
NetworkPerformanceSimulator.SetPacketLoss(0.05f); // 5% packet loss

// Simulate bandwidth limit
NetworkPerformanceSimulator.SetBandwidthLimit(1024); // 1MB/s
```

---

### Debugging Network Issues

**Enable Detailed Logging:**

```csharp
// ChimeraNetworkBootstrapper
enableDetailedLogging = true;
showNetworkStats = true;
```

**Use Network Debugger:**

```csharp
// Access via: Window → Chimera → Network Synchronization Debugger

// Shows:
// - Connected players
// - Network entity count
// - Bandwidth usage
// - Sync errors
```

---

## Troubleshooting

### "Cannot connect to server"

**Possible Causes:**
- ✅ Firewall blocking port 7979
- ✅ Incorrect server IP address
- ✅ Server not started
- ✅ NAT/router blocking connections

**Solutions:**
1. Check firewall settings (allow port 7979)
2. Verify server IP with `ipconfig` (Windows) or `ifconfig` (Mac/Linux)
3. Ensure server is running (check console for "Server Started")
4. For remote play, set up port forwarding on router

---

### "Creatures not syncing"

**Possible Causes:**
- ✅ Missing `NetworkEntityComponent` on creatures
- ✅ Sync system not running
- ✅ Bandwidth limit reached

**Solutions:**
1. Verify creatures have `NetworkEntityComponent`
2. Check `NetworkSyncSystemSimple` is in update list
3. Reduce creature count or sync frequency
4. Check console for sync errors

---

### "Player disconnects unexpectedly"

**Possible Causes:**
- ✅ Connection timeout
- ✅ Network instability
- ✅ Server crash

**Solutions:**
1. Increase `connectionTimeout` in ChimeraNetworkManager (default: 30s)
2. Implement reconnection logic:
   ```csharp
   ChimeraNetworkManager.OnNetworkError += (error) =>
   {
       if (error.Contains("timeout"))
       {
           AttemptReconnect();
       }
   };
   ```
3. Check server console for errors

---

### "Save data not persisting across sessions"

**Known Limitation:**
Multiplayer sessions currently use ephemeral state. Save data is NOT synchronized.

**Workaround:**
Implement server-side save as described in [Save Data Synchronization](#save-data-synchronization).

---

## Implementation Roadmap

### Phase 1: Core Multiplayer (✅ COMPLETE)
- [x] Network bootstrapping
- [x] Player connection handling
- [x] Basic creature synchronization
- [x] Network debugging tools

### Phase 2: Gameplay Features (⚠️ IN PROGRESS)
- [ ] Breeding synchronization
- [ ] Battle synchronization
- [ ] Trading system
- [ ] Chat system

### Phase 3: Persistence (🚧 PLANNED)
- [ ] Server-side save storage
- [ ] Cloud save integration
- [ ] Save migration for multiplayer
- [ ] Session persistence

### Phase 4: Advanced Features (🚧 PLANNED)
- [ ] Matchmaking UI
- [ ] Lobby system
- [ ] Voice chat integration
- [ ] Leaderboards
- [ ] Tournaments

---

## Example: Complete Multiplayer Session Flow

### Host Player Workflow

```csharp
// 1. Start host
ChimeraNetworkBootstrapper bootstrapper = FindFirstObjectByType<ChimeraNetworkBootstrapper>();
bootstrapper.autoStartServer = true;
bootstrapper.enableDedicatedServer = false;
bootstrapper.maxPlayers = 10;

// 2. Wait for players to join
ChimeraNetworkManager.OnPlayerJoined += (playerId) =>
{
    Debug.Log($"Player {playerId} joined the breeding world!");
    UpdateLobbyUI();
};

// 3. Start gameplay when ready
public void OnAllPlayersReady()
{
    BroadcastMessage(new GameStartMessage());
    TransitionToGameplay();
}

// 4. Handle creature breeding
public void OnCreaturesBred(Entity parent1, Entity parent2)
{
    // Server spawns offspring
    Entity offspring = BreedingSystem.CreateOffspring(parent1, parent2);

    // Automatically synced to all clients via NetworkSyncSystemSimple
}

// 5. Save session data
public void OnSessionEnd()
{
    foreach (var playerId in connectedPlayers.Keys)
    {
        SavePlayerDataToServer(playerId);
    }
}
```

### Client Player Workflow

```csharp
// 1. Connect to server
public void JoinServer(string ip, ushort port)
{
    ChimeraNetworkManager.Instance.StartClient(ip, port);
}

// 2. Receive welcome message
NetworkMessageHandler.Instance.Register<WelcomeMessage>((msg) =>
{
    DisplayWelcomeUI(msg.ServerName, msg.PlayerCount);
});

// 3. Sync local creatures
ChimeraNetworkManager.OnPlayerJoined += (playerId) =>
{
    if (playerId == LocalPlayerId)
    {
        RequestCreatureSync();
    }
};

// 4. Participate in breeding
public void RequestBreeding(uint creatureId1, uint creatureId2)
{
    SendMessageToServer(new BreedingRequestMessage
    {
        Parent1Id = creatureId1,
        Parent2Id = creatureId2
    });
}

// 5. Receive offspring
NetworkMessageHandler.Instance.Register<OffspringSpawnedMessage>((msg) =>
{
    DisplayNewCreatureNotification(msg.OffspringData);
});
```

---

## API Reference

### ChimeraNetworkBootstrapper

```csharp
public class ChimeraNetworkBootstrapper : MonoBehaviour
{
    // Configuration
    public ChimeraGameConfig gameConfig;
    public NetcodeConfiguration netcodeConfig;
    public bool autoStartServer;
    public ushort serverPort;
    public int maxPlayers;

    // Methods
    public void InitializeChimeraNetworking();
    public void StartNetworkServer();
    public void ShutdownNetwork();

    // Events
    public event Action OnNetworkInitialized;
    public event Action OnNetworkShutdown;
}
```

### ChimeraNetworkManager

```csharp
public class ChimeraNetworkManager : MonoBehaviour
{
    // Methods
    public void StartHost();
    public void StartServer();
    public void StartClient(string ip, ushort port);
    public void Disconnect();

    // Events
    public static event Action<bool> OnNetworkStatusChanged;
    public static event Action<ulong> OnPlayerJoined;
    public static event Action<ulong> OnPlayerLeft;
    public static event Action<string> OnNetworkError;

    // Properties
    public bool IsServer { get; }
    public bool IsClient { get; }
    public ulong LocalPlayerId { get; }
}
```

---

## Additional Resources

- **Unity Netcode for Entities Docs:** https://docs.unity3d.com/Packages/com.unity.netcode@latest
- **Unity Netcode for GameObjects:** https://docs-multiplayer.unity3d.com/
- **Project Chimera Architecture:** See `ARCHITECTURE.md`
- **Network Performance Testing:** `Assets/_Project/Scripts/Tools/NetworkTrafficAnalyzer.cs`

---

**Last Updated:** 2024-11-27
**Maintained By:** Project Chimera Network Team
**Related Docs:** ARCHITECTURE.md, SYSTEM_INITIALIZATION_ORDER.md, DEVELOPER_GUIDE.md
