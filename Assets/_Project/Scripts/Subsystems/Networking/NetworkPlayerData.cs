using System;
using UnityEngine;

namespace Laboratory.Networking
{
    /// <summary>
    /// Network player data for multiplayer functionality
    /// </summary>
    [Serializable]
    public class NetworkPlayerData
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float Health { get; set; }
        public int Level { get; set; }
        public bool IsAlive { get; set; }
        public float LastUpdateTime { get; set; }

        public NetworkPlayerData()
        {
            PlayerId = Guid.NewGuid().ToString();
            PlayerName = "Player";
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Health = 100f;
            Level = 1;
            IsAlive = true;
            LastUpdateTime = Time.time;
        }

        public NetworkPlayerData(string playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Health = 100f;
            Level = 1;
            IsAlive = true;
            LastUpdateTime = Time.time;
        }

        public void UpdatePosition(Vector3 newPosition, Quaternion newRotation)
        {
            Position = newPosition;
            Rotation = newRotation;
            LastUpdateTime = Time.time;
        }

        public void UpdateHealth(float newHealth)
        {
            Health = Mathf.Clamp(newHealth, 0f, 100f);
            IsAlive = Health > 0f;
            LastUpdateTime = Time.time;
        }

        public byte[] Serialize()
        {
            // Simple serialization - in a real implementation, use a proper serializer
            var json = JsonUtility.ToJson(this);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public static NetworkPlayerData Deserialize(byte[] data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<NetworkPlayerData>(json);
        }
    }
}
