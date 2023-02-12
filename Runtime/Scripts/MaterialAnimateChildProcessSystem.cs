using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.Scripts
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(MaterialAnimateProcessSystem))]
    public partial struct MaterialAnimateChildProcessSystem : ISystem
    {
        ComponentLookup<MaterialConfigRender> m_LookMaterialConfig;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_LookMaterialConfig = state.GetComponentLookup<MaterialConfigRender>(true);
            
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MeshLODComponent>()
                .WithAllRW<MaterialRenderPixel>();

            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_LookMaterialConfig.Update(ref state);
            state.Dependency = new AnimateChildProcessJob
            {
                LookMaterials = m_LookMaterialConfig
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct AnimateChildProcessJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<MaterialConfigRender> LookMaterials;

            void Execute(in MeshLODComponent lod, ref MaterialRenderPixel data)
            {
                if (LookMaterials.HasComponent(lod.Group))
                {
                    data.Value = LookMaterials[lod.Group].RenderConfig;
                }
            }
        }
    }
}
