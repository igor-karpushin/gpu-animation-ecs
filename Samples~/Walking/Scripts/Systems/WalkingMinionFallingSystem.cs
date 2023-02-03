using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

using SnivelerCode.Samples.Components;

namespace SnivelerCode.Samples.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct WalkingMinionFallingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WalkingMinionConfig, WorldTransform>();
            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new FallingJob
            {
                Commands = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }


        [BurstCompile]
        [WithAll(typeof(WalkingMinionConfig))]
        partial struct FallingJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;

            void Execute([EntityIndexInQuery] int index, Entity entity,
                in WorldTransform world)
            {
                if (!(world.Position.y < -2.5f)) return;
                Commands.DestroyEntity(index, entity);
            }
        }
    }
}