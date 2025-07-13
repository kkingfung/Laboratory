// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public string PlayerID { get; private set; }
    public int SkillLevel { get; private set; }

    public Player(string playerId, int skillLevel)
    {
        PlayerID = playerId;
        SkillLevel = skillLevel;
    }
}

public class Match
{
    public List<Player> Players { get; private set; } = new List<Player>();
    public int MaxPlayers { get; private set; }

    public Match(int maxPlayers)
    {
        MaxPlayers = maxPlayers;
    }

    public bool AddPlayer(Player player)
    {
        if (Players.Count < MaxPlayers)
        {
            Players.Add(player);
            return true;
        }
        return false;
    }

    public bool IsFull()
    {
        return Players.Count >= MaxPlayers;
    }
}

public class MatchmakingSystem : MonoBehaviour
{
    public int MaxPlayersPerMatch = 4;
    public int SkillVariance = 5;

    private List<Player> waitingPlayers = new List<Player>();
    private List<Match> activeMatches = new List<Match>();

    public void AddPlayerToQueue(Player player)
    {
        waitingPlayers.Add(player);
        TryCreateMatch(player);
    }

    private void TryCreateMatch(Player player)
    {
        Match potentialMatch = null;

        foreach (var match in activeMatches)
        {
            if (!match.IsFull() && IsSkillCompatible(player, match))
            {
                potentialMatch = match;
                break;
            }
        }

        if (potentialMatch == null)
        {
            potentialMatch = new Match(MaxPlayersPerMatch);
            activeMatches.Add(potentialMatch);
        }

        if (potentialMatch.AddPlayer(player))
        {
            waitingPlayers.Remove(player);

            if (potentialMatch.IsFull())
            {
                StartMatch(potentialMatch);
            }
        }
    }

    private bool IsSkillCompatible(Player player, Match match)
    {
        foreach (var matchPlayer in match.Players)
        {
            if (Mathf.Abs(matchPlayer.SkillLevel - player.SkillLevel) > SkillVariance)
            {
                return false;
            }
        }
        return true;
    }

    private void StartMatch(Match match)
    {
        Debug.Log("Match started with players:");
        foreach (var player in match.Players)
        {
            Debug.Log(player.PlayerID);
        }
        // Additional match start logic (e.g., load scene, notify players, etc.)
    }
}