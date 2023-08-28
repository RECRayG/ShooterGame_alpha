using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    [SerializeField]
    private CinemachineFreeLook playerFollowCamera;
    [SerializeField]
    private CinemachineFreeLook aimCamera;
    float shakerTimer;
    float shakerTimerTotal;
    float startingItencity;

    private void Awake()
    {
        Instance = this;
    }

    public void ShakeCamera(float itencity, float time)
    {
        CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = playerFollowCamera.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = itencity;

        CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin2 = aimCamera.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        cinemachineBasicMultiChannelPerlin2.m_AmplitudeGain = itencity;

        startingItencity = itencity;
        shakerTimer = time;
        shakerTimerTotal = time;
    }

    private void Update()
    {
        if (shakerTimer > 0)
        {
            shakerTimer -= Time.deltaTime;
            CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = playerFollowCamera.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = Mathf.Lerp(startingItencity, 0f, 1f - shakerTimer / shakerTimerTotal);

            CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin2 = aimCamera.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            cinemachineBasicMultiChannelPerlin2.m_AmplitudeGain = Mathf.Lerp(startingItencity, 0f, 1f - shakerTimer / shakerTimerTotal);

        }
    }
}
