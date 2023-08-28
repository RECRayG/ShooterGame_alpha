using System.Collections.Generic;
using UnityEngine;

namespace ImpactSystem.Effects
{
    [CreateAssetMenu(menuName = "Impact System/Surface Effect", fileName = "SurfaceEffect")]
    public class SurfaceEffect : ScriptableObject
    {
        // Создание объектов
        public List<SpawnObjectEffect> SpawnObjectEffects = new List<SpawnObjectEffect>();
        // Воспроизведение звуков
        public List<PlayAudioEffect> PlayAudioEffects = new List<PlayAudioEffect>();
    }
}