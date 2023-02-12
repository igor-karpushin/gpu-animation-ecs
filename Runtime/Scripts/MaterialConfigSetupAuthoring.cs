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
            var logGroup = GetComponentInChildren<LODGroup>();
            if(logGroup == null) return;
            if(animations == null) return;
            if(alphaMeshes == null) return;
            
            var materialAnimation = animations[animationIndex % animations.Count];
            
            var lods = logGroup.GetLODs();
            for (var i = 0; i < lods.Length; ++i)
            {
                var lodRenderer = lods[i].renderers[0];
                for (var k = 0; k < lodRenderer.sharedMaterials.Length; ++k)
                {
                    var propertyChild = new MaterialPropertyBlock();
                    lodRenderer.GetPropertyBlock(propertyChild, k);
                    propertyChild.SetFloat(s_AlphaClip, alphaMeshes[k] ? 1f : 0f);
                    propertyChild.SetFloat(s_ModelShown, 1f);
                    propertyChild.SetVector(s_RenderPixel, new Vector4(materialAnimation.Start, materialAnimation.Start, 0));
                    lodRenderer.SetPropertyBlock(propertyChild, k);
                }
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
