using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using ImpactSystem.Effects;
using ImpactSystem.Pool;
using Guns;
using ClipperLib;
using System;
using static UnityEngine.ParticleSystem;
using Unity.VisualScripting;
using Random = UnityEngine.Random;
using Guns.Enemy;

namespace ImpactSystem
{
    public class SurfaceManager : MonoBehaviour
    {
        private static SurfaceManager _instance;
        public static SurfaceManager Instance
        {
            get
            {
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        // ������ ��������� ������������
        [SerializeField] private List<SurfaceType> Surfaces = new List<SurfaceType>();
        // ����������� �� ���������
        [SerializeField] private Surface DefaultSurface;

        // ������ ��������� ������������ ��� ������ �������� ���
        [SerializeField] private List<SurfaceType> SurfacesNearWpn = new List<SurfaceType>();
        // ����������� �� ��������� ��� ������ �������� ��� ^)
        [SerializeField] private Surface DefaultSurfaceNearWpn;

        // ����� ������������ ������
        [SerializeField] private MinMaxCurve FadeTime;

        // ��� ��������
        private Dictionary<GameObject, ObjectPool<GameObject>> ObjectPools = new();
        private Dictionary<GameObject, GameObject> ObjectPoolsParent = new();

        private void Awake()
        {
            // �������� �� ������� ������ ��������, ������ ��� ������ ������� ������ 1 ������
            if (Instance != null)
            {
                Debug.LogError("More than one SurfaceManager active in the scene! Destroying latest one: " + name);
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void HandleImpact(GameObject HitObject, Vector3 HitPoint, Vector3 HitNormal, ImpactType Impact, int TriangleIndex, GunType Type)
        {
            // �������� �� ��������� � ������ ���������
            if (HitObject.TryGetComponent<Terrain>(out Terrain terrain))
            {
                // ��������� �������� ������� ������� ���������
                List<TextureAlpha> activeTextures = GetActiveTexturesFromTerrain(terrain, HitPoint);
                foreach (TextureAlpha activeTexture in activeTextures)
                {
                    // ���� ���� ��� ������ �� ������� �������� ���
                    if(!Type.Equals(GunType.BaseballBat))
                    {
                        // �������� ��� ������� �����������
                        SurfaceType surfaceType = Surfaces.Find(surface => surface.Albedo == activeTexture.Texture);
                        // ���� ��� ����������� ����������
                        if (surfaceType != null)
                        {
                            // ���������� �� ���� ��������������� ����� ������ � ��������������� ��������
                            foreach (Surface.SurfaceImpactTypeEffect typeEffect in surfaceType.Surface.ImpactTypeEffects)
                            {
                                // ���� ��������������� ��� ����� ��������� � ���������� �����
                                if (typeEffect.ImpactType == Impact)
                                {
                                    // ������������� ������
                                    PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect, activeTexture.Alpha);
                                }
                            }
                        }
                        else // ���� ��� ���������� �� ��������, �� ������������� ����������� �������
                        {
                            foreach (Surface.SurfaceImpactTypeEffect typeEffect in DefaultSurface.ImpactTypeEffects)
                            {
                                if (typeEffect.ImpactType == Impact)
                                {
                                    PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect, 1);
                                }
                            }
                        }
                    } // ���� ���� ��� ������ ������� �������� ���
                    else
                    {
                        // �������� ��� ������� �����������
                        SurfaceType surfaceType = SurfacesNearWpn.Find(surface => surface.Albedo == activeTexture.Texture);
                        // ���� ��� ����������� ����������
                        if (surfaceType != null)
                        {
                            // ���������� �� ���� ��������������� ����� ������ � ��������������� ��������
                            foreach (Surface.SurfaceImpactTypeEffect typeEffect in surfaceType.Surface.ImpactTypeEffects)
                            {
                                // ���� ��������������� ��� ����� ��������� � ���������� �����
                                if (typeEffect.ImpactType == Impact)
                                {
                                    // ������������� ������
                                    PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect, activeTexture.Alpha);
                                }
                            }
                        }
                        else // ���� ��� ���������� �� ��������, �� ������������� ����������� �������
                        {
                            foreach (Surface.SurfaceImpactTypeEffect typeEffect in DefaultSurfaceNearWpn.ImpactTypeEffects)
                            {
                                if (typeEffect.ImpactType == Impact)
                                {
                                   PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect, 1);
                                }
                            }
                        }
                    }
                }
            } // ����� �������� �� ��������� � ������� ������ (�������), �� ������ �� �� �����, ������ ��� 1 ��������
            else if (HitObject.TryGetComponent<Renderer>(out Renderer renderer))
            {
                Texture activeTexture = GetActiveTextureFromRenderer(renderer, TriangleIndex);

                // ���� ���� ��� ������ �� ������� �������� ���
                if (!Type.Equals(GunType.BaseballBat))
                {
                    SurfaceType surfaceType = Surfaces.Find(surface => surface.Albedo == activeTexture);
                    if (surfaceType != null)
                    {
                        foreach (Surface.SurfaceImpactTypeEffect typeEffect in surfaceType.Surface.ImpactTypeEffects)
                        {
                            if (typeEffect.ImpactType == Impact)
                            {
                                PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect, 1);
                            }
                        }
                    }
                    else
                    {
                        foreach (Surface.SurfaceImpactTypeEffect typeEffect in DefaultSurface.ImpactTypeEffects)
                        {
                            if (typeEffect.ImpactType == Impact)
                            {
                                PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect, 1);
                            }
                        }
                    }
                } // ���� ���� ��� ������ ������� �������� ���
                else
                {
                    SurfaceType surfaceType = SurfacesNearWpn.Find(surface => surface.Albedo == activeTexture);
                    if (surfaceType != null)
                    {
                        foreach (Surface.SurfaceImpactTypeEffect typeEffect in surfaceType.Surface.ImpactTypeEffects)
                        {
                            if (typeEffect.ImpactType == Impact)
                            {
                                PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect, 1);
                            }
                        }
                    }
                    else
                    {
                        foreach (Surface.SurfaceImpactTypeEffect typeEffect in DefaultSurfaceNearWpn.ImpactTypeEffects)
                        {
                            if (typeEffect.ImpactType == Impact)
                            {
                                PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect, 1);
                            }
                        }
                    }
                }
            }
        }

        private List<TextureAlpha> GetActiveTexturesFromTerrain(Terrain Terrain, Vector3 HitPoint)
        {
            Vector3 terrainPosition = HitPoint - Terrain.transform.position;
            Vector3 splatMapPosition = new Vector3(
                terrainPosition.x / Terrain.terrainData.size.x,
                0,
                terrainPosition.z / Terrain.terrainData.size.z
            );

            int x = Mathf.FloorToInt(splatMapPosition.x * Terrain.terrainData.alphamapWidth);
            int z = Mathf.FloorToInt(splatMapPosition.z * Terrain.terrainData.alphamapHeight);

            float[,,] alphaMap = Terrain.terrainData.GetAlphamaps(x, z, 1, 1);

            List<TextureAlpha> activeTextures = new List<TextureAlpha>();
            for (int i = 0; i < alphaMap.Length; i++)
            {
                if (alphaMap[0, 0, i] > 0)
                {
                    activeTextures.Add(new TextureAlpha()
                    {
                        Texture = Terrain.terrainData.terrainLayers[i].diffuseTexture,
                        Alpha = alphaMap[0, 0, i]
                    });
                }
            }

            return activeTextures;
        }

        private Texture GetActiveTextureFromRenderer(Renderer Renderer, int TriangleIndex)
        {
            if (Renderer.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
            {
                Mesh mesh = meshFilter.mesh;

                return GetTextureFromMesh(mesh, TriangleIndex, Renderer.sharedMaterials);
            }
            else if (Renderer is SkinnedMeshRenderer)
            {
                SkinnedMeshRenderer smr = (SkinnedMeshRenderer)Renderer;
                Mesh mesh = smr.sharedMesh;

                return GetTextureFromMesh(mesh, TriangleIndex, Renderer.sharedMaterials);
            }

            Debug.LogError($"{Renderer.name} has no MeshFilter or SkinnedMeshRenderer! Using default impact effect instead of texture-specific one because we'll be unable to find the correct texture!");
            return null;
        }

        private Texture GetTextureFromMesh(Mesh Mesh, int TriangleIndex, Material[] Materials)
        {
            // ������������ �������� ��������� ������� (������������ #)
            // ���� ���������� ������� ������ 1
            if (Mesh.subMeshCount > 1)
            {
                // ���������� ����, � ����� ����������� �������� ���� ���������
                int[] hitTriangleIndices = new int[]
                {
                    Mesh.triangles[TriangleIndex * 3],
                    Mesh.triangles[TriangleIndex * 3 + 1],
                    Mesh.triangles[TriangleIndex * 3 + 2]
                };

                // ������ �� ���� �����������
                for (int i = 0; i < Mesh.subMeshCount; i++)
                {
                    int[] submeshTriangles = Mesh.GetTriangles(i);
                    // ������ �� ���� ������������� ����������
                    for (int j = 0; j < submeshTriangles.Length; j += 3)
                    {
                        // ���� ������� ����������� ����� ����������� ������������ �� ������������ RayCast
                        if (submeshTriangles[j] == hitTriangleIndices[0]
                            && submeshTriangles[j + 1] == hitTriangleIndices[1]
                            && submeshTriangles[j + 2] == hitTriangleIndices[2])
                        {
                            // ������� ��������� ��������
                            return Materials[i].mainTexture;
                        }
                    }
                }
            }

            // ���� ���������� ������� ����� 1, �� ������� ������ �������� ��������
            return Materials[0].mainTexture;
        }

        private void PlayEffects(Vector3 HitPoint, Vector3 HitNormal, SurfaceEffect SurfaceEffect, float SoundOffset)
        {
            int i = 0;
            // ������� ������ ������������
            foreach (SpawnObjectEffect spawnObjectEffect in SurfaceEffect.SpawnObjectEffects)
            {
                // ��������� �� ����������� ��������� ������� ��������� ��������
                if (spawnObjectEffect.Probability > Random.value)
                {
                    if (!ObjectPools.ContainsKey(spawnObjectEffect.Prefab))
                    {
                        ObjectPools.Add(spawnObjectEffect.Prefab, new ObjectPool<GameObject>(() => Instantiate(spawnObjectEffect.Prefab)));
                    }

                    GameObject instance = ObjectPools[spawnObjectEffect.Prefab].Get();

                    if (instance.TryGetComponent(out PoolableObject poolable))
                    {
                        poolable.Parent = ObjectPools[spawnObjectEffect.Prefab];
                    }

                    GameObject parent = GetObjectAtPoint(HitPoint);
                    MeshCollider smr = null;
                    GameObject parentWithAnim = null;
                    if (parent != null && i == 0)
                    {
                        smr = parent.GetComponentInChildren<MeshCollider>();

                        if (smr != null && smr.GetComponent<EnemyHealth>() != null)
                        {
                            instance.transform.SetParent(smr.transform, true);
                        }
                        /*Transform parentTransform = parent.transform;
                        while (parentTransform != null)
                        {
                            parentWithAnim = parentTransform.gameObject;
                            parentTransform = parentWithAnim.transform.parent;

                            if (parentWithAnim.GetComponent<Animator>() != null)
                            {
                                smr = parentWithAnim.GetComponentInChildren<MeshCollider>();
                                break;
                            }
                        }*/
                    }

                    instance.SetActive(true);
                    instance.transform.position = HitPoint + HitNormal * 0.001f;
                    instance.transform.forward = HitNormal; // ������� ������� ����� �� ����������� �����

                    if(i == 0)
                    {
                        float size = Random.Range(spawnObjectEffect.minSize, spawnObjectEffect.maxSize);
                        instance.transform.localScale = new Vector3(size, size, size);
                        StartCoroutine(FadeOut(instance));
                    }
                
                    /*if (smr != null)
                    {
                        StartCoroutine(UpdateParticles(smr, instance, 0.1f));
                    }*/

                    // ������������ ��������� ������� �������
                    if (spawnObjectEffect.RandomizeRotation)
                    {
                        Vector3 offset = new Vector3(
                            Random.Range(0, 180 * spawnObjectEffect.RandomizedRotationMultiplier.x),
                            Random.Range(0, 180 * spawnObjectEffect.RandomizedRotationMultiplier.y),
                            Random.Range(0, 180 * spawnObjectEffect.RandomizedRotationMultiplier.z)
                        );

                        instance.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + offset);
                    }
                }

                i++;
            }

            // ������� ������ ������
            foreach (PlayAudioEffect playAudioEffect in SurfaceEffect.PlayAudioEffects)
            {
                if (!ObjectPools.ContainsKey(playAudioEffect.AudioSourcePrefab.gameObject))
                {
                    ObjectPools.Add(playAudioEffect.AudioSourcePrefab.gameObject, new ObjectPool<GameObject>(() => Instantiate(playAudioEffect.AudioSourcePrefab.gameObject)));
                }

                AudioClip clip = playAudioEffect.AudioClips[Random.Range(0, playAudioEffect.AudioClips.Count)];
                GameObject instance = ObjectPools[playAudioEffect.AudioSourcePrefab.gameObject].Get();
                instance.SetActive(true);
                AudioSource audioSource = instance.GetComponent<AudioSource>();

                audioSource.transform.position = HitPoint;
                audioSource.PlayOneShot(clip);
                // ��������� ����, ����� �� ��������� ����
                StartCoroutine(DisableAudioSource(ObjectPools[playAudioEffect.AudioSourcePrefab.gameObject], audioSource, clip.length));
            }
        }

        IEnumerator FadeOut(GameObject instance)
        {
            MeshRenderer renderer = instance.GetComponent<MeshRenderer>();

            int fadeTime = Mathf.CeilToInt(FadeTime.Evaluate(0, Random.value));

            // ������� ��������� ������� � ������� ��������� �������
            for (float t = 0.0f; t < fadeTime; t += Time.deltaTime)
            {
                Color color = renderer.material.color;
                color.a = Mathf.Lerp(1f, 0f, t / fadeTime);
                renderer.material.color = color;
                yield return null;
            }

            // ����������� ������� ����� ���������
            Destroy(instance);
        }

        /*private IEnumerator UpdateParticles(MeshCollider meshCollider, GameObject particleObject, float Time)
        {
            *//*while(instance.activeSelf)
            {*/
        /*Mesh m = new Mesh();
        smr.BakeMesh(m);

        Vector3[] vertices = m.vertices;
        Mesh m2 = new Mesh();
        m2.vertices = vertices;*//*

        //instance.transform.position = parent.transform.position;
        *//*}*//*

        // �������� ������� ����� �� Mesh Collider, � ������� ����� ���������� Particle
        //Vector3 position = meshCollider.ClosestPoint(particleObject.transform.position);

        // ��������� ������� Particle
        //particleObject.transform.position = position;

        yield return new WaitForSeconds(Time);
    }*/

        /* void OnAnimatorIK()
         {
             // �������� ������� � ����������� ������ ������ �� ������ ���������
             foreach (KeyValuePair<GameObject, ObjectPool<GameObject>> poolEntry in ObjectPools)
             {
                 GameObject key = poolEntry.Key;  // ���� (GameObject)
                 ObjectPool<GameObject> value = poolEntry.Value;  // �������� (ObjectPool<GameObject>)

                 if(key.active)
                 {
                     GameObject parent = ObjectPoolsParent[key];
                     GameObject parentWithAnim = null;
                     Transform parentTransform = parent.transform;
                     while (parentTransform != null)
                     {
                         parentWithAnim = parentTransform.gameObject;
                         parentTransform = parentWithAnim.transform.parent;

                         if(parentWithAnim.GetComponent<Animator>() != null)
                         {
                             break;
                         }
                     }

                     Animator animator = parentWithAnim.GetComponent<Animator>();
                     AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                     GameObject instance = ObjectPools[key].Get();
                     instance.transform.position = animator.GetIKPosition(AvatarIKGoal.LeftHand);
                 }
             }
         }*/

        private GameObject GetObjectAtPoint(Vector3 point)
        {
            Collider[] colliders = Physics.OverlapSphere(point, 0.1f);
            foreach (Collider collider in colliders)
            {
                GameObject obj = collider.gameObject;
                if (obj != null)
                {
                    return obj;
                }
            }
            return null;
        }

        private IEnumerator DisableAudioSource(ObjectPool<GameObject> Pool, AudioSource AudioSource, float Time)
        {
            yield return new WaitForSeconds(Time);

            AudioSource.gameObject.SetActive(false);
            Pool.Release(AudioSource.gameObject);
        }

        private class TextureAlpha
        {
            public float Alpha;
            public Texture Texture;
        }
    }
}