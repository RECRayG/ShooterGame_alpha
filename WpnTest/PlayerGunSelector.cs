using Guns;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Guns
{
    [DisallowMultipleComponent]
    public class PlayerGunSelector : MonoBehaviour
    {
        /*public Camera Camera;*/
        [SerializeField]
        protected GunType Gun;
        [SerializeField]
        protected Transform GunParent;
        [SerializeField]
        public List<GunScriptableObject> Guns;
        [SerializeField]
        protected WeaponScrolling weaponScrolling;
        [SerializeField]
        public PlayerAction playerAction;

        [SerializeField]
        private WeaponUI weaponUI;
        /*[SerializeField]
        private PlayerIK InverseKinematics;*/

        [Space]
        [Header("Runtime Filled")]
        public GunScriptableObject ActiveGun;
        public int indexOfWeaponAtList;
        public bool isShootWeapon;
        public int countGuns;

        protected GameObject[] activateAiming;

        private PlayerHealth playerHealth;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            GunScriptableObject gun = Guns.Find(gun => gun.Type == Gun);

            if (gun == null)
            {
                Debug.LogError($"No GunScriptableObject found for GunType: {gun}");
                return;
            }

            indexOfWeaponAtList = Guns.FindIndex(gun => gun.Type == Gun);
            ActiveGun = gun;
            countGuns = Guns.Count;
            isShootWeapon = ActiveGun.isShooting();

            /*// Активировать все объекты анимации для выбранного тип оружия
            if (ActiveGun.Type.Equals(GunType.Pistol))
            {
                activateAiming = GameObject.FindGameObjectsWithTag("PistolAiming");
                foreach(GameObject go in activateAiming)
                {
                    go.SetActive(true);
                }
            } 
            else if (ActiveGun.Type.Equals(GunType.Shotgun))
            {
                activateAiming = GameObject.FindGameObjectsWithTag("ShotgunAiming");
                foreach (GameObject go in activateAiming)
                {
                    go.SetActive(true);
                }
            }*/

            if(!gun.Type.Equals(GunType.None))
            {
                gun.Spawn(GunParent, this/*, Camera*/);
            }

            weaponUI.UpdateInfo(ActiveGun.weaponSprite);
            gun.SetPlayerGunSelector(this);

            // some magic for IK
            /*Transform[] allChildren = GunParent.GetComponentsInChildren<Transform>();
            InverseKinematics.LeftElbowIKTarget = allChildren.FirstOrDefault(child => child.name == "LeftElbow");
            InverseKinematics.RightElbowIKTarget = allChildren.FirstOrDefault(child => child.name == "RightElbow");
            InverseKinematics.LeftHandIKTarget = allChildren.FirstOrDefault(child => child.name == "LeftHand");
            InverseKinematics.RightHandIKTarget = allChildren.FirstOrDefault(child => child.name == "RightHand");*/

        }

        public void UpdateWeaponUI()
        {
            weaponUI.UpdateInfo(ActiveGun.weaponSprite);
        }

        private void Update()
        {
            // Можно менять оружие только тогда, когда игрок не перезаряжается и он ещё жив, а также игра не на паузе
            if(!playerAction.IsReloading && !playerHealth.isDead && !PauseController.instance.isPause)
            {
                if (weaponScrolling.isScrollUp())
                {
                    GunScriptableObject gun = null;
                    if (!ActiveGun.Type.Equals(GunType.None))
                    {
                        ActiveGun.DeleteWeapon();
                    }
                    if (indexOfWeaponAtList + 1 >= Guns.Count)
                    {
                        indexOfWeaponAtList = Guns.Count - 1;
                        gun = Guns.ElementAt(indexOfWeaponAtList);
                    }
                    else
                    {
                        indexOfWeaponAtList += 1;
                        gun = Guns.ElementAt(indexOfWeaponAtList);
                    }

                    ActiveGun = gun;
                    Gun = gun.Type;
                    if (!ActiveGun.Type.Equals(GunType.None))
                    {
                        gun.Spawn(GunParent, this);
                    }
                    isShootWeapon = ActiveGun.isShooting();

                    weaponUI.UpdateInfo(ActiveGun.weaponSprite);
                    gun.SetPlayerGunSelector(this);
                }
                else if (weaponScrolling.isScrollDown())
                {
                    GunScriptableObject gun = null;
                    if (!ActiveGun.Type.Equals(GunType.None))
                    {
                        ActiveGun.DeleteWeapon();
                    }
                    if (indexOfWeaponAtList - 1 <= 0)
                    {
                        indexOfWeaponAtList = 0;
                        gun = Guns.ElementAt(indexOfWeaponAtList);
                    }
                    else
                    {
                        indexOfWeaponAtList -= 1;
                        gun = Guns.ElementAt(indexOfWeaponAtList);
                    }

                    ActiveGun = gun;
                    Gun = gun.Type;
                    if (!ActiveGun.Type.Equals(GunType.None))
                    {
                        gun.Spawn(GunParent, this);
                    }
                    isShootWeapon = ActiveGun.isShooting();

                    weaponUI.UpdateInfo(ActiveGun.weaponSprite);
                    gun.SetPlayerGunSelector(this);
                }
            }
        }

        public void SetWeapon(GunScriptableObject newGun)
        {
            /*ActiveGun.DeleteWeapon();

            ActiveGun = newGun;
            Gun = newGun.Type;
            if (!ActiveGun.Type.Equals(GunType.None))
            {
                newGun.Spawn(GunParent, this);
            }
            isShootWeapon = ActiveGun.isShooting();

            weaponUI.UpdateInfo(ActiveGun.weaponSprite);
            newGun.SetPlayerGunSelector(this);*/



            if (!ActiveGun.Type.Equals(GunType.None))
            {
                ActiveGun.DeleteWeapon();
            }

            int tempIndex = Guns.FindIndex(gun => gun == newGun);
            if(tempIndex != -1)
            {
                indexOfWeaponAtList = tempIndex;
                ActiveGun = newGun;
                Gun = newGun.Type;
                if (!ActiveGun.Type.Equals(GunType.None))
                {
                    newGun.Spawn(GunParent, this);
                }
                isShootWeapon = ActiveGun.isShooting();

                weaponUI.UpdateInfo(newGun.weaponSprite);
                newGun.SetPlayerGunSelector(this);
            }
        }

        public GunScriptableObject GetActiveGun()
        {
            return ActiveGun;
        }

        public bool isShooting()
        {
            return isShootWeapon;
        }

        public GunType GetGunType()
        {
            return Gun;
        }
    }
}