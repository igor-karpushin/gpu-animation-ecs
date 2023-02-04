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

        class WalkingSpawnerBaker : Baker<WalkingSpawnerAuthoring>
        {
            public override void Bake(WalkingSpawnerAuthoring authoring)
            {
                var random = Random.Range(1, ushort.MaxValue);
                
                AddComponent(new WalkingSpawnerConfig
                {
                    Prefab = GetEntity(authoring.prefab),
                    SpawnTimer = authoring.spawnTimer,
                    MinionRadius = authoring.minionRadius,
                    MinionSpeed = authoring.minionSpeed,
                    FallingEndTime = authoring.minionFallTime
                });
                
                AddComponent(new WalkingSpawnerData
                {
                    CurrentTimer = authoring.spawnTimer - 1,
                    Random = new Unity.Mathematics.Random((uint)random)
                });
            }
        }
    }
}