using System.Collections;
using TMPro;
using Unity.Entities;
using UnityEngine;

using SnivelerCode.Samples.Components;

namespace SnivelerCode.Samples.Gui
{
    public class WalkingStats : MonoBehaviour
    {
        [SerializeField] 
        TMP_Text fpsLabel;
        
        [SerializeField]
        TMP_Text entriesLabel;
        Coroutine m_Coroutine;

        void OnEnable() => StartCoroutine(UpdateLabels());

        void OnDisable() => StopAllCoroutines();

        IEnumerator UpdateLabels()
        {
            var manager = World.All[0].EntityManager;
            var query = manager.CreateEntityQuery(typeof(WalkingMinionConfig));
            
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                fpsLabel.text = $"fps: {(int)(1.0f / Time.smoothDeltaTime)}";
                entriesLabel.text = $"entries: {query.CalculateEntityCount()}";    
            }
        }
    }   
}


