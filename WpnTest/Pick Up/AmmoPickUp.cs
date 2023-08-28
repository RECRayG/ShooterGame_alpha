using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Guns
{
    public class AmmoPickUp : MonoBehaviour
    {
        [SerializeField]
        private AmmoConfigScriptableObject ammoConfigScriptableObject;
        [SerializeField]
        private GunType AmmoGunType;
        [SerializeField]
        private float timeToDisappear = 10f;

        private PlayerGunSelector playerGunSelector;

        [SerializeField]
        private float audioValue = 0.5f;
        [SerializeField]
        private AudioClip audioPickUp;

        private AudioSource audioSource;

        [Space]
        [Header("Runtime Filled")]
        public int AmmoCount;

        private void Awake()
        {
            playerGunSelector = FindObjectOfType<PlayerGunSelector>();

            if(AmmoGunType.Equals(GunType.Pistol))
            {
                AmmoCount = Random.Range(10, 20);
            }
            else if(AmmoGunType.Equals(GunType.Shotgun))
            {
                AmmoCount = Random.Range(4, 8);
            }

            StartCoroutine(TimeToDisappear());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Player")
            {
                // Если количество патрон после добавления превысит максимальный лимит (учитывая только патроны в запасе)
                if (AmmoCount + ammoConfigScriptableObject.CurrentAmmo > ammoConfigScriptableObject.MaxAmmo)
                {
                    // Если можно будет добавить патроны, но не все
                    if(ammoConfigScriptableObject.MaxAmmo - ammoConfigScriptableObject.CurrentAmmo > 0)
                    {
                        int fewAmmo = ammoConfigScriptableObject.MaxAmmo - ammoConfigScriptableObject.CurrentAmmo;
                        ammoConfigScriptableObject.CurrentAmmo += fewAmmo;
                        AmmoCount -= fewAmmo;

                        if(AmmoCount <= 0)
                        {
                            Destroy(gameObject.transform.parent.gameObject.transform.parent.gameObject);
                        }

                        playerGunSelector.UpdateWeaponUI();

                        audioSource = other.GetComponent<AudioSource>();
                        audioSource.PlayOneShot(audioPickUp, audioValue);
                    } // Если боезапас максимальный
                    else
                    {

                    }
                }
                else
                {
                    ammoConfigScriptableObject.CurrentAmmo += AmmoCount;
                    Destroy(gameObject.transform.parent.gameObject.transform.parent.gameObject);
                    playerGunSelector.UpdateWeaponUI();

                    audioSource = other.GetComponent<AudioSource>();
                    audioSource.PlayOneShot(audioPickUp, audioValue);
                }
            }
        }

        private IEnumerator TimeToDisappear()
        {
            yield return new WaitForSeconds(timeToDisappear);

            Destroy(gameObject.transform.parent.gameObject.transform.parent.gameObject);
        }
    }
}
