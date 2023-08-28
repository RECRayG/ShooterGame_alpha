using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Cinemachine;
using MyStarterAssets;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField]
    public float _Health;
    [SerializeField]
    private float _MaxHealth = 100;

    [SerializeField]
    private TextMeshProUGUI percentOfHealth;

    [SerializeField]
    private Image healthBar;

    [SerializeField]
    private Image circleBorder;

    [SerializeField]
    private Image circleShadow;

    [SerializeField]
    private float lerpSpeedMultiplyer = 3f;

    [SerializeField]
    private int countOfAudioReact = 0;

    private float lerpSpeed;
    private float lerpSpeedVolume;
    private float lerpSpeedCamera;

    private Animator animator;
    [SerializeField]
    private WeaponUI weaponUI;
    [SerializeField]
    private float fadeSpeed = 1.5f;

    [SerializeField]
    private Volume damageVolume;
    private List<VolumeComponent> damageVolumeComponents;

    [SerializeField]
    private Volume deathVolume;
    private List<VolumeComponent> deathVolumeComponents;

    [SerializeField]
    private float lerpSpeedDeathVolume;
    [SerializeField]
    private float deathCameraSpeed;

    [SerializeField]
    private CinemachineVirtualCamera deathCamera;
    [SerializeField]
    private float deathCameraHeight;

    public bool isDead = false;
    private float cameraYPos;

    private PlayerInputActionsCode playerInputActionsCode;

    private void Start()
    {
        deathCamera.Priority = 0;
        damageVolume.weight = 0f;
        deathVolume.weight = 0f;
        animator = GetComponent<Animator>();
        playerInputActionsCode = GetComponent<PlayerInputActionsCode>();
    }

    private void OnEnable()
    {
        _Health = _MaxHealth;
    }

    private void Update()
    {
        percentOfHealth.text = _Health.ToString();

        lerpSpeed = lerpSpeedMultiplyer * Time.deltaTime;
        lerpSpeedVolume = lerpSpeedDeathVolume * Time.deltaTime;
        lerpSpeedCamera = deathCameraSpeed * Time.deltaTime;

        HealthBarFiller();
        ColorChanger();

        if(isDead)
        {
            ChangeDamageWeightAfterDeath();
            ChangeDeathWeight();
            ChangeCameraPosition();

            FadeImage(healthBar);
            FadeImage(circleBorder);
            FadeImage(circleShadow);
            FadeTMP(percentOfHealth);
        }
        else
        {
            ChangeDamageWeight();
        }
    }

    private void HealthBarFiller()
    {
        healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, _Health / _MaxHealth, lerpSpeed);
    }

    private void ColorChanger()
    {
        Color healthColor = Color.Lerp(Color.red, Color.green, (_Health / _MaxHealth));
        healthBar.color = healthColor;
    }

    public void Damage(float damagePoints)
    {
        if(_Health > 0)
        {
            playerInputActionsCode.aim = false;
            AudioManager.instance.Play("PlayerResponseDamage" + UnityEngine.Random.Range(0, countOfAudioReact));
            _Health -= damagePoints;
            CameraShake.Instance.ShakeCamera(2f, .1f);

            int temp = (int) _Health;
            _Health = temp;
        }
        
        if (_Health <= 0 && !isDead)
        {
            isDead = true;
            deathCamera.Priority = 30;
            cameraYPos = deathCamera.transform.position.y;
            animator.SetTrigger("Death");
            AudioManager.instance.Play("PlayerDeath");
            weaponUI.isDead = true;

            _Health = 0;
            
            Die();
        }
    }

    private void FadeImage(Image I)
    {
        var color = I.color;
        //color.a -= fadeSpeed * Time.deltaTime;
       
        color.a = Mathf.Lerp(color.a, 0, fadeSpeed * Time.deltaTime);
        I.color = color;
    }

    private void FadeTMP(TextMeshProUGUI TMP)
    {
        var color = TMP.color;
        //color.a -= fadeSpeed * Time.deltaTime;

        color.a = Mathf.Lerp(color.a, 0, fadeSpeed * Time.deltaTime);
        TMP.color = color;

        /*color.a -= fadeSpeed * Time.deltaTime;

        color.a = Mathf.Clamp(color.a, 0, 1);
        TMP.color = color;*/
    }

    private void ChangeDamageWeight()
    {
        damageVolume.weight = Mathf.Lerp(damageVolume.weight, 1 - (_Health / _MaxHealth), lerpSpeed);
    }

    private void ChangeDamageWeightAfterDeath()
    {
        damageVolume.weight = Mathf.Lerp(damageVolume.weight, 0, lerpSpeedVolume);
    }

    private void ChangeDeathWeight()
    {
        deathVolume.weight = Mathf.Lerp(deathVolume.weight, 1, lerpSpeedVolume);
    }

    private void ChangeCameraPosition()
    {
        if(Vector3.Distance(deathCamera.transform.position, transform.position) < deathCameraHeight)
        {
            deathCamera.transform.localPosition = Vector3.Lerp(deathCamera.transform.localPosition,
                                                        new Vector3(0,
                                                                    cameraYPos + deathCameraHeight,
                                                                    0),
                                                        lerpSpeedCamera);
        }
    }

    private void Die()
    {
        //Destroy(this.gameObject);
    }
}
