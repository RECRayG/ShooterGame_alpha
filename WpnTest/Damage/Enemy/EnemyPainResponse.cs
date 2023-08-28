using Google.Protobuf.WellKnownTypes;
using NTC.Global.Pool;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Guns.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class EnemyPainResponse : MonoBehaviour
    {
        [SerializeField]
        private List<EnemyHealth> Health;
        private List<EnemyHealth> tempListHealth;
        private PlayerGunSelector playerGunSelector;
        private Animator Animator;
        [SerializeField]
        [Range(1, 100)]
        private int MaxDamagePainThreshold = 5;

        [SerializeField] private float duration = 12f; // Длительность перемещения объекта вниз
        [SerializeField] public float depth = 0.5f; // Глубина, на которую перемещается объект
        [SerializeField] private float timeToWaitBeforeDown = 5f; // Время, через которое начнётся погружение под землю
        [SerializeField] private List<GameObject> dropAfterDeath;

        [SerializeField] private float chanceToDropPistolAmmo = 0.65f;
        [SerializeField] private float chanceToDropShotgunAmmo = 0.35f;

        [SerializeField] float spawnOffset = 1f;
        [SerializeField] float jumpForce = 0.2f;

        private int index;
        private AnimatorControllerParameter[] parameters;

        public bool isDie = false;

        private void Start()
        {
            Animator = GetComponent<Animator>();
            playerGunSelector = FindObjectOfType<PlayerGunSelector>();
            parameters = Animator.parameters;
        }

        public void HandlePain(int Damage)
        {
            // you can do some cool stuff based on the
            // amount of damage taken relative to max health
            // here we're simply setting the additive layer
            // weight based on damage vs max pain threshhold

            index = Health.FindIndex(p => p.CurrentHealth == 0);
            // Если нет ни 1 части тела, которая полностью потеряла здоровье
            if(index == -1)
            {
                // Приравниваем все части тела друг другу, кроме головы
                float minHealth = 0;
                int minTemp = Health.Min(h => h.CurrentHealth);
                minHealth = minTemp;

                if(Health.Count > 1)
                {
                    Health.ForEach(h => {
                        if (h.isHead)
                        {
                            if (minTemp == h.CurrentHealth)
                            {
                                tempListHealth = new List<EnemyHealth>(Health);
                                tempListHealth.Remove(h);
                                minHealth = tempListHealth.Min(h => h.CurrentHealth);
                            }
                        }
                    });
                }

                int i = 0;
                Health.ForEach(p => {
                    if (i == 0)
                    {
                        if(p.MainHealthBar != null && p.MainHealthBar.gameObject.active)
                        {
                            p.MainHealthBar.SetProgress((float)(minHealth / p.MaxHealth), 3);
                        }
                    }

                    if (!p.isHead)
                    {
                        p.CurrentHealth = (int) minHealth;
                    } 
                    else
                    {
                       /* if (p.CurrentHealth > (int) minHealth)
                        {
                            p.CurrentHealth = (int) minHealth;
                        }*/

                        if (p.HeadHealthBar != null && p.HeadHealthBar.gameObject.active)
                        {
                            float headHealth = p.CurrentHealth;
                            p.HeadHealthBar.SetProgress((float)(headHealth / p.MaxHealth), 3);
                        }
                    }

                    i++;
                });

                /* Animator.ResetTrigger("Hit");
                 Animator.SetLayerWeight(1, (float)Damage / MaxDamagePainThreshold);
                 Animator.SetTrigger("Hit");*/
                if (playerGunSelector.ActiveGun.Type.Equals(GunType.BaseballBat) && !Animator.GetCurrentAnimatorStateInfo(0).IsName("Zombie Dying"))
                {
                    foreach (AnimatorControllerParameter parameter in parameters)
                    {
                        if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == "Hit")
                        {
                            Animator.ResetTrigger("Hit");
                            Animator.SetLayerWeight(1, (float)Damage / MaxDamagePainThreshold);
                            Animator.SetTrigger("Hit");
                        }
                    }
                }
            }
        }

        public void HandleDeath(Vector3 playerPosition/*Rigidbody hitRigidbody, Vector3 force, Vector3 hitPoint*/)
        {
            if(!Animator.GetCurrentAnimatorStateInfo(0).IsName("Zombie Dying"))
            {
                /*Debug.Log("Force: " + force);
                Debug.Log("hitPoint: " + hitPoint);
                hitRigidbody.AddForceAtPosition(force * 1000f, hitPoint, ForceMode.Impulse);*/

                /*Animator.applyRootMotion = true;*/
                Animator.SetTrigger("Die");
                //Destroy(gameObject.transform.parent.gameObject, 6f);

                int i = 0;

                bool isPistol = false;
                bool isShotgun = false;

                // Если у игрока есть пистолет в инвентаре
                if(playerGunSelector.Guns.FindIndex(p => p.Type == GunType.Pistol) != -1)
                {
                    isPistol = true;
                }
                
                // Если у игрока есть дробовик в инвентаре
                if (playerGunSelector.Guns.FindIndex(p => p.Type == GunType.Shotgun) != -1)
                {
                    isShotgun = true;
                }

                if(isPistol && isShotgun)
                {
                    if(Random.value < chanceToDropPistolAmmo)
                    {
                        GunScriptableObject pistol = playerGunSelector.Guns.Find(gun => gun.Type.Equals(GunType.Pistol));

                        float currAmmo = pistol.AmmoConfig.CurrentAmmo;
                        float currClipAmmo = pistol.AmmoConfig.CurrentClipAmmo;
                        float maxAmmo = pistol.AmmoConfig.MaxAmmo;

                        float chanceToDrop = 1f - (currAmmo + currClipAmmo) / maxAmmo;
                        
                        if (Random.value < chanceToDrop)
                        {
                            i = 0;
                            GameObject[] temp = dropAfterDeath.ToArray();
                            for (int j = 0; j < temp.Length; j++)
                            {
                                if (j == i)
                                {
                                    //Instantiate(temp[j], transform.position /*+ Vector3.up * spawnOffset*/, Quaternion.identity);
                                    
                                    GameObject newObj = Instantiate(temp[j], transform.position + Vector3.up * spawnOffset, Quaternion.identity);
                                    Rigidbody rb = newObj.GetComponent<Rigidbody>();
                                    Vector3 direction = (playerPosition - newObj.transform.position).normalized;
                                    rb.AddForce(direction * jumpForce, ForceMode.Impulse);
                                    rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
                                }
                            }
                        }
                    } 
                    else
                    {
                        GunScriptableObject shotgun = playerGunSelector.Guns.Find(gun => gun.Type.Equals(GunType.Shotgun));
                        float currAmmo = shotgun.AmmoConfig.CurrentAmmo;
                        float currClipAmmo = shotgun.AmmoConfig.CurrentClipAmmo;
                        float maxAmmo = shotgun.AmmoConfig.MaxAmmo;

                        float chanceToDrop = 1f - (currAmmo + currClipAmmo) / maxAmmo;

                        if (Random.value < chanceToDrop)
                        {
                            i = 1;
                            GameObject[] temp = dropAfterDeath.ToArray();
                            for (int j = 0; j < temp.Length; j++)
                            {
                                if (j == i)
                                {
                                    //Instantiate(temp[j], transform.position /*+ Vector3.up * spawnOffset*/, Quaternion.identity);
                                    
                                    GameObject newObj = Instantiate(temp[j], transform.position + Vector3.up * spawnOffset, Quaternion.identity);
                                    Rigidbody rb = newObj.GetComponent<Rigidbody>();
                                    Vector3 direction = (playerPosition - newObj.transform.position).normalized;
                                    rb.AddForce(direction * jumpForce, ForceMode.Impulse);
                                    rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
                                }
                            }
                        }
                    }
                } 
                else if(isPistol)
                {
                    GunScriptableObject pistol = playerGunSelector.Guns.Find(gun => gun.Type.Equals(GunType.Pistol));
                    float currAmmo = pistol.AmmoConfig.CurrentAmmo;
                    float currClipAmmo = pistol.AmmoConfig.CurrentClipAmmo;
                    float maxAmmo = pistol.AmmoConfig.MaxAmmo;

                    float chanceToDrop = 1f - (currAmmo + currClipAmmo) / maxAmmo;

                    if (Random.value < chanceToDrop)
                    {
                        i = 0;
                        GameObject[] temp = dropAfterDeath.ToArray();
                        for (int j = 0; j < temp.Length; j++)
                        {
                            if (j == i)
                            {
                                //Instantiate(temp[j], transform.position /*+ Vector3.up * spawnOffset*/, Quaternion.identity);
                                
                                GameObject newObj = Instantiate(temp[j], transform.position + Vector3.up * spawnOffset, Quaternion.identity);
                                Rigidbody rb = newObj.GetComponent<Rigidbody>();
                                Vector3 direction = (playerPosition - newObj.transform.position).normalized;
                                rb.AddForce(direction * jumpForce, ForceMode.Impulse);
                                rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
                            }
                        }
                    }
                }
                else if(isShotgun)
                {
                    GunScriptableObject shotgun = playerGunSelector.Guns.Find(gun => gun.Type.Equals(GunType.Shotgun));
                    float currAmmo = shotgun.AmmoConfig.CurrentAmmo;
                    float currClipAmmo = shotgun.AmmoConfig.CurrentClipAmmo;
                    float maxAmmo = shotgun.AmmoConfig.MaxAmmo;

                    float chanceToDrop = 1f - (currAmmo + currClipAmmo) / maxAmmo;

                    if (Random.value < chanceToDrop)
                    {
                        i = 1;
                        GameObject[] temp = dropAfterDeath.ToArray();
                        for (int j = 0; j < temp.Length; j++)
                        {
                            if (j == i)
                            {
                                //Instantiate(temp[j], transform.position /*+ Vector3.up * spawnOffset*/, Quaternion.identity);
                                
                                GameObject newObj = Instantiate(temp[j], transform.position + Vector3.up * spawnOffset, Quaternion.identity);
                                Rigidbody rb = newObj.GetComponent<Rigidbody>();
                                Vector3 direction = (playerPosition - newObj.transform.position).normalized;
                                rb.AddForce(direction * jumpForce, ForceMode.Impulse);
                                rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
                            }
                        }
                    }
                }

                /*dropAfterDeath.ForEach(prefab => {
                            if (i == 1)
                            {
                                //float spawnOffset = 1f;
                                //float jumpForce = 0.5f;

                                *//*GameObject newObj = *//*
                                Instantiate(prefab, transform.position *//*+ Vector3.up * spawnOffset*//*, Quaternion.identity);
                                //Rigidbody rb = newObj.GetComponent<Rigidbody>();
                                //Vector3 direction = (playerPosition - newObj.transform.position).normalized;
                                //rb.AddForce(Vector3.up * jumpForce - direction, ForceMode.Impulse);
                            }

                            i++;
                        });*/

                isDie = true;

                WaveSystem.instance.current_summons_alive--;
                WaveSystem.instance.current_summons_dead++;
                WaveSystem.instance.allEnemies++;

                StartCoroutine(MoveDownAndDestroy());
            }
        }

        public IEnumerator MoveDownAndDestroy()
        {
            yield return new WaitForSeconds(timeToWaitBeforeDown);

            float elapsedTime = 0; // Прошедшее время
            Vector3 startingPos = transform.position; // Исходная позиция объекта

            while (elapsedTime < duration) // Пока не истечет длительность перемещения
            {
                float t = elapsedTime / duration; // Процент времени, прошедшего с начала перемещения
                transform.position = Vector3.Lerp(startingPos, startingPos - new Vector3(0, depth, 0), t); // Плавно перемещаем объект вниз

                elapsedTime += Time.deltaTime; // Увеличиваем прошедшее время на время кадра
                yield return null; // Ждем следующего кадра
            }

            NightPool.Despawn(gameObject.transform.parent); // Уничтожаем объект после того, как он достиг глубины
        }
    }
}