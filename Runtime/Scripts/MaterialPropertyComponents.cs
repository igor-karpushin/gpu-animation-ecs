using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.Scripts
{
    
    [MaterialProperty("_SnivelerAlphaEnabled")]
    public struct MaterialAlphaEnabled : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_SnivelerModelShown")]
    public struct MaterialModelShown : IComponentData
    {
        public float Value;
    }
    
    [MaterialProperty("_SnivelerRenderPixel")]
    public struct MaterialRenderPixel : IComponentData
    {
        public float3 Value;
    }
    
    public struct MaterialSubMeshAlpha : IBufferElementData
    {
        public bool Value;
    }
    
    public struct MaterialFirstSetup : IComponentData
    {
    }
    
    public struct MaterialConfigStatic : IComponentData
    {
        public byte BoneCount;
        public byte AnimationIndex;
    }
    
    public struct MaterialConfigRender : IComponentData
    {
        public byte AnimationIndex;
        public float Time;
        public float3 RenderConfig;
    }
    
    [Serializable]
    public struct MaterialAnimation : IBufferElementData
    {
        public byte Fps;
        public ushort Start;
        public ushort Frames;
        public byte Speed;
        public bool Loop;
    }
}