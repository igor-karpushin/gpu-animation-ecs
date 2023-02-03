using Unity.Entities;
using Unity.Mathematics;

namespace SnivelerCode.Samples.Components
{
    public struct WalkingSpawnerConfig : IComponentData
    {
        public Entity Prefab;
        public float SpawnTimer;
        public float MinionRadius;
        public float MinionSpeed;
        public float FallingEndTime;
    }
    
    public struct WalkingSpawnerData : IComponentData
    {
        public float CurrentTimer;
        public Random Random;
    }
    
    public struct WalkingMinionConfig : IComponentData
    {
        public float Radius;
        public float Speed;
        public float FallingEndTime;
    }
    
    public struct WalkingMinionData : IComponentData
    {
        public byte Status;
        public float StatusTime;
    }
    
    enum WalkingAnimationType : byte
    {
        Pose,
        Walk,
        Fall,
        FallEnd
    } 
}