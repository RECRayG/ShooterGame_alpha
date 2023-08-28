/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int startingHealth = 100;

    private int CurrentHealth;
    private Vector3 StartPosition;

    // Start is called before the first frame update
    void Start()
    {
        StartPosition = transform.position;
        CurrentHealth = startingHealth;
    }

    public void GetShot(int damage, SurviveAgent surviveAgent)
    {
        ApplyDamage(damage, surviveAgent);
    }

    private void ApplyDamage(int damage, SurviveAgent surviveAgent)
    {
        CurrentHealth -= damage;

        if(CurrentHealth <= 0)
        {
            Die(surviveAgent);
        }
    }

    private void Die(SurviveAgent surviveAgent)
    {
        Debug.Log("I died!");
        surviveAgent.RegisterKill();
        Respawn();
    }

    private void Respawn()
    {
        CurrentHealth = startingHealth;
        transform.position = StartPosition;
    }
}*/