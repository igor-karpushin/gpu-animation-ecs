using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using SnivelerCode.GpuAnimation.Scripts;

namespace SnivelerCode.Samples.Components
{
    public readonly partial struct WalkingMinionProcessAspect : IAspect
    {
        readonly RefRO<WalkingMinionConfig> m_MinionConfig;
        readonly RefRW<WalkingMinionData> m_MinionData;
        readonly RefRW<WorldTransform> m_WorldTransform;
        readonly RefRW<MaterialConfigStatic> m_MaterialConfig;

        public quaternion Rotation => m_WorldTransform.ValueRO.Rotation;
        public float3 Position => m_WorldTransform.ValueRO.Position;
        public float MinionRadius => m_MinionConfig.ValueRO.Radius;

        public void Process(float deltaTime, RaycastHit raycastHit)
        {
            var direction = float3.zero;
            var distance = math.abs(raycastHit.distance - 1);
            if (distance < 0.05f)
            {
                switch (m_MinionData.ValueRW.Status)
                {
                    case 1:
                        m_MinionData.ValueRW.Status = 2;
                        m_MinionData.ValueRW.StatusTime = 0f;
                        m_WorldTransform.ValueRW.Position.y = raycastHit.point.y;
                        m_MaterialConfig.ValueRW.AnimationIndex = (byte)WalkingAnimationType.FallEnd;
                        break;

                    case 2:
                        m_MinionData.ValueRW.StatusTime += deltaTime;
                        if (m_MinionData.ValueRW.StatusTime > m_MinionConfig.ValueRO.FallingEndTime)
                        {
                            m_MinionData.ValueRW.Status = 3;
                            m_MaterialConfig.ValueRW.AnimationIndex = (byte)WalkingAnimationType.Walk;
                        }
                        break;

                    case 3:
                        direction = math.mul(m_WorldTransform.ValueRW.Rotation, math.forward()) * deltaTime * m_MinionConfig.ValueRO.Speed;
                        break;
                }
            }
            else
            {
                if (m_MinionData.ValueRW.Status != 1)
                {
                    m_MinionData.ValueRW.Status = 1;
                    m_MaterialConfig.ValueRW.AnimationIndex = (byte)WalkingAnimationType.Fall;
                }

                direction = new float3(0, -deltaTime * 1.7f, 0);
            }

            m_WorldTransform.ValueRW.Position += direction;
        }
        
    }
}