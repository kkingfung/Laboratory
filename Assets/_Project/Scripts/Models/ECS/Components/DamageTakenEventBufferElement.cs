using Unity.Entities;
using Unity.Mathematics;

public struct DamageTakenEvent
{
    public Entity TargetEntity;
    public int DamageAmount;
    public DamageType DamageType;
    public float3 SourcePosition;
    public bool IsDead;
}

[InternalBufferCapacity(8)]
public struct DamageTakenEventBufferElement : IBufferElementData
{
    public DamageTakenEvent Value;
}
