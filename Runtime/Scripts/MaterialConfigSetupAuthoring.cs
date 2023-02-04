using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Scripts
{
    [ExecuteInEditMode]
    public class MaterialConfigSetupAuthoring : MonoBehaviour
    {
        static readonly int s_AlphaClip = Shader.PropertyToID("_SnivelerAlphaEnabled");
        static readonly int s_ModelShown = Shader.PropertyToID("_SnivelerModelShown");
        static readonly int s_RenderPixel = Shader.PropertyToID("_SnivelerRenderPixel");
        
        public int animationIndex;
        public int bonesCount;
        public List<MaterialAnimation> animations;
        public List<bool> alphaMeshes;

#if UNITY_EDITOR
        void OnEnable()
        {
            var meshRenderer = GetComponentInChildren<MeshRenderer>();
            if(meshRenderer == null) return;
            if(animations == null) return;
            if(alphaMeshes == null) return;
            
            var materialAnimation = animations[animationIndex % animations.Count];
                
            for (var i = 0; i < meshRenderer.sharedMaterials.Length; ++i)
            {
                var propertyChild = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(propertyChild, i);
                propertyChild.SetFloat(s_AlphaClip, alphaMeshes[i] ? 1f : 0f);
                propertyChild.SetFloat(s_ModelShown, 1f);
                propertyChild.SetVector(s_RenderPixel, new Vector4(materialAnimation.Start, materialAnimation.Start, 0));
                meshRenderer.SetPropertyBlock(propertyChild, i);
            } 
        }
#endif

        public void Setup(int bones, List<MaterialAnimation> anims, List<bool> alpha)
        {
            bonesCount = bones;
            animations = anims;
            alphaMeshes = alpha;
            animationIndex = 0;
        }
    }
    
    public class MaterialSetupBaker : Baker<MaterialConfigSetupAuthoring>
    {
        public override void Bake(MaterialConfigSetupAuthoring data)
        {
            AddComponent(new MaterialConfigStatic
            {
                AnimationIndex = (byte)data.animationIndex,
                BoneCount = (byte)data.bonesCount
            });
            AddComponent(new MaterialConfigRender { AnimationIndex = (byte)data.animationIndex });
            AddComponent<MaterialFirstSetup>();

            if (data.animations != null)
            {
                var animationBuffer = AddBuffer<MaterialAnimation>();
                data.animations.ForEach(value => animationBuffer.Add(value));
            }

            if (data.alphaMeshes != null)
            {
                var alphaBuffer = AddBuffer<MaterialSubMeshAlpha>();
                data.alphaMeshes.ForEach(value =>
                    alphaBuffer.Add(new MaterialSubMeshAlpha { Value = value }));
            }
        }
    }
}
