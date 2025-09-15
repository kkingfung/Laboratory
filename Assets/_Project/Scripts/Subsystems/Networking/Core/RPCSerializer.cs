using System;
using System.IO;
using System.Text;
using Laboratory.Core;
using Laboratory.Core.State;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Provides serialization and deserialization functionality for RPC messages.
    /// Handles binary encoding/decoding of network communication data with efficient formatting.
    /// </summary>
    public static class RPCSerializer
    {
        #region Enums

        /// <summary>
        /// Defines the types of RPC messages that can be serialized and transmitted.
        /// </summary>
        public enum RPCType : byte
        {
            /// <summary>No message type specified.</summary>
            None = 0,
            /// <summary>Player attack action message.</summary>
            PlayerAttack = 1,
            /// <summary>Player movement update message.</summary>
            PlayerMove = 2,
            /// <summary>Player jump action message.</summary>
            PlayerJump = 3,
            /// <summary>Chat message between players.</summary>
            ChatMessage = 4,
            /// <summary>Game state synchronization message.</summary>
            GameStateSync = 5
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Serializes a player attack RPC with target and damage information.
        /// </summary>
        /// <param name="playerId">ID of the attacking player.</param>
        /// <param name="targetId">ID of the target player.</param>
        /// <param name="damage">Amount of damage to apply.</param>
        /// <returns>Serialized byte array representing the attack RPC.</returns>
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
        /// Serializes a player movement RPC with position and direction vectors.
        /// </summary>
        /// <param name="playerId">ID of the moving player.</param>
        /// <param name="posX">X position coordinate.</param>
        /// <param name="posY">Y position coordinate.</param>
        /// <param name="posZ">Z position coordinate.</param>
        /// <param name="dirX">X direction component.</param>
        /// <param name="dirY">Y direction component.</param>
        /// <param name="dirZ">Z direction component.</param>
        /// <returns>Serialized byte array representing the movement RPC.</returns>
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
        /// Serializes a player jump action RPC.
        /// </summary>
        /// <param name="playerId">ID of the jumping player.</param>
        /// <returns>Serialized byte array representing the jump RPC.</returns>
        public static byte[] SerializePlayerJump(int playerId)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write((byte)RPCType.PlayerJump);
            writer.Write(playerId);

            return ms.ToArray();
        }

        /// <summary>
        /// Serializes a chat message RPC with sender and message content.
        /// </summary>
        /// <param name="playerId">ID of the message sender.</param>
        /// <param name="message">Text content of the message.</param>
        /// <returns>Serialized byte array representing the chat message RPC.</returns>
        /// <exception cref="ArgumentException">Thrown when message is too long to serialize.</exception>
        public static byte[] SerializeChatMessage(int playerId, string message)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write((byte)RPCType.ChatMessage);
            writer.Write(playerId);
            WriteString(writer, message);

            return ms.ToArray();
        }

        /// <summary>
        /// Serializes a game state synchronization message.
        /// </summary>
        /// <param name="state">Game state to synchronize.</param>
        /// <returns>Serialized byte array representing the game state sync RPC.</returns>
        public static byte[] SerializeGameState(GameState state)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write((byte)RPCType.GameStateSync);
            writer.Write((byte)state);

            return ms.ToArray();
        }

        #endregion

        #region Deserialization Methods

        /// <summary>
        /// Parses an RPC message from byte array and extracts type and data.
        /// </summary>
        /// <param name="data">Raw message bytes to deserialize.</param>
        /// <param name="rpcType">Output RPC type extracted from message.</param>
        /// <param name="rpcData">Output RPC data object containing parsed information.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        public static bool TryDeserialize(byte[] data, out RPCType rpcType, out object? rpcData)
        {
            rpcType = RPCType.None;
            rpcData = null;

            if (data == null || data.Length == 0)
            {
                Debug.LogWarning("RPCSerializer: Cannot deserialize null or empty data");
                return false;
            }

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
                            Position = new float3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                            Direction = new float3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
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

                    case RPCType.GameStateSync:
                        // Handle game state sync separately
                        ms.Position = 1; // Reset to after RPC type byte
                        rpcData = (GameState)reader.ReadByte();
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

        /// <summary>
        /// Attempts to deserialize a game state synchronization message.
        /// </summary>
        /// <param name="data">Raw message bytes containing game state.</param>
        /// <param name="state">Output game state if deserialization succeeds.</param>
        /// <returns>True if game state was successfully extracted; otherwise, false.</returns>
        public static bool TryDeserializeGameState(byte[] data, out GameState state)
        {
            state = GameState.None;

            if (data == null || data.Length < 2)
            {
                return false;
            }

            try
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms);

                var rpcType = (RPCType)reader.ReadByte();
                if (rpcType != RPCType.GameStateSync)
                {
                    return false;
                }

                state = (GameState)reader.ReadByte();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"RPCSerializer: Failed to deserialize game state - {ex}");
                return false;
            }
        }

        #endregion

        #region Private Utility Methods

        /// <summary>
        /// Writes a string to the binary writer with length prefix.
        /// </summary>
        /// <param name="writer">Binary writer to write to.</param>
        /// <param name="value">String value to write (null strings are written as empty).</param>
        /// <exception cref="ArgumentException">Thrown when string is too long to serialize.</exception>
        private static void WriteString(BinaryWriter writer, string? value)
        {
            if (value == null)
            {
                writer.Write((ushort)0);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > ushort.MaxValue)
            {
                throw new ArgumentException($"String too long to serialize: {bytes.Length} bytes (max: {ushort.MaxValue})");
            }

            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }

        /// <summary>
        /// Reads a length-prefixed string from the binary reader.
        /// </summary>
        /// <param name="reader">Binary reader to read from.</param>
        /// <returns>Decoded UTF-8 string, or empty string if length is zero.</returns>
        private static string ReadString(BinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            if (length == 0) return string.Empty;

            var bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        #endregion

        #region RPC Data Classes

        /// <summary>
        /// Data container for player attack RPC messages.
        /// </summary>
        public class PlayerAttackData
        {
            /// <summary>ID of the attacking player.</summary>
            public int PlayerId;
            /// <summary>ID of the target player.</summary>
            public int TargetId;
            /// <summary>Amount of damage to apply.</summary>
            public int Damage;
        }

        /// <summary>
        /// Data container for player movement RPC messages.
        /// </summary>
        public class PlayerMoveData
        {
            /// <summary>ID of the moving player.</summary>
            public int PlayerId;
            /// <summary>New position coordinates.</summary>
            public float3 Position;
            /// <summary>Movement direction vector.</summary>
            public float3 Direction;
        }

        /// <summary>
        /// Data container for player jump RPC messages.
        /// </summary>
        public class PlayerJumpData
        {
            /// <summary>ID of the jumping player.</summary>
            public int PlayerId;
        }

        /// <summary>
        /// Data container for chat message RPC messages.
        /// </summary>
        public class ChatMessageData
        {
            /// <summary>ID of the message sender.</summary>
            public int PlayerId;
            /// <summary>Text content of the message.</summary>
            public string Message = string.Empty;
        }

        #endregion
    }
}
