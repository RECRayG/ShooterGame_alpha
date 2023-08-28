using UnityEngine;

namespace ImpactSystem.Effects
{
    [CreateAssetMenu(menuName = "Impact System/Spawn Object Effect", fileName = "SpawnObjectEffect")]
    public class SpawnObjectEffect : ScriptableObject
    {
        // ��� ������
        public GameObject Prefab;
        // ����������� ��������� �������
        public float Probability = 1;
        // ��������������� �������� (��/���)
        public bool RandomizeRotation;
        public float minSize = 1f;
        public float maxSize = 1f;
        [Tooltip("Zero values will lock the rotation on that axis. Values up to 360 are sensible for each X,Y,Z")]
        public Vector3 RandomizedRotationMultiplier = Vector3.zero;
    }
}