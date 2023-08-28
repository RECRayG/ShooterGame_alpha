using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Guns
{
    public class WeaponPickUp : MonoBehaviour
    {
        [SerializeField] 
        private PlayerGunSelector playerGunSelector;
        [SerializeField]
        private GunScriptableObject GunForGet;
        [SerializeField]
        private AmmoConfigScriptableObject ammoConfigScriptableObject;
        [SerializeField]
        private float audioValue = 0.5f;
        [SerializeField]
        private AudioClip audioPickUp;

        private AudioSource audioSource;
        
        private void Start()
        {
            playerGunSelector = FindObjectOfType<PlayerGunSelector>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.tag == "Player")
            {
                if(!playerGunSelector.playerAction.IsReloading)
                {
                    if (GunForGet.Type.Equals(GunType.Pistol) || GunForGet.Type.Equals(GunType.Shotgun))
                    {
                        ammoConfigScriptableObject.CurrentAmmo = 0;
                        ammoConfigScriptableObject.CurrentClipAmmo = ammoConfigScriptableObject.ClipSize;
                    }

                    Destroy(gameObject.transform.parent.gameObject);
                    playerGunSelector.Guns.Add(GunForGet);
                    playerGunSelector.SetWeapon(GunForGet);
                    playerGunSelector.UpdateWeaponUI();

                    audioSource = other.GetComponent<AudioSource>();
                    audioSource.PlayOneShot(audioPickUp, audioValue);
                }
            }
        }
    }
}
