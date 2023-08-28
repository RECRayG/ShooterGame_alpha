using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [SerializeField]
    public GameObject player;

    public void Spawn()
    {
        Instantiate(player, transform.position, Quaternion.identity);
    }
}
