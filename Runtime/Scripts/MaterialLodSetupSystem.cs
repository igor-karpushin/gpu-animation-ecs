using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.Scripts
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MaterialLodSetupSystem : ISystem
    {
        ComponentLookup<MaterialConfigStatic> m_MaterialConfigLookup;
        BufferLookup<MaterialAnimation> m_AnimationLookup;
        BufferLookup<MaterialSubMeshAlpha> m_MaterialSubMeshLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_MaterialConfigLookup = state.GetComponentLookup<MaterialConfigStatic>(true);
            m_AnimationLookup = state.GetBufferLookup<MaterialAnimation>(true);
            m_MaterialSubMeshLookup = state.GetBufferLookup<MaterialSubMeshAlpha>(true);

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MeshLODComponent, MaterialMeshInfo>()
                .WithNone<MaterialFirstSetup>();

            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            m_MaterialConfigLookup.Update(ref state);
            m_AnimationLookup.Update(ref state);
            m_MaterialSubMeshLookup.Update(ref state);

            state.Dependency = new SetupJob
            {
                Commands = ecb.AsParallelWriter(),
                MaterialConfig = m_MaterialConfigLookup,
                AnimationLookup = m_AnimationLookup,
                MaterialSubMeshLookup = m_MaterialSubMeshLookup
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(MaterialFirstSetup))]
        partial struct SetupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;
            [ReadOnly]
            public ComponentLookup<MaterialConfigStatic> MaterialConfig;
            [ReadOnly]
            public BufferLookup<MaterialAnimation> AnimationLookup;
            [ReadOnly]
            public BufferLookup<MaterialSubMeshAlpha> MaterialSubMeshLookup;

            void Execute(
                [EntityIndexInQuery] int index,
                in Entity entity,
                in MeshLODComponent meshLod,
                in MaterialMeshInfo meshInfo)
            {
                Commands.AddComponent(index, entity, default(MaterialFirstSetup));
                Commands.AddComponent(index, entity,new MaterialModelShown { Value = 1f });
                if (AnimationLookup.HasBuffer(meshLod.Group))
                {
                    if (MaterialConfig.HasComponent(meshLod.Group))
                    {
                        var animations = AnimationLookup[meshLod.Group];
                        var config = MaterialConfig[meshLod.Group];
                        var animationIndex = config.AnimationIndex % animations.Length;
                        var animation = animations[animationIndex];
                        Commands.AddComponent(index, entity,new MaterialRenderPixel
                        {
                            Value = new float3(animation.Start, animation.Start, 0)
                        });
                    }
                }

                if (MaterialSubMeshLookup.HasBuffer(meshLod.Group))
                {
                    Commands.RemoveComponent<MaterialSubMeshAlpha>(index, meshLod.Group);
                    var subMesh = MaterialSubMeshLookup[meshLod.Group];
                    if (meshInfo.Submesh < subMesh.Length)
                    {
                        Commands.AddComponent(
                            index,
                            entity,
                            new MaterialAlphaEnabled
                            {
                                Value = subMesh[meshInfo.Submesh].Value ? 1f : 0f
                            }
                        );
                    }
                }
            }
        }
    }
}
