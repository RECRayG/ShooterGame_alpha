using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*using UnityEngine.Animations.Rigging;*/

public class CharacterAiming : MonoBehaviour
{
    public float turnSpeed = 15;
    /*public float aimDuration = 3f;
    public Rig aimLayer;*/
    Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float yawCamera = mainCamera.transform.rotation.eulerAngles.y;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yawCamera, 0), turnSpeed * Time.fixedDeltaTime);
    }

    /*private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            aimLayer.weight += Time.deltaTime / aimDuration * 50;
        }
        else
        {
            aimLayer.weight -= Time.deltaTime / aimDuration;
        }
    }*/
}
