using UnityEngine;
using Mirror;

namespace ProjectV3.Shared.Data
{
    /// <summary>
    /// Oyuncu durumunu temsil eden enum
    /// </summary>
    public enum PlayerState
    {
        Idle,
        Moving,
        Attacking,
        Defending,
        Dead
    }

    /// <summary>
    /// Ağ üzerinden senkronize edilecek oyuncu verisi
    /// </summary>
    public struct PlayerNetworkData : NetworkMessage
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public PlayerState CurrentState { get; set; }
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteInt(PlayerId);
            writer.WriteString(PlayerName);
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteInt((int)CurrentState);
            writer.WriteFloat(CurrentHealth);
            writer.WriteFloat(MaxHealth);
        }

        public void Deserialize(NetworkReader reader)
        {
            PlayerId = reader.ReadInt();
            PlayerName = reader.ReadString();
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            CurrentState = (PlayerState)reader.ReadInt();
            CurrentHealth = reader.ReadFloat();
            MaxHealth = reader.ReadFloat();
        }
    }

    /// <summary>
    /// Oyuncu giriş bilgilerini taşıyan mesaj yapısı
    /// </summary>
    public struct PlayerConnectionMessage : NetworkMessage
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteInt(PlayerId);
            writer.WriteString(PlayerName);
        }

        public void Deserialize(NetworkReader reader)
        {
            PlayerId = reader.ReadInt();
            PlayerName = reader.ReadString();
        }
    }
} 