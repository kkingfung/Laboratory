using System;
using System.IO;
using System.Text;
using UnityEngine;

#nullable enable

namespace Infrastructure
{
    /// <summary>
    /// Serializes and deserializes RPC messages for network communication.
    /// Example format:
    /// [MessageType (byte)][Payload...]
    /// </summary>
    public static class RPCSerializer
    {
        #region RPC Message Types

        public enum RPCType : byte
        {
            None = 0,
            PlayerAttack = 1,
            PlayerMove = 2,
            PlayerJump = 3,
            ChatMessage = 4,
            // Add more RPC types here
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serialize an RPC call with data payload.
        /// </summary>
        public static byte[] SerializePlayerAttack(int playerId, int targetId, int damage)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write((byte)RPCType.PlayerAttack);
            writer.Write(playerId);
            writer.Write(targetId);
            writer.Write(damage);

            return ms.ToArray();
        }

        /// <summary>
        /// Serialize a player move RPC with position and direction.
        /// </summary>
        public static byte[] SerializePlayerMove(int playerId, float posX, float posY, float posZ, float dirX, float dirY, float dirZ)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write((byte)RPCType.PlayerMove);
            writer.Write(playerId);
            writer.Write(posX);
            writer.Write(posY);
            writer.Write(posZ);
            writer.Write(dirX);
            writer.Write(dirY);
            writer.Write(dirZ);

            return ms.ToArray();
        }

        /// <summary>
        /// Serialize a player jump RPC.
        /// </summary>
        public static byte[] SerializePlayerJump(int playerId)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write((byte)RPCType.PlayerJump);
            writer.Write(playerId);

            return ms.ToArray();
        }

        /// <summary>
        /// Serialize a chat message RPC.
        /// </summary>
        public static byte[] SerializeChatMessage(int playerId, string message)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write((byte)RPCType.ChatMessage);
            writer.Write(playerId);
            WriteString(writer, message);

            return ms.ToArray();
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            if (value == null)
            {
                writer.Write((ushort)0);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > ushort.MaxValue)
                throw new ArgumentException("String too long to serialize");

            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }

        #endregion

        #region Deserialization

        /// <summary>
        /// Parses an RPC message from bytes.
        /// </summary>
        public static bool TryDeserialize(byte[] data, out RPCType rpcType, out object? rpcData)
        {
            rpcType = RPCType.None;
            rpcData = null;

            try
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms);

                rpcType = (RPCType)reader.ReadByte();

                switch (rpcType)
                {
                    case RPCType.PlayerAttack:
                        rpcData = new PlayerAttackData
                        {
                            PlayerId = reader.ReadInt32(),
                            TargetId = reader.ReadInt32(),
                            Damage = reader.ReadInt32()
                        };
                        break;

                    case RPCType.PlayerMove:
                        rpcData = new PlayerMoveData
                        {
                            PlayerId = reader.ReadInt32(),
                            Position = new Unity.Mathematics.float3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                            Direction = new Unity.Mathematics.float3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
                        };
                        break;

                    case RPCType.PlayerJump:
                        rpcData = new PlayerJumpData
                        {
                            PlayerId = reader.ReadInt32()
                        };
                        break;

                    case RPCType.ChatMessage:
                        rpcData = new ChatMessageData
                        {
                            PlayerId = reader.ReadInt32(),
                            Message = ReadString(reader)
                        };
                        break;

                    default:
                        Debug.LogWarning($"RPCSerializer: Unknown RPC type {rpcType}");
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"RPCSerializer: Failed to deserialize RPC - {ex}");
                return false;
            }
        }

        public static byte[] SerializeGameState(GameStateManager.GameState state)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write((byte)RPCType.GameStateSync);
            writer.Write((byte)state);

            return ms.ToArray();
        }

        public static bool TryDeserializeGameState(byte[] data, out GameStateManager.GameState state)
        {
            state = GameStateManager.GameState.None;
            try
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms);

                var rpcType = (RPCType)reader.ReadByte();
                if (rpcType != RPCType.GameStateSync) return false;

                state = (GameStateManager.GameState)reader.ReadByte();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ReadString(BinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            if (length == 0) return string.Empty;

            var bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        #endregion

        #region RPC Data Classes

        public class PlayerAttackData
        {
            public int PlayerId;
            public int TargetId;
            public int Damage;
        }

        public class PlayerMoveData
        {
            public int PlayerId;
            public Unity.Mathematics.float3 Position;
            public Unity.Mathematics.float3 Direction;
        }

        public class PlayerJumpData
        {
            public int PlayerId;
        }

        public class ChatMessageData
        {
            public int PlayerId;
            public string Message = string.Empty;
        }

        #endregion
    }
}
