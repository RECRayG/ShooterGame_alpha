using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using NTC.Global.Pool;

namespace Guns.Enemy
{
    [DisallowMultipleComponent]
    public class Enemy : MonoBehaviour, IPoolItem
    {
        //[SerializeField] private float timeToWaitBeforeDown = 5f; // Время, через которое начнётся погружение под землю
        private NavMeshAgent navMeshAgent;

        public List<EnemyHealth> Health;
        /*public EnemyMovement Movement;*/
        public EnemyPainResponse PainResponse;
        private Rigidbody[] ragdollRigidbodies;
        private CharacterJoint[] characterJoints;
        private MeshCollider[] characterColliders;
        [SerializeField]
        private Animator animator;
        [SerializeField] 
        private Transform debugTransform;
        private Transform playerBody;
        public float enemySpeedRun;

        [Header("Combat")]
        [SerializeField] float attackPause = 3f; // Пауза между атаками
        [SerializeField] float attackRange = 1f; // Расстояние до активации атаки

        float timePassed;
        float newDestinationPause = 0.5f; // Время восстановления


        [SerializeField]
        private bool audioBindAnimation = false;

        [SerializeField]
        private bool audioBindAnimationBegin = true;

        [SerializeField]
        public AudioClip[] enemyAttackSounds;

        [SerializeField]
        public AudioClip[] enemyDeathSounds;

        [SerializeField]
        public bool isEducation = false;

        private PlayerHealth playerHealth;

        private AudioSource enemyAudioSource;

        private int randomIndex = 0;

        public ProgressBar MainHealthBar;
        public ProgressBar HeadHealthBar;

        public GameObject MainHealth;
        public GameObject HeadHealth;


        public float randomRadiusMoveToPlayer = 10f;
        public NavMeshPath navMeshPath;
        public Transform nearPlayer;
        public Transform target;
        public Vector3 randomPoint;

        public Vector3 randomPointToSpawn;

        public bool enemyIsDie = false;

        public void SetRandomPointToSpawn(Vector3 value)
        {
            randomPointToSpawn = value;
            //navMeshAgent.Warp(value);
        }

        /*private void Awake()
        {
            MainHealth = transform.parent.gameObject.GetComponentInChildren<MainHealth>().gameObject;
            HeadHealth = transform.parent.gameObject.GetComponentInChildren<HeadHealth>().gameObject;

            MainHealthBar = MainHealth.GetComponentInChildren<ProgressBar>();
            HeadHealthBar = HeadHealth.GetComponentInChildren<ProgressBar>();
        }*/

        /*private void Awake()
        {
            ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            characterJoints = GetComponentsInChildren<CharacterJoint>();
            characterColliders = GetComponentsInChildren<MeshCollider>();
            animator = GetComponent<Animator>();
            foreach (var rigidbody in ragdollRigidbodies)
            {
                rigidbody.isKinematic = true;
            }
        }*/

        private void Start()
        {
            navMeshPath = new NavMeshPath();
            navMeshAgent = transform.root.GetComponent<NavMeshAgent>();
            playerBody = GameObject.FindGameObjectWithTag("Player").transform;
            nearPlayer = playerBody;
            target = playerBody;

            enemyAudioSource = GetComponent<AudioSource>();
            playerHealth = playerBody.GetComponentInChildren<PlayerHealth>();

            Health.ForEach(p => { 
                p.OnTakeDamage += PainResponse.HandlePain; 
                p.OnDeath += Die; 
            });

            StartCoroutine(Move_COR());
        }

        private IEnumerator Move_COR()
        {
            // Пока враг ещё жив
            while (!PainResponse.isDie)
            {
                yield return new WaitForSeconds(0.1f);
                if(Vector3.Distance(transform.position, playerBody.position) > randomRadiusMoveToPlayer)
                {
                    if (Vector3.Distance(nearPlayer.position, playerBody.position) > randomRadiusMoveToPlayer)
                    {
                        GoToNearRandomPoint();
                    }
                }
                else
                {
                    target = playerBody;
                }

                if (navMeshAgent != null && navMeshAgent.enabled)
                {
                    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Die"))
                    {
                        if (!PainResponse.isDie)
                        {
                            navMeshAgent.SetDestination(playerBody.position);
                            navMeshAgent.speed = enemySpeedRun;
                            animator.SetFloat("Speed", 1f);
                        }
                        else
                        {
                            navMeshAgent.speed = 0f;
                            animator.SetFloat("Speed", 0f);
                        }
                    }
                    else
                    {
                        navMeshAgent.speed = 0f;
                        animator.SetFloat("Speed", 0f);
                    }
                }
                
            }
        }

        private void GoToNearRandomPoint()
        {
            bool getCorrectPoint = false;
            while(!getCorrectPoint)
            {
                NavMeshHit hit;
                NavMesh.SamplePosition(Random.insideUnitSphere * randomRadiusMoveToPlayer + playerBody.position, out hit, randomRadiusMoveToPlayer, NavMesh.AllAreas);
                randomPoint = hit.position;

                if(randomPoint.y >= 0 && randomPoint.y <= 0.1)
                {
                    navMeshAgent.CalculatePath(randomPoint, navMeshPath);
                    if(navMeshPath.status == NavMeshPathStatus.PathComplete &&
                        !NavMesh.Raycast(playerBody.position, randomPoint, out hit, NavMesh.AllAreas))
                    {
                        getCorrectPoint = true;
                    }
                }
            }
            nearPlayer.position = randomPoint;
            target = nearPlayer;
        }

        private void Update()
        {
			if(!PainResponse.isDie)
			{
				Vector3 directionR = playerBody.transform.position - transform.position;
				Quaternion rotationR = Quaternion.LookRotation(directionR, Vector3.up);
				transform.parent.transform.rotation = rotationR;
			}
			
            if(playerHealth != null && !playerHealth.isDead)
            {
                if (timePassed >= attackPause)
                {
                    if (Vector3.Distance(playerBody.transform.position, transform.position) <= attackRange)
                    {
						/*Vector3 directionR = playerBody.transform.position - transform.position;
						Quaternion rotationR = Quaternion.LookRotation(directionR, Vector3.up);
						transform.parent.transform.rotation = rotationR;*/
				
                        animator.SetTrigger("Attack");

                        if (!audioBindAnimation && !audioBindAnimationBegin)
                        {
                            randomIndex = Random.Range(0, enemyAttackSounds.Length);
                            enemyAudioSource.clip = enemyAttackSounds[randomIndex];
                            AudioManager.instance.Play(gameObject.transform.parent.gameObject.name + "_Hit" + randomIndex, enemyAudioSource);
                        }

                        timePassed = 0f;
                    }
                }
                else
                {
                    if (Vector3.Distance(playerBody.transform.position, transform.position) <= attackRange)
                    {
                        Vector3 direction = playerBody.transform.position - transform.position;
                        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                        transform.rotation = rotation;
                    }
                }
                timePassed += Time.deltaTime;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        public void StartDealDamage()
        {
            if (audioBindAnimation && !audioBindAnimationBegin)
            {
                randomIndex = Random.Range(0, enemyAttackSounds.Length);
                enemyAudioSource.clip = enemyAttackSounds[randomIndex];
                AudioManager.instance.Play(gameObject.transform.parent.gameObject.name + "_Hit" + randomIndex, enemyAudioSource);
            }

            EnemyDamageDealer[] dealers = GetComponentsInChildren<EnemyDamageDealer>();
            foreach(EnemyDamageDealer dealer in dealers)
            {
                dealer.StartDealDamage();
            }

            //GunSelector.ActiveGun.TryToShoot(playerInputActionsCode);
        }

        public void EndDealDamage()
        {
            EnemyDamageDealer[] dealers = GetComponentsInChildren<EnemyDamageDealer>();
            foreach (EnemyDamageDealer dealer in dealers)
            {
                dealer.EndDealDamage();
            }
        }

        public void PlayAudioAttack()
        {
            if (!audioBindAnimation && audioBindAnimationBegin)
            {
                randomIndex = Random.Range(0, enemyAttackSounds.Length);
                enemyAudioSource.clip = enemyAttackSounds[randomIndex];
                AudioManager.instance.Play(gameObject.transform.parent.gameObject.name + "_Hit" + randomIndex, enemyAudioSource);
            }
        }

        /*private void OnTriggerEnter(Collider other)
        {
            if(other.gameObject.CompareTag("DebugSphere"))
            {
                //Transform head = transform.parent.Find("Head Health");
                //Transform main = transform.parent.Find("Main Health");

                if (MainHealth != null)
                {
                    MainHealth.SetActive(true);
                }

                if (HeadHealth != null)
                {
                    HeadHealth.SetActive(true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("DebugSphere"))
            {
                //Transform head = transform.parent.Find("Head Health");
                //Transform main = transform.parent.Find("Main Health");

                if (MainHealth != null)
                {
                    MainHealth.SetActive(false);
                }

                if (HeadHealth != null)
                {
                    HeadHealth.SetActive(false);
                }
            }
        }*/

        /*
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("DebugSphere"))
            {
                Transform head = transform.parent.Find("Head Health");
                Transform main = transform.parent.Find("Main Health");

                if (head != null)
                {
                    head.gameObject.SetActive(false);
                }

                if (main != null)
                {
                    main.gameObject.SetActive(false);
                }
            }
        }
*/


        private void Die(Vector3 Position, Vector3 playerPosition/*, Vector3 direction, Vector3 hitPoint, float damageTaken*/)
        {
            /*Movement.StopMoving();*/
            /*Rigidbody hitRigidbody = ragdollRigidbodies.OrderBy(rigidbody => Vector3.Distance(rigidbody.position, hitPoint)).First();

            float speed = Mathf.Sqrt((damageTaken * 2) / hitRigidbody.mass);
            float forceMagnitude = speed * hitRigidbody.mass;

            EnemyHealth enemyHealth = hitRigidbody.GetComponent<EnemyHealth>();
            if(enemyHealth != null)
            {
                float percentForce = damageTaken / enemyHealth.MaxHealth;
                float forceMagnitude2 = Mathf.Lerp(1, forceMagnitude, percentForce);

                Vector3 forceDirection = transform.position - direction;
                forceDirection.y = transform.position.y + 1;
                forceDirection.Normalize();

                PainResponse.HandleDeath(hitRigidbody, forceDirection * forceMagnitude2, hitPoint);
                EnableRagdoll();
            }*/
            if(!enemyIsDie)
            {
                enemyAudioSource.clip = enemyDeathSounds[0];
                AudioManager.instance.Play(gameObject.transform.parent.gameObject.name + "_Death", enemyAudioSource);

                attackRange = 0f;
                PainResponse.HandleDeath(playerPosition);
                enemyIsDie = true;
            }
        }

        private void DisableRagdoll()
        {
            foreach(var rigidbody in ragdollRigidbodies)
            {
                //rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
                rigidbody.useGravity = false;
            }

            foreach(var join in characterJoints)
            {
                join.enableCollision = false;
            }

            animator.enabled = true;
        }

        private void EnableRagdoll()
        {
            foreach (var rigidbody in ragdollRigidbodies)
            {
                rigidbody.isKinematic = false;
                rigidbody.detectCollisions = true;
                rigidbody.useGravity = true;
                rigidbody.velocity = Vector3.zero;
            }

            foreach (var join in characterJoints)
            {
                join.enableCollision = true;
            }

            animator.enabled = false;

            //StartCoroutine(DisableMeshes());
        }

        public void SpawnEnemy()
        {
            transform.parent.position = randomPointToSpawn;
            navMeshAgent.Warp(randomPointToSpawn);
        }

        public void OnSpawn()
        {
            enemyIsDie = false;

            transform.parent.position = new Vector3(transform.parent.position.x, 0f, transform.parent.position.z);
            //navMeshAgent.Warp(randomPointToSpawn);
            

            // При спавне восстановить зомби параметры здоровья
            Health.ForEach(h => {
                // Если это голова, то восстановить здоровье в индикаторах здоровья
                // Гарантируется, что у всех есть хотя бы меш 1 головы
                if (h.isHead)
                {
                    if (h.MainHealthBar != null)
                    {
                        h.MainHealthBar.gameObject.transform.parent.gameObject.SetActive(true);
                        h.MainHealthBar.SetProgress(1);

                        //NightPool.Spawn(h.MainHealthBar);
                    }

                    if (h.HeadHealthBar != null)
                    {
                        h.HeadHealthBar.gameObject.transform.parent.gameObject.SetActive(true);
                        h.HeadHealthBar.SetProgress(1);

                        //NightPool.Spawn(h.HeadHealthBar);
                    }
                }
                h.CurrentHealth = h.MaxHealth;
            });

            if (navMeshAgent != null)
                navMeshAgent.enabled = true;
        }

        public void OnDespawn()
        {
            enemyIsDie = true;


            //animator.ResetTrigger("Reborn");
            //throw new System.NotImplementedException();


            
            //transform.position = Vector3.zero;

            /* if(MainHealth != null)
             {
                 MainHealth.SetActive(true);
                 MainHealthBar.SetProgress(1);
             }

             if(HeadHealth != null)
             {
                 HeadHealth.SetActive(true);
                 HeadHealthBar.SetProgress(1);
             }*/

            // Сбросить анимации
            animator.SetTrigger("Reborn");
            PainResponse.isDie = false;
            if (navMeshAgent != null)
                navMeshAgent.enabled = false;
        }

        /*private IEnumerator DisableMeshes()
        {
            yield return new WaitForSeconds(timeToWaitBeforeDown);

            foreach (var rigidbody in ragdollRigidbodies)
            {
                rigidbody.isKinematic = true;
            }

            *//*foreach (var collider in characterColliders)
            {
                collider.enabled = false;
            }*//*

            StartCoroutine(PainResponse.MoveDownAndDestroy());
        }*/
    }
}