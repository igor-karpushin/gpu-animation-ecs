using System.Collections.Generic;
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
        
    class GeneratorPrefabInstance
    {
        public GameObject Source;
        public bool Extend;
        public Dictionary<string, GeneratorClipInstance> Clips;
        public List<bool> SubAlpha;
    }
    
    class GeneratorAnimationTexture
    {
        public Color[] Pixels;
        public GeneratorAnimationTexture(ushort dimension) => Pixels = new Color[dimension * dimension];
        public void Write(int index, Color color) => Pixels[index] = color;
    }
}