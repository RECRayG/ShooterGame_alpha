using Guns;
using MyStarterAssets;
using ImpactSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace Guns
{
    // Настройка оружия
    [CreateAssetMenu(fileName = "Gun", menuName = "Guns/Gun", order = 0)]
    public class GunScriptableObject : ScriptableObject
    {
        public ImpactType ImpactType;
        public GunType Type; // Тип оружия
        public string Name; // Название оружия
        public GameObject ModelPrefab; // 
        public Vector3 SpawnPoint; // Локальная точка координат оружия (просто значения Transform Pistol)
        public Vector3 SpawnRotation; // Локальный поворот оружия
        public bool isShootWeapon; // Ручное оружие или дальнобойное
        public bool isRecoil;
        public Sprite weaponSprite;
        public Sprite weaponCrosshair;
        public PlayerGunSelector playerGunSelector;

        public DamageConfigScriptableObject DamageConfig;
        public ShootConfigScriptableObject ShootConfig; // Ссылка на конфигурацию выстрела
        public AmmoConfigScriptableObject AmmoConfig;
        public TrailConfigScriptableObject TrailConfig; // Ссылка на конфигурацию следа от выстрела
        public AudioConfigScriptableObject AudioConfig;

        private MonoBehaviour ActiveMonoBehaviour; // Частное моноповедение
        private AudioSource ShootingAudioSource;
        private GameObject Model; // Частная модель игры - текущая модель
        //private Camera ActiveCamera;
        private float LastShootTime; // Время последнего выстрела
        private float InitialClickTime; // Время нажатия кнопки выстрела
        private float StopShootingTime; // Время остановки нажатия кнопки выстрела

        private ParticleSystem[] ShootSystem; // Система частиц выстрела
        private ObjectPool<TrailRenderer> TrailPool; // Частный пул объектов
        private DamageDealer damageDealer; // Частный пул объектов
        //private ObjectPool<Bullet> BulletPool;
        private bool LastFrameWantedToShoot; // Последний кадр стрельбы

        private ThirdPersonShootController thirdPersonShootController; // Ссылка на контроллер персонажа
        private Transform spawnBulletPosition; // Ссылка на объект, содержащий позицию начала выстрела
        private PlayerInputActionsCode playerInputActionsCode; // Ссылка на класс отслеживания нажатий

        /// <summary>
        /// Spawns the Gun Model into the scene
        /// </summary>
        /// <param name="Parent">Parent for the gun model</param>
        /// <param name="ActiveMonoBehaviour">An Active MonoBehaviour that can have Coroutines attached to them.
        /// <param name="Camera">The camera to raycast from. Required if <see cref="ShootConfigScriptableObject.ShootType"/> = <see cref="ShootType.FromCamera"/></paramref>
        /// The input handling script is a good candidate for this.
        /// </param>
        /// То, где появляется оружие при его выборе
        public void Spawn(Transform Parent, MonoBehaviour ActiveMonoBehaviour/*, Camera Camera = null*/)
        {
            this.ActiveMonoBehaviour = ActiveMonoBehaviour;

            // in editor these will not be properly reset, in build it's fine
            LastShootTime = 0;
            //StopShootingTime = 0;
            //InitialClickTime = 0;
            /*if(isShootWeapon)
            {
                AmmoConfig.CurrentClipAmmo = AmmoConfig.ClipSize;
                AmmoConfig.CurrentAmmo = AmmoConfig.MaxAmmo;
            }*/

            /*if (!ShootConfig.IsHitscan)
            {
                BulletPool = new ObjectPool<Bullet>(CreateBullet);
            }*/

            Model = Instantiate(ModelPrefab);
            Model.transform.SetParent(Parent, false);
            Model.transform.localPosition = SpawnPoint;
            Model.transform.localRotation = Quaternion.Euler(SpawnRotation);

            //ActiveCamera = Camera;

            //ShootingAudioSource = Model.GetComponent<AudioSource>();
            if (isShootWeapon)
            {
                TrailPool = new ObjectPool<TrailRenderer>(CreateTrail);
                ShootSystem = Model.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem particleSystem in ShootSystem)
                {
                    particleSystem.enableEmission = false;
                }
            }
            else
            {
                damageDealer = Model.GetComponentInChildren<DamageDealer>();
            }

            ShootingAudioSource = Model.GetComponent<AudioSource>();
        }

        public void DeleteWeapon()
        {
            Destroy(Model);
        }

        /// <summary>
        /// Used to override the Camera provided in <see cref="Spawn(Transform, MonoBehaviour, Camera)"/>.
        /// Cameras are only required if 
        /// </summary>
        /// <param name="Camera"></param>
        /*public void UpdateCamera(Camera Camera)
        {
            ActiveCamera = Camera;
        }*/

        /// <summary>
        /// Expected to be called every frame
        /// </summary>
        /// <param name="WantsToShoot">Whether or not the player is trying to shoot</param>
        public void Tick(bool WantsToShoot, PlayerInputActionsCode playerInputActionsCode)
        {
            // Возврат оружия к исходному положению после отдачи
            Model.transform.localRotation = Quaternion.Lerp(
                Model.transform.localRotation,
                Quaternion.Euler(SpawnRotation),
                Time.deltaTime * ShootConfig.RecoilRecoverySpeed
            );

            // Если игрок хочет выстрелить
            if (WantsToShoot)
            {
                LastFrameWantedToShoot = true;
                if(!playerGunSelector.playerAction.IsReloading)
                {
                    TryToShoot(playerInputActionsCode);
                }
            }

            // Если игрок не хочет выстрелить и он стрелял до этого
            if (!WantsToShoot && LastFrameWantedToShoot)
            {
                StopShootingTime = Time.time;
                LastFrameWantedToShoot = false;
            }
        }

        /// <summary>
        /// Plays the reloading audio clip if assigned.
        /// Expected to be called on the first frame that reloading begins
        /// </summary>
        public void StartReloading()
        {
            AudioConfig.PlayReloadClip(ShootingAudioSource);
        }

        /// <summary>
        /// Handle ammo after a reload animation.
        /// ScriptableObjects can't catch Animation Events, which is how we're determining when the
        /// reload has completed, instead of using a timer
        /// </summary>
        public void EndReload()
        {
            AmmoConfig.Reload();
            playerGunSelector.UpdateWeaponUI();
        }

        /// <summary>
        /// Whether or not this gun can be reloaded
        /// </summary>
        /// <returns>Whether or not this gun can be reloaded</returns>
        public bool CanReload()
        {
            return AmmoConfig.CanReload();
        }

        /// <summary>
        /// Performs the shooting raycast if possible based on gun rate of fire. Also applies bullet spread and plays sound effects based on the AudioConfig.
        /// </summary>
        /// Вызов выстрела
        public void TryToShoot(PlayerInputActionsCode playerInputActionsCode)
        {
            this.playerInputActionsCode = playerInputActionsCode;
            if (playerInputActionsCode.aim)
            {
                // Если оружие огнестрелное
                if (isShootWeapon)
                {
                    if (Time.time - LastShootTime - ShootConfig.FireRate > Time.deltaTime)
                    {
                        float lastDuration = Mathf.Clamp(
                            0,
                            (StopShootingTime - InitialClickTime),
                            ShootConfig.MaxSpreadTime
                        );
                        float lerpTime = (ShootConfig.RecoilRecoverySpeed - (Time.time - StopShootingTime))
                            / ShootConfig.RecoilRecoverySpeed;

                        InitialClickTime = Time.time - Mathf.Lerp(0, lastDuration, Mathf.Clamp01(lerpTime));
                    }

                    // Выстрел производится только после того, как прошло время задержки
                    if (Time.time > ShootConfig.FireRate + LastShootTime)
                    {
                        LastShootTime = Time.time;

                        if (AmmoConfig.CurrentClipAmmo == 0)
                        {
                            AudioConfig.PlayOutOfAmmoClip(ShootingAudioSource);
                            return;
                        }

                        AudioConfig.PlayShootingClip(ShootingAudioSource, AmmoConfig.CurrentClipAmmo == 1);

                        foreach (ParticleSystem particleSystem in ShootSystem)
                        {
                            particleSystem.enableEmission = true;
                            particleSystem.Play();
                        }
                        //ShootSystem.Play();

                        //AudioConfig.PlayShootingClip(ShootingAudioSource, AmmoConfig.CurrentClipAmmo == 1);

                        //Vector3 spreadAmount = ShootConfig.GetSpread(Time.time - InitialClickTime);

                        /*Vector3 shootDirection = Vector3.zero;
                        Model.transform.forward += Model.transform.TransformDirection(spreadAmount);
                        if (ShootConfig.ShootType == ShootType.FromGun)
                        {
                            shootDirection = ShootSystem.transform.forward;
                        }
                        else
                        {
                            shootDirection = ActiveCamera.transform.forward + ActiveCamera.transform.TransformDirection(spreadAmount);
                        }

                        AmmoConfig.CurrentClipAmmo--;

                        if (ShootConfig.IsHitscan)
                        {
                            DoHitscanShoot(shootDirection);
                        }
                        else
                        {
                            DoProjectileShoot(shootDirection);
                        }*/

                        ///////////////Мой код///////////////////////////
                        AmmoConfig.CurrentClipAmmo--;
                        playerGunSelector.UpdateWeaponUI();

                        Vector3 shootDirection = Vector3.zero;
                        spawnBulletPosition = GameObject.FindGameObjectWithTag("SpawnBulletPistol").GetComponent<Transform>();
                        thirdPersonShootController = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonShootController>();
                        // Направление стрельбы (случайный диапазон по всем координатам, например: x: -0.1 и x: 0.1)
                        for (int i = 0; i < ShootConfig.CountOfBullet; i++)
                        {
                            if (!isRecoil)
                            {
                                shootDirection = (thirdPersonShootController.GetMouseWorldPosition() - spawnBulletPosition.position)/*ShootSystem[1].transform.forward*/
                                + new Vector3(
                                    Random.Range(-ShootConfig.Spread.x,
                                                ShootConfig.Spread.x
                                                ),
                                    Random.Range(-ShootConfig.Spread.y,
                                                ShootConfig.Spread.y
                                                ),
                                    Random.Range(-ShootConfig.Spread.z,
                                                ShootConfig.Spread.z
                                                )
                                    );
                                shootDirection.Normalize(); // Направление выстрела длиной = 1
                            }
                            else
                            {
                                Vector3 spreadAmount = ShootConfig.GetSpread(ShootConfig, Time.time - InitialClickTime); // Подсчёт направления в зависимости от отдачи
                                Model.transform.forward += Model.transform.TransformDirection(spreadAmount);
                                //shootDirection.Normalize(); // Направление выстрела длиной = 1

                                shootDirection = Model.transform.forward;
                            }

                            // Передача луча из точки положения выстрела системы частиц
                            if (Physics.Raycast(
                                    spawnBulletPosition.position,
                                    shootDirection,
                                    out RaycastHit hit,
                                    float.MaxValue,
                                    ShootConfig.HitMask
                                ))
                            // Если возвращает true, значит, во что-то попали
                            {
                                ActiveMonoBehaviour.StartCoroutine(
                                    PlayTrail(
                                        spawnBulletPosition.position, // StartPoint
                                        hit.point, // EndPoint
                                        hit,
                                        shootDirection
                                    )
                                );
                            }
                            // Если возвращает false, значит, не попали
                            else
                            {
                                ActiveMonoBehaviour.StartCoroutine(
                                    PlayTrail(
                                        (thirdPersonShootController.GetMouseWorldPosition() - spawnBulletPosition.position).normalized, // StartPoint
                                        (thirdPersonShootController.GetMouseWorldPosition() - spawnBulletPosition.position).normalized + (shootDirection * TrailConfig.MissDistance), // EndPoint
                                        new RaycastHit(),
                                        shootDirection
                                    )
                                );
                            }
                        }
                        ///////////////Мой код///////////////////////////
                    }
                }
                // Если оружие ближнего боя
                else
                {
                    if (Time.time - LastShootTime - ShootConfig.FireRate > Time.deltaTime)
                    {
                        float lastDuration = Mathf.Clamp(
                            0,
                            (StopShootingTime - InitialClickTime),
                            ShootConfig.MaxSpreadTime
                        );
                        float lerpTime = (ShootConfig.RecoilRecoverySpeed - (Time.time - StopShootingTime))
                            / ShootConfig.RecoilRecoverySpeed;

                        InitialClickTime = Time.time - Mathf.Lerp(0, lastDuration, Mathf.Clamp01(lerpTime));
                    }

                    // Выстрел производится только после того, как прошло время задержки
                    if (Time.time > ShootConfig.FireRate + LastShootTime)
                    {
                        LastShootTime = Time.time;

                        if (Type.Equals(GunType.BaseballBat))
                        {
                            AudioConfig.PlayShootingClip(ShootingAudioSource);
                        }
                    }
                    
                    /*RaycastHit hit;

                    Debug.Log(ShootConfig.HitMask.value.ToString());
                    if (Physics.Raycast(Model.transform.position, -Model.transform.up, out hit, damageDealer.weaponLength, ShootConfig.HitMask))
                    {
                        Debug.Log("Raycast true");
                        if (!damageDealer.hasDealDamage.Contains(hit.transform.gameObject))
                        {
                            Debug.Log("Damage");
                            damageDealer.hasDealDamage.Add(hit.transform.gameObject);
                        }
                    }
                    else
                    {
                        Debug.Log("Raycast false");
                    }*/
                }
            }
        }

        /// <summary>
        /// Generates a live Bullet instance that is launched in the <paramref name="ShootDirection"/> direction
        /// with velocity from <see cref="ShootConfigScriptableObject.BulletSpawnForce"/>.
        /// </summary>
        /// <param name="ShootDirection"></param>
        /*private void DoProjectileShoot(Vector3 ShootDirection)
        {
            Bullet bullet = BulletPool.Get();
            bullet.gameObject.SetActive(true);
            bullet.OnCollsion += HandleBulletCollision;

            // We have to ensure if shooting from the camera, but shooting real proejctiles, that we aim the gun at the hit point
            // of the raycast from the camera. Otherwise the aim is off.
            // When shooting from the gun, there's no need to do any of this because the recoil is already handled in TryToShoot
            if (ShootConfig.ShootType == ShootType.FromCamera
                && Physics.Raycast(
                    GetRaycastOrigin(),
                    ShootDirection,
                    out RaycastHit hit,
                    float.MaxValue,
                    ShootConfig.HitMask
                ))
            {
                Vector3 directionToHit = (hit.point - ShootSystem.transform.position).normalized;
                Model.transform.forward = directionToHit;
                ShootDirection = directionToHit;
            }

            bullet.transform.position = ShootSystem.transform.position;
            bullet.Spawn(ShootDirection * ShootConfig.BulletSpawnForce);

            TrailRenderer trail = TrailPool.Get();
            if (trail != null)
            {
                trail.transform.SetParent(bullet.transform, false);
                trail.transform.localPosition = Vector3.zero;
                trail.emitting = true;
                trail.gameObject.SetActive(true);
            }
        }*/

        /// <summary>
        /// Performs a Raycast to determine if a shot hits something. Spawns a TrailRenderer
        /// and will apply impact effects and damage after the TrailRenderer simulates moving to the
        /// hit point. 
        /// See <see cref="PlayTrail(Vector3, Vector3, RaycastHit)"/> for impact logic.
        /// </summary>
        /// <param name="ShootDirection"></param>
        /*private void DoHitscanShoot(Vector3 ShootDirection)
        {
            if (Physics.Raycast(
                    GetRaycastOrigin(),
                    ShootDirection,
                    out RaycastHit hit,
                    float.MaxValue,
                    ShootConfig.HitMask
                ))
            {
                ActiveMonoBehaviour.StartCoroutine(
                    PlayTrail(
                        ShootSystem.transform.position,
                        hit.point,
                        hit
                    )
                );
            }
            else
            {
                ActiveMonoBehaviour.StartCoroutine(
                    PlayTrail(
                        ShootSystem.transform.position,
                        ShootSystem.transform.position + (ShootDirection * TrailConfig.MissDistance),
                        new RaycastHit()
                    )
                );
            }
        }*/

        /// <summary>
        /// Returns the proper Origin point for raycasting based on <see cref="ShootConfigScriptableObject.ShootType"/>
        /// </summary>
        /// <returns></returns>
        /*public Vector3 GetRaycastOrigin()
        {
            Vector3 origin = ShootSystem.transform.position;

            if (ShootConfig.ShootType == ShootType.FromCamera)
            {
                origin = ActiveCamera.transform.position
                    + ActiveCamera.transform.forward * Vector3.Distance(
                            ActiveCamera.transform.position,
                            ShootSystem.transform.position
                        );
            }

            return origin;
        }*/

        /// <summary>
        /// Returns the forward of the spawned gun model
        /// </summary>
        /// <returns></returns>
        /*public Vector3 GetGunForward()
        {
            return Model.transform.forward;
        }*/

        /// <summary>
        /// Plays a bullet trail/tracer from start/end point. 
        /// If <paramref name="Hit"/> is not an empty hit, it will also play an impact using the <see cref="SurfaceManager"/>.
        /// </summary>
        /// <param name="StartPoint">Starting point for the trail</param>
        /// <param name="EndPoint">Ending point for the trail</param>
        /// <param name="Hit">The hit object. If nothing is hit, simply pass new RaycastHit()</param>
        /// <returns>Coroutine</returns>
        private IEnumerator PlayTrail(Vector3 StartPoint, Vector3 EndPoint, RaycastHit Hit, Vector3 shootDirection)
        {
            TrailRenderer instance = TrailPool.Get(); // Получение последнего экземпляра элемента следа от пуль
            instance.gameObject.SetActive(true); // Делаем его актвным
            instance.transform.position = StartPoint; // Начинает своё движение из начальной точки
            // avoid position carry-over from last frame if reused
            yield return null; // Ждём 1 кадр, чтобы след от пули не возвращался к оружию

            instance.emitting = true;

            float distance = Vector3.Distance(StartPoint, EndPoint); // Рывок следа от пули от начальной точки к конечной
            float remainingDistance = distance;
            // Пока текущая точка не дошла до конечной
            while (remainingDistance > 0)
            {
                // То, как быстро вылетают пули (векторная 3-хмерная траектория, в которой задаётся скорость)
                instance.transform.position = Vector3.Lerp(
                    StartPoint,
                    EndPoint,
                    Mathf.Clamp01(1 - (remainingDistance / distance)) // Постоянная скорость
                );
                // Двигаем след от пули
                remainingDistance -= TrailConfig.SimulationSpeed * Time.deltaTime;

                // Ждём 1 кадр
                yield return null;
            }

            // Устанавливаем точку положения ренддеринга следа от пули точно в конечной точке
            instance.transform.position = EndPoint;

            // Если пуля попала во что-то
            if (Hit.collider != null)
            {
                /*if (Hit.collider.GetComponent<BulletTarget>() != null)
                {
                    Debug.Log("Kill");
                }
                else
                {
                    Debug.Log("Miss");
                }*/
                SurfaceManager.Instance.HandleImpact(
                    Hit.transform.gameObject,
                    EndPoint,
                    Hit.normal,
                    ImpactType,
                    Hit.triangleIndex,
                    Type
                );

                if(Hit.collider.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(DamageConfig.GetDamage(distance), StartPoint/*, shootDirection, Hit.point*/);
                }
            }

            // Ждём 1 дополнительный кадр для исчезновения
            yield return new WaitForSeconds(TrailConfig.Duration);
            yield return null;
            instance.emitting = false;
            // Объект неактивен
            instance.gameObject.SetActive(false);
            // Удаление объекта из пула следов от пуль
            TrailPool.Release(instance);
        }

        /// <summary>
        /// Callback handler for <see cref="Bullet.OnCollsion"/>. Disables TrailRenderer, releases the 
        /// Bullet from the BulletPool, and applies impact effects if <paramref name="Collision"/> is not null.
        /// </summary>
        /// <param name="Bullet"></param>
        /// <param name="Collision"></param>
        /*private void HandleBulletCollision(Bullet Bullet, Collision Collision)
        {
            TrailRenderer trail = Bullet.GetComponentInChildren<TrailRenderer>();
            if (trail != null)
            {
                trail.transform.SetParent(null, true);
                ActiveMonoBehaviour.StartCoroutine(DelayedDisableTrail(trail));
            }

            Bullet.gameObject.SetActive(false);
            BulletPool.Release(Bullet);

            if (Collision != null)
            {
                ContactPoint contactPoint = Collision.GetContact(0);

                HandleBulletImpact(
                    Vector3.Distance(contactPoint.point, Bullet.SpawnLocation),
                    contactPoint.point,
                    contactPoint.normal,
                    contactPoint.otherCollider
                );
            }
        }*/

        /// <summary>
        /// Disables the trail renderer based on the <see cref="TrailConfigScriptableObject.Duration"/> provided
        ///and releases it from the<see cref="TrailPool"/>
        /// </summary>
        /// <param name="Trail"></param>
        /// <returns></returns>
        /*private IEnumerator DelayedDisableTrail(TrailRenderer Trail)
        {
            yield return new WaitForSeconds(TrailConfig.Duration);
            yield return null;
            Trail.emitting = false;
            Trail.gameObject.SetActive(false);
            TrailPool.Release(Trail);
        }*/

        /// <summary>
        /// Calls <see cref="SurfaceManager.HandleImpact(GameObject, Vector3, Vector3, ImpactType, int)"/> and applies damage
        /// if a damagable object was hit
        /// </summary>
        /// <param name="DistanceTraveled"></param>
        /// <param name="HitLocation"></param>
        /// <param name="HitNormal"></param>
        /// <param name="HitCollider"></param>
        /*private void HandleBulletImpact(
            float DistanceTraveled,
            Vector3 HitLocation,
            Vector3 HitNormal,
            Collider HitCollider)
        {
            SurfaceManager.Instance.HandleImpact(
                    HitCollider.gameObject,
                    HitLocation,
                    HitNormal,
                    ImpactType,
                    0
                );

            if (HitCollider.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(DamageConfig.GetDamage(DistanceTraveled));
            }
        }*/

        /// <summary>
        /// Creates a trail Renderer for use in the object pool.
        /// </summary>
        /// <returns>A live TrailRenderer GameObject</returns>
        /// Создание следа от выстрела - вызывается, когда нужно получить новый объект из пула, но его нет
        private TrailRenderer CreateTrail()
        {
            GameObject instance = new GameObject("Bullet Trail");
            TrailRenderer trail = instance.AddComponent<TrailRenderer>();
            trail.colorGradient = TrailConfig.Color;
            trail.material = TrailConfig.Material;
            trail.widthCurve = TrailConfig.WidthCurve;
            trail.time = TrailConfig.Duration;
            trail.minVertexDistance = TrailConfig.MinVertexDistance;

            trail.emitting = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return trail;
        }

        public bool isShooting()
        {
            return isShootWeapon;
        }

        public GameObject GetModelPrefab()
        {
            return ModelPrefab;
        }

        public ShootConfigScriptableObject GetShootConfig()
        {
            return ShootConfig;
        }

        public void SetPlayerGunSelector(PlayerGunSelector playerGunSelector)
        {
            this.playerGunSelector = playerGunSelector;
        }

        /// <summary>
        /// Creates a Bullet for use in the object pool.
        /// </summary>
        /// <returns>A live Bullet GameObject</returns>
        /*private Bullet CreateBullet()
        {
            return Instantiate(ShootConfig.BulletPrefab);
        }*/
    }
}