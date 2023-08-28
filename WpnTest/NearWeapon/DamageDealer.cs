using Guns;
using ImpactSystem;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public static bool canDealDamage; // ����� �� �������� ����
    public static List<GameObject> hasDealDamage; // ������ ��������, ���������� ����
    public static bool isGetDamage;

    [SerializeField] public float weaponLength; // ����� ������ (������ ����, ��������� �������� ����� ������������� ��� ����)
    //[SerializeField] public float weaponDamage; // ����, ��������� �������
    [SerializeField] GunScriptableObject gunScriptableObject;

    [SerializeField] DamageConfigScriptableObject DamageConfig;

    private void Start()
    {
        isGetDamage = false;
        canDealDamage = false; // ������ �������� ���� � ������ ������
        hasDealDamage = new List<GameObject>();
    }

    private void Update()
    {
        // ���� ����� ������� ����
        if (canDealDamage)
        {
            if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, weaponLength, gunScriptableObject.GetShootConfig().HitMask))
            {
                if (!hasDealDamage.Contains(hit.transform.gameObject))
                {
                    hasDealDamage.Add(hit.transform.gameObject);

                    if (!isGetDamage)
                    {
                        //Vector3 shootDirection = (transform.position - transform.up * weaponLength) - transform.position;
                        SurfaceManager.Instance.HandleImpact(
                            hit.transform.gameObject,
                            hit.point,
                            hit.normal,
                            gunScriptableObject.ImpactType,
                            hit.triangleIndex,
                            gunScriptableObject.Type
                        );

                        if(hit.collider.TryGetComponent(out IDamageable damageable))
                            damageable.TakeDamage(DamageConfig.GetDamage(), hit.point/*, shootDirection, hit.point*/);
                        
                        isGetDamage = true;
                    }
                }
            }
        }
    }

    public void StartDealDamage()
    {
        canDealDamage = true;
        //Update();
    }

    public void EndDealDamage()
    {
        isGetDamage = false;
        canDealDamage = false;
        hasDealDamage.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position - transform.up * weaponLength);
    }
}
