using TMPro;
using Unity.Entities;
using UnityEngine;

using SnivelerCode.Samples.Components;

namespace SnivelerCode.Samples.Gui
{
    public class FlyingStats : MonoBehaviour
    {
        [SerializeField] 
        TMP_Text fpsLabel;
        
        [SerializeField]
        TMP_Text entriesLabel;
        Coroutine m_Coroutine;

        void FixedUpdate()
        {
            var query = World.All[0].EntityManager.CreateEntityQuery(typeof(FlyingEntry));
            fpsLabel.text = $"fps: {(int)(1.0f / Time.smoothDeltaTime)}";
            entriesLabel.text = $"entries: {query.CalculateEntityCount()}";    
        }
    }   
}


