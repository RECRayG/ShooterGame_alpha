using System.Collections.Generic;
using UnityEngine;

namespace ImpactSystem.Effects
{
    [CreateAssetMenu(menuName = "Impact System/Play Audio Effect", fileName = "PlayAudioEffect")]
    public class PlayAudioEffect : ScriptableObject
    {
        // Источник звука
        public AudioSource AudioSourcePrefab;
        public List<AudioClip> AudioClips = new List<AudioClip>();
        // Громкость рандомная
        [Tooltip("Values are clamped to 0-1")]
        public Vector2 VolumeRange = new Vector2(0, 1);
    }
}