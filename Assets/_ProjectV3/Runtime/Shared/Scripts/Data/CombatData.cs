using UnityEngine;
using Mirror;

namespace ProjectV3.Shared.Data
{
    /// <summary>
    /// Saldırı türlerini temsil eden enum
    /// </summary>
    public enum AttackType
    {
        LightAttack,
        HeavyAttack,
        SpecialAttack
    }

    /// <summary>
    /// Hasar türlerini temsil eden enum
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical,
        True
    }

    /// <summary>
    /// Saldırı verilerini taşıyan yapı
    /// </summary>
    public struct AttackData : NetworkMessage
    {
        public int AttackerId { get; set; }
        public int TargetId { get; set; }
        public AttackType Type { get; set; }
        public DamageType DamageType { get; set; }
        public float Damage { get; set; }
        public Vector3 ImpactPoint { get; set; }
        public Vector3 ImpactNormal { get; set; }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteInt(AttackerId);
            writer.WriteInt(TargetId);
            writer.WriteInt((int)Type);
            writer.WriteInt((int)DamageType);
            writer.WriteFloat(Damage);
            writer.WriteVector3(ImpactPoint);
            writer.WriteVector3(ImpactNormal);
        }

        public void Deserialize(NetworkReader reader)
        {
            AttackerId = reader.ReadInt();
            TargetId = reader.ReadInt();
            Type = (AttackType)reader.ReadInt();
            DamageType = (DamageType)reader.ReadInt();
            Damage = reader.ReadFloat();
            ImpactPoint = reader.ReadVector3();
            ImpactNormal = reader.ReadVector3();
        }
    }

    /// <summary>
    /// Hasar sonuçlarını taşıyan yapı
    /// </summary>
    public struct DamageResult : NetworkMessage
    {
        public int TargetId { get; set; }
        public float DamageDealt { get; set; }
        public float RemainingHealth { get; set; }
        public bool IsCritical { get; set; }
        public bool IsKillingBlow { get; set; }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteInt(TargetId);
            writer.WriteFloat(DamageDealt);
            writer.WriteFloat(RemainingHealth);
            writer.WriteBool(IsCritical);
            writer.WriteBool(IsKillingBlow);
        }

        public void Deserialize(NetworkReader reader)
        {
            TargetId = reader.ReadInt();
            DamageDealt = reader.ReadFloat();
            RemainingHealth = reader.ReadFloat();
            IsCritical = reader.ReadBool();
            IsKillingBlow = reader.ReadBool();
        }
    }
} 