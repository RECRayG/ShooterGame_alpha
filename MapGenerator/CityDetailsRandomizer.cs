using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityDetailsRandomizer : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField]
    float chanceToSpawn = 0.5f;

    public void RandomizeCityDetails()
    {
        int randomNumber = 0;

        CityDetailId[] detailsMainIds = GetComponentsInChildren<CityDetailId>();
        foreach (CityDetailId details in detailsMainIds)
        {
            // ����� ���� �������� ��������, � �� ����������
            for (int i = 0; i < details.transform.childCount; i++)
            {
                Transform child = details.transform.GetChild(i);

                child.gameObject.SetActive(false);
            }

            // ���� ����������� ���� �� ����� �������, �� ����������
            if (Random.value < chanceToSpawn)
            {
                randomNumber = Random.Range(0, details.transform.childCount);

                details.transform.GetChild(randomNumber).gameObject.SetActive(true);
            }

            //Transform[] childs = details.GetComponentsInChildren<Transform>();
            // ����� ���� �������� ��������, � �� ��������� ���������
            //for (int i = 0; i < childs.Length; i++)
            //{
            // ���� ����������� ���� �� ����� �������, �� ����������
            //if (Random.value < chanceToSpawn)
            //{
            //randomNumber = Random.Range(0, details.childCount);

            //childs[i].GetChild(randomNumber).gameObject.SetActive(true);
            //}
            //}
        }
    }
}
