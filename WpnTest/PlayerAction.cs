//using Guns;
using MyStarterAssets;
//using Unity.VisualScripting;
//using UnityEditor.Animations;
using UnityEngine;
//using UnityEngine.InputSystem;
//using UnityEngine.UI;

namespace Guns
{
    [DisallowMultipleComponent]
    public class PlayerAction : MonoBehaviour
    {
        private PlayerInputActionsCode playerInputActionsCode;
        private MyThirdPersonController myThirdPersonController;
        // public for editor
        public PlayerGunSelector GunSelector;
        [SerializeField]
        private bool AutoReload = false;
        /*[SerializeField]
        private PlayerIK InverseKinematics;
        [SerializeField]
        private Image Crosshair;*/
        public bool IsReloading;
        [SerializeField] private Animator PlayerAnimator;

        private PlayerHealth playerHealth;
        //[SerializeField] private AudioSource baseballBatAudioSource;

        private void Awake()
        {
            playerInputActionsCode = GetComponent<PlayerInputActionsCode>();
            myThirdPersonController = GetComponent<MyThirdPersonController>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            // Если игрок ещё жив
            if(!playerHealth.isDead && !PauseController.instance.isPause)
            {
                myThirdPersonController.SetShootWeapon(GunSelector.isShooting());

                if (GunSelector.isShooting())
                {
                    // Если игрок нажал R или если патроны в обойме кончились
                    if (ShouldManualReload() || ShouldAutoReload())
                    {
                        // Перевоспроизвести анимацию, в зависимости от типа оружия
                        if (GunSelector.ActiveGun.Type.Equals(GunType.Pistol))
                        {
                            PlayerAnimator.Play("Reloading", 9, 0f);
                        }
                        else if (GunSelector.ActiveGun.Type.Equals(GunType.Shotgun))
                        {
                            PlayerAnimator.Play("Reloading", 10, 0f);
                        }
                        IsReloading = true;
                        PlayerAnimator.SetTrigger("Reload");

                        GunSelector.ActiveGun.StartReloading();
                    }

                    playerInputActionsCode.reload = false;
                    PlayerAnimator.ResetTrigger("Reload");
                }

                // Если оружие дальнобойное
                if (playerInputActionsCode.shoot && GunSelector.ActiveGun != null && GunSelector.isShooting() && !IsReloading)
                {
                    GunSelector.ActiveGun.Tick(playerInputActionsCode.shoot, playerInputActionsCode);
                    playerInputActionsCode.shoot = false;

                } // Если оружие ближнее
                else if (playerInputActionsCode.shoot && GunSelector.ActiveGun != null && !GunSelector.isShooting())
                {
                    /*if (GunSelector.ActiveGun.Type.Equals(GunType.BaseballBat) && baseballBatAudioSource != null)
                    {
                        GunSelector.ActiveGun.AudioConfig.PlayShootingClip(baseballBatAudioSource);
                    }*/

                    if (GunSelector.ActiveGun.Type.Equals(GunType.BaseballBat))
                    {
                        GunSelector.ActiveGun.Tick(playerInputActionsCode.shoot, playerInputActionsCode);
                    }

                    myThirdPersonController.SetAttackNearWeapon(true);
                    myThirdPersonController.SetShooting(true);
                    playerInputActionsCode.shoot = false;
                    //myThirdPersonController.SetAttackNearWeapon(false);
                }

                /*if (!playerInputActionsCode.shoot && GunSelector.ActiveGun != null && !GunSelector.isShooting())
                {
                    Debug.Log("Player Action Shoot: FALSE");
                    myThirdPersonController.SetAttackNearWeapon(false);
                }*/

                /*GunSelector.ActiveGun.Tick(
                    !IsReloading
                    && Application.isFocused && Mouse.current.leftButton.isPressed
                    && GunSelector.ActiveGun != null
                );

                if (ShouldManualReload() || ShouldAutoReload())
                {
                    GunSelector.ActiveGun.StartReloading();
                    IsReloading = true;
                    PlayerAnimator.SetTrigger("Reload");
                    InverseKinematics.HandIKAmount = 0.25f;
                    InverseKinematics.ElbowIKAmount = 0.25f;
                }

                UpdateCrosshair();*/
            }
        }

        public void StartDealDamage()
        {
            if (!GunSelector.isShooting())
                GunSelector.GetActiveGun().GetModelPrefab().GetComponentInChildren<DamageDealer>().StartDealDamage();

            //GunSelector.ActiveGun.TryToShoot(playerInputActionsCode);
        }

        public void EndDealDamage()
        {
            if (!GunSelector.isShooting())
                GunSelector.GetActiveGun().GetModelPrefab().GetComponentInChildren<DamageDealer>().EndDealDamage();
        }

        /*private void UpdateCrosshair()
        {
            if (GunSelector.ActiveGun.ShootConfig.ShootType == ShootType.FromGun)
            {
                Vector3 gunTipPoint = GunSelector.ActiveGun.GetRaycastOrigin();
                Vector3 forward = GunSelector.ActiveGun.GetGunForward();

                Vector3 hitPoint = gunTipPoint + forward * 10;
                if (Physics.Raycast(gunTipPoint, forward, out RaycastHit hit, float.MaxValue, GunSelector.ActiveGun.ShootConfig.HitMask))
                {
                    hitPoint = hit.point;
                }
                Vector3 screenSpaceLocation = GunSelector.Camera.WorldToScreenPoint(hitPoint);

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)Crosshair.transform.parent,
                    screenSpaceLocation,
                    null,
                    out Vector2 localPosition))
                {
                    Crosshair.rectTransform.anchoredPosition = localPosition;
                }
                else
                {
                    Crosshair.rectTransform.anchoredPosition = Vector2.zero;
                }
            }
        }*/

        private bool ShouldManualReload()
        {
            return !IsReloading
                && playerInputActionsCode.reload
                && GunSelector.ActiveGun.CanReload();
        }

        private bool ShouldAutoReload()
        {
            return !IsReloading
                && AutoReload
                && GunSelector.ActiveGun.AmmoConfig.CurrentClipAmmo == 0
                && GunSelector.ActiveGun.CanReload();
        }

        public void EndReload()
        {
            GunSelector.ActiveGun.EndReload();
            IsReloading = false;
        }
    }
}