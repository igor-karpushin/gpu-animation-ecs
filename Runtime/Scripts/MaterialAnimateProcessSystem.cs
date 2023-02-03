using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SnivelerCode.GpuAnimation.Scripts
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MaterialAnimateProcessSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MaterialConfigStatic, MaterialAnimation>()
                .WithAllRW<MaterialConfigRender>();

            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new AnimateProcessJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct AnimateProcessJob : IJobEntity
        {
            public float DeltaTime;
            void Execute(
                in MaterialConfigStatic config,
                ref MaterialConfigRender data, 
                in DynamicBuffer<MaterialAnimation> anims)
            {
                if (config.AnimationIndex != data.AnimationIndex)
                {
                    data.Time = 0;
                    data.AnimationIndex = config.AnimationIndex;
                }
                else
                {
                    data.Time += DeltaTime;
                }
                
                var animationIndex = config.AnimationIndex % anims.Length;
                var animation = anims[animationIndex];
                
                var floatFrame = data.Time * animation.Fps * animation.Speed;
                var rawFrame = (ushort)floatFrame;

                var rawFrameNext = rawFrame + 1;

                int frame;
                int nextFrame;
                if (animation.Loop)
                {
                    frame = rawFrame % animation.Frames;
                    nextFrame = rawFrameNext % animation.Frames;
                }
                else
                {
                    frame = math.clamp(rawFrame, 0, animation.Frames - 1);
                    nextFrame = math.clamp(rawFrameNext, 0, animation.Frames - 1);
                }

                data.RenderConfig = new float3(
                    animation.Start + frame * config.BoneCount,
                    animation.Start + nextFrame * config.BoneCount,
                    floatFrame - rawFrame);
            }
        }
    }
}
