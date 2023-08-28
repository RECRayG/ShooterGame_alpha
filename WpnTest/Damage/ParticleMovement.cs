using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class ParticleMovement : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private List<GameObject> particles = new List<GameObject>(); // ������ ���� Particle Systems ������ �������

    private List<MeshCollider> meshColliders = new List<MeshCollider>(); // Mesh Collider �������

    private void Start()
    {
        // �������� ��� Particle Systems, ����������� ������ �������
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
        // ��������� ������� ���� Particle Systems, ����������� ������ �������
        foreach (var ps in particles)
        {
            // ���������, ���� �� � ������� Particle System ��������� Shape, ������� ����� ��������� Mesh Renderer
            var shape = ps.GetComponent<ParticleSystem>().shape;
            if (shape.meshRenderer == null)
            {
                continue;
            }

            // �������� ������� ������� �������, ����������� Particle System
            var position = animator.transform.position;

            // �������� ������� ������� Mesh Collider �������
            var colliderPos = meshCollider[i].bounds.center;

            // ��������� ������� Particle System � ����������� �� ������� Mesh Collider �������
            var newPos = position + (colliderPos - position);
            shape.meshRenderer.transform.position = newPos;

            i++;
        }
    }
}
