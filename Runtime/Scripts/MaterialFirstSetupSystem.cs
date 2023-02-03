using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace SnivelerCode.GpuAnimation.Scripts
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MaterialFirstSetupSystem : ISystem
    {
        ComponentLookup<MaterialMeshInfo> m_LookMaterial;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_LookMaterial = state.GetComponentLookup<MaterialMeshInfo>(true);
            
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MaterialFirstSetup, MaterialConfigStatic, MaterialAnimation, Child>();

            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            m_LookMaterial.Update(ref state);

            state.Dependency = new SetupJob
            {
                Commands = ecb.AsParallelWriter(),
                MaterialMeshes = m_LookMaterial
                
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct SetupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;
            [ReadOnly] public ComponentLookup<MaterialMeshInfo> MaterialMeshes;

            void Execute([EntityIndexInQuery] int index, in Entity entity,
                in MaterialFirstSetup _,
                in MaterialConfigStatic config,
                in DynamicBuffer<MaterialAnimation> animations,
                in DynamicBuffer<MaterialSubMeshAlpha> alphaBuffer, 
                in DynamicBuffer<Child> child)
            {
                Commands.RemoveComponent<MaterialFirstSetup>(index, entity);
                Commands.RemoveComponent<MaterialSubMeshAlpha>(index, entity);
                
                var animationIndex = config.AnimationIndex % animations.Length;
                var animation = animations[animationIndex];

                for (var i = 0; i < child.Length; ++i)
                {
                    var childIndex = index + i;
                    var childEntity = child[i].Value;
                    
                    Commands.AddComponent(childIndex, childEntity,new MaterialModelShown { Value = 1f });
                    Commands.AddComponent(childIndex, childEntity,new MaterialRenderPixel
                    {
                        Value = new float3(animation.Start, animation.Start, 0)
                    });
                    
                    if (MaterialMeshes.HasComponent(childEntity))
                    {
                        var meshInfo = MaterialMeshes[childEntity];
                        if (meshInfo.Submesh < alphaBuffer.Length)
                        {
                            Commands.AddComponent(
                                childIndex,
                                childEntity,
                                new MaterialAlphaEnabled
                                {
                                    Value = alphaBuffer[meshInfo.Submesh].Value ? 1f : 0f
                                }
                            );
                        }
                    }
                }
            }
        }
    }
}
