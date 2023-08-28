using Guns;
using System.Collections;
using System.Collections.Generic;
using ImpactSystem;
using UnityEngine;

public class EnemyDamageDealer : MonoBehaviour
{
    public bool canDealDamage;
    public bool hasDealDamage;

    [SerializeField]
    public float weaponLength;
    [SerializeField]
    public float weaponDamage;

    [SerializeField]
    private LayerMask HitMask;

    private void Start()
    {
        canDealDamage = false;
        hasDealDamage = false;
    }

    private void Update()
    {
        if (canDealDamage && !hasDealDamage)
        {
            if(Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, weaponLength, HitMask))
            {
                if(hit.transform.TryGetComponent(out PlayerHealth health))
                {
                    health.Damage(weaponDamage);
                    hasDealDamage = true;
                }
            }
        }
    }

    public void StartDealDamage()
    {
        canDealDamage = true;
    }

    public void EndDealDamage()
    {
        hasDealDamage = false;
        canDealDamage = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position - transform.up * weaponLength);
    }
}
