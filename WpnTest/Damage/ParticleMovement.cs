using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class ParticleMovement : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private List<GameObject> particles = new List<GameObject>(); // Массив всех Particle Systems внутри объекта

    private List<MeshCollider> meshColliders = new List<MeshCollider>(); // Mesh Collider объекта

    private void Start()
    {
        // Получаем все Particle Systems, находящиеся внутри объекта
        FindAllParticleSystems(transform);
        AddMeshColliders();
    }

    private void AddMeshColliders()
    {
        foreach (GameObject gameObject in particles)
        {
            MeshCollider meshCollider = gameObject.GetComponentInParent<MeshCollider>();
            if (meshCollider != null)
            {
                meshColliders.Add(meshCollider);
            }
        }
    }

    private void FindAllParticleSystems(Transform parent)
    {
        foreach (Transform child in parent)
        {
            ParticleSystem particleSystem = child.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particles.Add(child.gameObject);
            }
            if (child.childCount > 0)
            {
                FindAllParticleSystems(child);
            }
        }
    }

    private void OnAnimatorIK()
    {
        MeshCollider[] meshCollider = meshColliders.ToArray();
        int i = 0;
        // Обновляем позиции всех Particle Systems, находящихся внутри объекта
        foreach (var ps in particles)
        {
            // Проверяем, есть ли у данного Particle System компонент Shape, который может содержать Mesh Renderer
            var shape = ps.GetComponent<ParticleSystem>().shape;
            if (shape.meshRenderer == null)
            {
                continue;
            }

            // Получаем текущую позицию объекта, содержащего Particle System
            var position = animator.transform.position;

            // Получаем текущую позицию Mesh Collider объекта
            var colliderPos = meshCollider[i].bounds.center;

            // Обновляем позицию Particle System в зависимости от позиции Mesh Collider объекта
            var newPos = position + (colliderPos - position);
            shape.meshRenderer.transform.position = newPos;

            i++;
        }
    }
}
