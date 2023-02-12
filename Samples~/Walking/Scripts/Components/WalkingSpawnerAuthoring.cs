using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SnivelerCode.Samples.Components
{
    public class WalkingSpawnerAuthoring : MonoBehaviour
    {
        [Header("Spawner")]
        public GameObject prefab;
        public float spawnTimer;
        
        [Header("Minion")]
        public float minionRadius;
        public float minionSpeed;
        public float minionFallTime;
    }
    
    public class WalkingSpawnerBaker : Baker<WalkingSpawnerAuthoring>
    {
        public override void Bake(WalkingSpawnerAuthoring data)
        {
            var random = Random.Range(1, ushort.MaxValue);
            if (data.prefab != null)
            {
                AddComponent(new WalkingSpawnerConfig
                {
                    Prefab = GetEntity(data.prefab),
                    SpawnTimer = data.spawnTimer,
                    MinionRadius = data.minionRadius,
                    MinionSpeed = data.minionSpeed,
                    FallingEndTime = data.minionFallTime
                });
                
                AddComponent(new WalkingSpawnerData
                {
                    CurrentTimer = data.spawnTimer - 1,
                    Random = new Unity.Mathematics.Random((uint)random)
                });
            }
        }
    }
}