#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Scripts.Editor
{
    class GeneratorClipInstance
    {
        public bool Enable;
        public int Start;
        public int Count;
        public int Fps;
        public float Speed;
        public AnimationClip Source;
    }
    
    class GeneratorLodInstance
    {
        public int Percent;
        public Mesh Mesh;
        public SkinnedMeshRenderer Skin;
        public bool Locked;
    }
        
    class GeneratorPrefabInstance
    {
        public GameObject Source;
        public AnimatorController Animator;
        public Dictionary<string, int> BoneMap;

        public bool Extend;
        public Dictionary<string, GeneratorClipInstance> Clips;
        public List<bool> SubAlpha;
        public List<GeneratorLodInstance> Lods;

        public GeneratorPrefabInstance()
        {
            Clips = new Dictionary<string, GeneratorClipInstance>();
            SubAlpha = new List<bool>();
            Lods = new List<GeneratorLodInstance>();
            BoneMap = new Dictionary<string, int>();
        }

        public void Clear()
        {
            Animator = null;
            Extend = false;
            Clips.Clear();
            SubAlpha.Clear();
            Lods.Clear();
            BoneMap.Clear();
        }
        

        public void AnimatorSet(AnimatorController animator)
        {
            Animator = animator;
            if (animator)
            {
                var clips = animator.animationClips;
                foreach (var clip in clips)
                {
                    var clipNameLower = clip.name.ToLower();
                    Clips.Add(clipNameLower, new GeneratorClipInstance
                    {
                        Enable = false,
                        Fps = 15,
                        Speed = 1f,
                        Source = clip
                    });
                }  
            }
        }
    }
    
    class GeneratorAnimationTexture
    {
        public Color[] Pixels;
        public GeneratorAnimationTexture(ushort dimension) => Pixels = new Color[dimension * dimension];
        public void Write(int index, Color color) => Pixels[index] = color;
    }
}

#endif