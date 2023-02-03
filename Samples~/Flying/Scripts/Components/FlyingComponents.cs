using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SnivelerCode.Samples.Components
{
    public struct FlyingEntry : IComponentData
    {
        public float Radius;
        public float Height;
        public float DeltaSpeed;
    }

    public struct FlyingAngle : IComponentData
    {
        public float Value;
    }

    public readonly partial struct FlyingAspect : IAspect
    {
        readonly RefRO<FlyingEntry> m_Entry;
        readonly RefRW<FlyingAngle> m_Angle;
        readonly RefRW<WorldTransform> m_Transform;

        public void ChangeAngle(float deltaTime) => m_Angle.ValueRW.Value += deltaTime;

        public void ChangePosition(float deltaTime)
        {
            var nextAngle = m_Angle.ValueRO.Value + deltaTime * (1 + m_Entry.ValueRO.DeltaSpeed);
            var nextPosition = new float3(
                m_Entry.ValueRO.Radius * math.sin(nextAngle),
                m_Entry.ValueRO.Height + math.sin(nextAngle * 7) * 0.2f,
                m_Entry.ValueRO.Radius * math.cos(nextAngle)
            );
            m_Angle.ValueRW.Value = nextAngle;
                
            m_Transform.ValueRW.Position = nextPosition;
            m_Transform.ValueRW.Rotation = quaternion.RotateY(nextAngle + math.PI * 0.5f);
        }
    }
    
    [InternalBufferCapacity(8)]
    public struct FlyingSpawnBuffer : IBufferElementData
    {
        public Entity Value;
    }
    
    public struct FlyingSpawnConfig : IComponentData
    {
        public float SpawnTime;
        public ushort SpawnTotalCount;
    }
    
    public struct FlyingSpawnData : IComponentData
    {
        public float SpawnCurrentTime;
        public ushort SpawnCount;
    }
}