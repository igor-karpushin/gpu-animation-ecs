using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using SnivelerCode.Samples.Components;

namespace SnivelerCode.Samples.Systems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct FlyingSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<FlyingSpawnBuffer, FlyingSpawnConfig>()
                .WithAllRW<FlyingSpawnData>();
            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new SpawnJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Buffer = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct SpawnJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Buffer;
            public float DeltaTime;

            void Execute([EntityIndexInQuery] int index, in Entity entity,
                in FlyingSpawnConfig config,
                ref FlyingSpawnData data,
                in DynamicBuffer<FlyingSpawnBuffer> buffer)
            {
                if (data.SpawnCount < config.SpawnTotalCount)
                {
                    data.SpawnCurrentTime += DeltaTime;
                    if (data.SpawnCurrentTime > config.SpawnTime)
                    {
                        data.SpawnCount++;
                        data.SpawnCurrentTime = 0;

                        const float heightDelta = 8f;
                        var testRandom = Random.CreateFromIndex((uint)(data.SpawnCount * 2.5f));

                        var randomAngle = testRandom.NextFloat(0, math.PI);
                        var randomHeight = testRandom.NextFloat(-heightDelta, heightDelta);
                        var randomRadius = testRandom.NextFloat(4f, 8f);
                        var deltaHeight = 1.5f - math.abs(randomHeight) / heightDelta;

                        var nextPosition = new float3(
                            randomRadius * math.sin(randomAngle) * deltaHeight,
                            randomHeight,
                            randomRadius * math.cos(randomAngle) * deltaHeight
                        );

                        var randomPrefabIndex = testRandom.NextInt(0, buffer.Length);
                        var randomPrefab = buffer[randomPrefabIndex].Value;

                        var prefabEntity = Buffer.Instantiate(index, randomPrefab);
                        Buffer.AddComponent(index, prefabEntity, new FlyingEntry
                        {
                            Height = nextPosition.y,
                            Radius = math.length(nextPosition.xz),
                            DeltaSpeed = testRandom.NextFloat(-0.4f, 0.2f)
                        });
                        Buffer.AddComponent(index, prefabEntity, new FlyingAngle
                        {
                            Value = math.atan(nextPosition.z / nextPosition.x)
                        });
                        Buffer.SetComponent(index, prefabEntity, LocalTransform.FromPosition(nextPosition));
                    }
                }
                else
                {
                    Buffer.RemoveComponent<FlyingSpawnData>(index, entity);
                }
            }
        }
    }
}
