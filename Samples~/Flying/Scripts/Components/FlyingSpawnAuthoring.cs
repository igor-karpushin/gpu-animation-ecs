using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SnivelerCode.Samples.Components
{
    public class FlyingSpawnAuthoring : MonoBehaviour
    {
        public List<GameObject> prefabs;
        public int totalCount;
        public float spawnTimer;
    }
    
    public class SceneWalkingBaker : Baker<FlyingSpawnAuthoring>
    {
        public override void Bake(FlyingSpawnAuthoring data)
        {
            AddComponent(new FlyingSpawnConfig
            {
                SpawnTotalCount = (ushort)data.totalCount,
                SpawnTime = data.spawnTimer
            });
            AddComponent(default(FlyingSpawnData));
            
            if (data.prefabs != null)
            {
                var buffer = AddBuffer<FlyingSpawnBuffer>();
                foreach (var gamObject in data.prefabs)
                {
                    buffer.Add(new FlyingSpawnBuffer { Value = GetEntity(gamObject) });
                }
            }
        }
    }
}
