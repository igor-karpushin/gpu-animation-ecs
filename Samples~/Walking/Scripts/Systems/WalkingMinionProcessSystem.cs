using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using SnivelerCode.GpuAnimation.Scripts;
using SnivelerCode.Samples.Components;

namespace SnivelerCode.Samples.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WalkingMinionProcessSystem : ISystem
    {
        EntityQuery m_Query;
            
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WalkingMinionConfig>()
                .WithAllRW<WalkingMinionData, WorldTransform>()
                .WithAllRW<MaterialConfigStatic>()
                .WithNone<MaterialFirstSetup>();

            m_Query = state.GetEntityQuery(in builder);
            state.RequireForUpdate(m_Query);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
        
        //[BurstCompile] // error System.IntPtr
        public void OnUpdate(ref SystemState state)
        {
            var entitiesCount = m_Query.CalculateEntityCount();
            
            var raycastCommands =
                CollectionHelper.CreateNativeArray<RaycastCommand>(entitiesCount, state.WorldUpdateAllocator);

            var raycastHits =
                CollectionHelper.CreateNativeArray<RaycastHit>(entitiesCount, state.WorldUpdateAllocator);

            var entityLinks = new NativeParallelHashMap<Entity, int>(entitiesCount, state.WorldUpdateAllocator);

            state.Dependency = new CollectJob
            {
                RaycastCommands = raycastCommands,
                EntityLinks = entityLinks.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            

            // burst: error
            state.Dependency = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 8, state.Dependency);

            state.Dependency = new StatusMinionJob
            {
                RaycastHits = raycastHits,
                DeltaTime = SystemAPI.Time.DeltaTime,
                EntityLinks = entityLinks.AsReadOnly()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(MaterialFirstSetup))]
        partial struct StatusMinionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Buffer;
            [NativeDisableParallelForRestriction] public NativeArray<RaycastHit> RaycastHits;
            public float DeltaTime;
            public NativeParallelHashMap<Entity, int>.ReadOnly EntityLinks;

            void Execute(Entity entity, WalkingMinionProcessAspect aspect)
            {
                if(!EntityLinks.ContainsKey(entity)) return;
                var raycastIndex = EntityLinks[entity];
                
                var raycastHit = RaycastHits[raycastIndex];
                aspect.Process(DeltaTime, raycastHit);
            }
        }

        [BurstCompile]
        [WithNone(typeof(MaterialFirstSetup))]
        partial struct CollectJob : IJobEntity
        {
            public NativeArray<RaycastCommand> RaycastCommands;
            public NativeParallelHashMap<Entity, int>.ParallelWriter EntityLinks;

            void Execute([EntityIndexInQuery] int index, Entity entity, WalkingMinionProcessAspect aspect)
            {
                if (index < RaycastCommands.Length)
                {
                    var backPosition = math.mul(aspect.Rotation, math.back()) * aspect.MinionRadius;
                    backPosition.y += 1;
                    
                    var queryParams = QueryParameters.Default;
                    queryParams.hitBackfaces = false;
                    var command = new RaycastCommand(aspect.Position + backPosition, math.down(), queryParams, 1.2f);
                    RaycastCommands[index] = command;   
                    
                    EntityLinks.TryAdd(entity, index);
                }
            }
        }
    }
}
