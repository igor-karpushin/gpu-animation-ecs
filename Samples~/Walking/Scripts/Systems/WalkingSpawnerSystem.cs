using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

using SnivelerCode.Samples.Components;

namespace SnivelerCode.Samples.Systems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct WalkingSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WalkingSpawnerConfig, WorldTransform>()
                .WithAllRW<WalkingSpawnerData>();
            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new SetupJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Buffer = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct SetupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Buffer;
            public float DeltaTime;

            void Execute([EntityIndexInQuery] int index, in WorldTransform world, 
                in WalkingSpawnerConfig config, ref WalkingSpawnerData data)
            {
                data.CurrentTimer += DeltaTime;
                if (data.CurrentTimer > config.SpawnTimer)
                {
                    data.CurrentTimer = data.Random.NextFloat(-0.2f, 0.2f);
                    var entity = Buffer.Instantiate(index, config.Prefab);
                    
                    Buffer.AddComponent(index, entity, new WalkingMinionConfig
                    {
                        Radius = config.MinionRadius,
                        Speed = config.MinionSpeed,
                        FallingEndTime = config.FallingEndTime
                    });

                    Buffer.AddComponent(index, entity, default(WalkingMinionData));
                    
                    Buffer.SetComponent(index, entity,
                        LocalTransform.FromPositionRotation(world.Position, world.Rotation));
                }
            }
        }
    }
}