using Guns;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class WeaponUI : MonoBehaviour
{
    [SerializeField]
    private PlayerGunSelector playerGunSelector;

    [SerializeField]
    private Image icon;

    [SerializeField]
    private TextMeshProUGUI clipSize;

    [SerializeField]
    private TextMeshProUGUI ammoSize;

    [SerializeField]
    private TextMeshProUGUI slash;

    [SerializeField]
    private Image ammoBackground;

    [SerializeField]
    private Image iconBackground;

    [SerializeField]
    private Image pistolCrosshair;

    [SerializeField]
    private Image shotgunCrosshair;

    [SerializeField]
    private Image noneCrosshair;

    [SerializeField]
    private float fadeSpeed = 1.5f;
    public bool isDead = false;

    private void Update()
    {
        if(isDead)
        {
            FadeImage(icon);
            FadeTMP(clipSize);
            FadeTMP(ammoSize);
            FadeTMP(slash);
            FadeImage(ammoBackground);
            FadeImage(iconBackground);
            FadeImage(pistolCrosshair);
            FadeImage(shotgunCrosshair);
            FadeImage(noneCrosshair);

            //WaveInfo
            FadeImage(WaveSystem.instance.waveCountImage);
            FadeImage(WaveSystem.instance.enemyCountLeftImage);
            FadeImage(WaveSystem.instance.enemyCountAllImage);
            /*FadeTMP(WaveSystem.instance.waveCountText);
            FadeTMP(WaveSystem.instance.enemyCountLeftText);
            FadeTMP(WaveSystem.instance.enemyCountAllText);*/

            FadeImage(WaveSystem.instance.nextWaveInfoImage);
            FadeTMP(WaveSystem.instance.nextWaveInfoTimer);
            FadeTMP(WaveSystem.instance.skipWave);
            FadeTMP(WaveSystem.instance.seconds);
            FadeTMP(WaveSystem.instance.nextWaveInfoText);

            //AppendImage(WaveSystem.instance.deathScreenExitButtonBG);
            //AppendImage(WaveSystem.instance.deathScreenExitButtonText);
        }
    }

    private void AppendImage(Image I)
    {
        var color = I.color;
        //color.a -= fadeSpeed * Time.deltaTime;

        color.a = Mathf.Lerp(color.a, 255, fadeSpeed * Time.deltaTime);
        I.color = color;
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

    public void UpdateInfo(Sprite weaponIcon)
    {
        icon.sprite = weaponIcon;

        if(playerGunSelector.ActiveGun.Type.Equals(GunType.None) ||
           playerGunSelector.ActiveGun.Type.Equals(GunType.BaseballBat))
        {
            slash.enabled = false;
            clipSize.enabled = false;
            ammoSize.enabled = false;
            ammoBackground.enabled = false;
            pistolCrosshair.enabled = false;
            shotgunCrosshair.enabled = false;

            noneCrosshair.enabled = true;
        } 
        else
        {
            slash.enabled = true;
            clipSize.enabled = true;
            ammoSize.enabled = true;
            ammoBackground.enabled = true;

            clipSize.text = playerGunSelector.ActiveGun.AmmoConfig.CurrentClipAmmo.ToString();
            ammoSize.text = playerGunSelector.ActiveGun.AmmoConfig.CurrentAmmo.ToString();

            if(playerGunSelector.ActiveGun.Type.Equals(GunType.Pistol))
            {
                shotgunCrosshair.enabled = false;
                noneCrosshair.enabled = false;

                pistolCrosshair.enabled = true;
            }
            else if (playerGunSelector.ActiveGun.Type.Equals(GunType.Shotgun))
            {
                pistolCrosshair.enabled = false;
                noneCrosshair.enabled = false;

                shotgunCrosshair.enabled = true;
            }
        }
    }
}
