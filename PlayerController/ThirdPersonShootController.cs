using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using MyStarterAssets;
using System;
using UnityEngine.Animations.Rigging;
using Guns;

public class ThirdPersonShootController : MonoBehaviour
{
    [SerializeField] private Rig rigPistol;
    [SerializeField] private List<GameObject> rigPistolTransform;

    [SerializeField] private Rig rigShotgun;
    [SerializeField] private List<GameObject> rigShotgunTransform;
    [SerializeField] private Rig rigShotgunMovement;
    [SerializeField] private List<GameObject> rigShotgunMovementTransform;
    [SerializeField] private Rig rigShotgunIdle;
    [SerializeField] private List<GameObject> rigShotgunIdleTransform;

    [SerializeField] private Rig rigBatAttack;
    [SerializeField] private List<GameObject> rigBatAttackTransform;

    [SerializeField] private CinemachineFreeLook aimFreeLook;
    [SerializeField] private CinemachineFreeLook playerFollowCamera;
    [SerializeField] private CinemachineVirtualCamera playerDeathCamera;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform debugTransform;

    //[SerializeField] public GameObject firePoint;
    //[SerializeField] public List<GameObject> vfx = new List<GameObject>();
    //[SerializeField] private GameObject effectToSpawn;
    [SerializeField] private Transform pfBulletProjectile;
    [SerializeField] private Transform spawnBulletPosition;

    [SerializeField] private PlayerGunSelector playerGunSelector;
    [SerializeField] private PlayerAction playerAction;

    [SerializeField] private float speed = 10f;
    private Rigidbody bulletRigidbody;
    private bool isActive = true;

    private MyThirdPersonController myThirdPersonController;
    private PlayerInputActionsCode playerInputActionsCode;
    private Animator animator;
    //private RigBuilder rigBuilder;

    Vector3 mouseWorldPosition = Vector3.zero;

    private List<Transform> heads = new();
    private List<Transform> mains = new();
    private bool enemyLookAt = false;
    [SerializeField]
    private float timeToEnemyHealthGone = 3f;

    private PlayerHealth playerHealth;

    private void Start()
    {
        //effectToSpawn = vfx[0];
        if (playerGunSelector.GetGunType().Equals(GunType.Pistol))
        {
            // ��������
            rigPistolTransform[0].SetActive(false);
            rigPistolTransform[1].SetActive(false);
            rigPistol.weight = 0f;
            animator.SetLayerWeight(2, 0f);
        }
        else if (playerGunSelector.GetGunType().Equals(GunType.Shotgun))
        {
            // ��������
            rigShotgunTransform[0].SetActive(false);
            rigShotgunTransform[1].SetActive(false);
            rigShotgun.weight = 0f;
            animator.SetLayerWeight(3, 0f);
        }
    }

    private void Awake()
    {
        myThirdPersonController = GetComponent<MyThirdPersonController>();
        playerInputActionsCode = GetComponent<PlayerInputActionsCode>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        //rigBuilder = GetComponent<RigBuilder>();
    }

    private IEnumerator EnemyHealthBarGone()
    {
        yield return new WaitForSeconds(timeToEnemyHealthGone);

        if(!enemyLookAt)
        {
            if(heads.Count > 0)
            {
                heads.ForEach( head => {
                    head.gameObject.SetActive(false);
                });

                heads.Clear();
            }

            if(mains.Count > 0)
            {
                mains.ForEach(main => {
                    main.gameObject.SetActive(false);
                });

                mains.Clear();
            }
        }
    }

    private void Update()
    {
        /*if (myThirdPersonController.GetAnimator().GetFloat("Speed") == 0f)
        {
            myThirdPersonController.GetAnimator().SetFloat("InputX", 0f, 0.1f, Time.deltaTime);
            myThirdPersonController.GetAnimator().SetFloat("InputY", 0f, 0.1f, Time.deltaTime);
        }*/

        mouseWorldPosition = Vector3.zero;

        // ���� ����� ���
        if(!playerHealth.isDead && animator != null)
        {
            Vector2 screenCenterPoint = new Vector3(Screen.width / 2f, Screen.height / 2f);
            Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint/*Input.mousePosition*/);
            Transform hitTransform = null; //////////////
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderLayerMask))
            {
                debugTransform.position = raycastHit.point;
                mouseWorldPosition = raycastHit.point;
                hitTransform = raycastHit.transform; //////////////

                // ���� ������������ ��������� � ��������, ������� ����� ������������
                if (raycastHit.collider.gameObject.CompareTag("Enemy"))
                {
                    enemyLookAt = true;

                    Transform head = raycastHit.transform.root.Find("Head Health");
                    Transform main = raycastHit.transform.root.Find("Main Health");

                    // ������ �����, �������� ���
                    if (head != null)
                    {
                        head.gameObject.SetActive(true);
                        heads.Add(head);
                    }

                    if (main != null)
                    {
                        main.gameObject.SetActive(true);
                        mains.Add(main);
                    }
                }
                else
                {
                    enemyLookAt = false;

                    StartCoroutine(EnemyHealthBarGone());
                }
            }

            // ����� ������� � �� ������
            if (playerInputActionsCode.aim && !myThirdPersonController.IsFalling())
            {
                // � ����� ������ �������� �������� ������������ �� �������
                myThirdPersonController.SetTargetSpeed(myThirdPersonController.GetMoveSpeed());

                //rigBuilder.enabled = true;

                // ������������� �������� ������������ � ����������� �� ���� ������
                if (playerGunSelector.GetGunType().Equals(GunType.Pistol))
                {
                    ////// ������ ������ ��������
                    //// ����
                    // ��������� �������� ������������ �����
                    animator.SetLayerWeight(7, Mathf.Lerp(animator.GetLayerWeight(7), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������ � ����� ��� ������������
                    animator.SetLayerWeight(8, Mathf.Lerp(animator.GetLayerWeight(8), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������ � �����
                    animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ����� ����
                    rigBatAttackTransform[0].SetActive(false);
                    rigBatAttackTransform[1].SetActive(false);
                    rigBatAttackTransform[2].SetActive(false);
                    rigBatAttackTransform[3].SetActive(false);
                    rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);
                    //// ��������
                    // ��������� ������� ������������ �� ���������
                    rigShotgunTransform[0].SetActive(false);
                    rigShotgunTransform[1].SetActive(false);
                    rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ���� � ����������
                    animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������ � ����������
                    rigShotgunMovementTransform[0].SetActive(false);
                    rigShotgunMovementTransform[1].SetActive(false);
                    rigShotgunMovementTransform[2].SetActive(false);
                    rigShotgunMovementTransform[3].SetActive(false);
                    rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������� � ����������
                    rigShotgunIdleTransform[0].SetActive(false);
                    rigShotgunIdleTransform[1].SetActive(false);
                    rigShotgunIdleTransform[2].SetActive(false);
                    rigShotgunIdleTransform[3].SetActive(false);
                    rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);
                    //// ��������
                    // ��������� �������� ���� � ����������
                    animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));

                    /////// �������� ��������
                    //// ��������
                    // ���� ����� ��������������
                    if (playerAction.IsReloading)
                    {
                        // ��������� �������� ������������ �� ���������
                        rigPistolTransform[0].SetActive(false);
                        rigPistolTransform[1].SetActive(false);
                        rigPistol.weight = Mathf.Lerp(rigPistol.weight, 0f, Time.deltaTime * 20f);
                        animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));

                        // �������� �������� ����������� ���������
                        animator.SetLayerWeight(9, 1f);
                    } // ���� ����� �� ��������������
                    else
                    {
                        // �������� �������� ������������ ��� ���������
                        rigPistolTransform[0].SetActive(true);
                        rigPistolTransform[1].SetActive(true);
                        rigPistol.weight = Mathf.Lerp(rigPistol.weight, 1f, Time.deltaTime * 20f);
                        animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 1f, Time.deltaTime * 10f));
                    }
                }
                else if (playerGunSelector.GetGunType().Equals(GunType.Shotgun))
                {
                    ////// ������ ������ ��������
                    //// ����
                    // ��������� �������� ������������ �����
                    animator.SetLayerWeight(7, Mathf.Lerp(animator.GetLayerWeight(7), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������ � ����� ��� ������������
                    animator.SetLayerWeight(8, Mathf.Lerp(animator.GetLayerWeight(8), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������ � �����
                    animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ����� ����
                    rigBatAttackTransform[0].SetActive(false);
                    rigBatAttackTransform[1].SetActive(false);
                    rigBatAttackTransform[2].SetActive(false);
                    rigBatAttackTransform[3].SetActive(false);
                    rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);
                    //// ��������
                    // ��������� �������� ������������ �� ���������
                    rigPistolTransform[0].SetActive(false);
                    rigPistolTransform[1].SetActive(false);
                    rigPistol.weight = Mathf.Lerp(rigPistol.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));
                    // ��������� ������� ���� � ����������
                    animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
                    //// ��������
                    // ��������� �������� ������ � ����������
                    rigShotgunMovementTransform[0].SetActive(false);
                    rigShotgunMovementTransform[1].SetActive(false);
                    rigShotgunMovementTransform[2].SetActive(false);
                    rigShotgunMovementTransform[3].SetActive(false);
                    rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������� � ����������
                    rigShotgunIdleTransform[0].SetActive(false);
                    rigShotgunIdleTransform[1].SetActive(false);
                    rigShotgunIdleTransform[2].SetActive(false);
                    rigShotgunIdleTransform[3].SetActive(false);
                    rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);
                    // ��������� �������� ���� � ����������
                    animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));

                    /////// �������� ��������
                    //// ��������
                    // ���� ����� ��������������
                    if (playerAction.IsReloading)
                    {
                        // ��������� �������� ������������ �� ���������
                        rigShotgunTransform[0].SetActive(false);
                        rigShotgunTransform[1].SetActive(false);
                        rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 0f, Time.deltaTime * 20f);
                        animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 0f, Time.deltaTime * 10f));

                        // �������� �������� ����������� ���������
                        animator.SetLayerWeight(10, 1f);
                    } // ���� ����� �� ��������������
                    else
                    {
                        // �������� �������� ������������ ��� ���������
                        rigShotgunTransform[0].SetActive(true);
                        rigShotgunTransform[1].SetActive(true);
                        rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 1f, Time.deltaTime * 20f);
                        animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 1f, Time.deltaTime * 10f));
                    }
                }
                else if (playerGunSelector.GetGunType().Equals(GunType.BaseballBat))
                {
                    ////// ������ ������ ��������
                    //// ����
                    // ��������� �������� ������ � �����
                    animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                    //// ��������
                    // ��������� �������� ������������ �� ���������
                    rigPistolTransform[0].SetActive(false);
                    rigPistolTransform[1].SetActive(false);
                    rigPistol.weight = Mathf.Lerp(rigPistol.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));
                    // ��������� ������� ���� � ����������
                    animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
                    //// ��������
                    // ��������� ������� ������������ �� ���������
                    rigShotgunTransform[0].SetActive(false);
                    rigShotgunTransform[1].SetActive(false);
                    rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������ � ����������
                    rigShotgunMovementTransform[0].SetActive(false);
                    rigShotgunMovementTransform[1].SetActive(false);
                    rigShotgunMovementTransform[2].SetActive(false);
                    rigShotgunMovementTransform[3].SetActive(false);
                    rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������� � ����������
                    rigShotgunIdleTransform[0].SetActive(false);
                    rigShotgunIdleTransform[1].SetActive(false);
                    rigShotgunIdleTransform[2].SetActive(false);
                    rigShotgunIdleTransform[3].SetActive(false);
                    rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);
                    // ��������� �������� ���� � ����������
                    animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));

                    ////// �������� ��������
                    //// ����
                    // �������� �������� ������������ �����
                    animator.SetLayerWeight(7, Mathf.Lerp(animator.GetLayerWeight(7), 1f, Time.deltaTime * 10f));
                    // �������� �������� ������ � ����� ��� ������������
                    animator.SetLayerWeight(8, Mathf.Lerp(animator.GetLayerWeight(8), 1f, Time.deltaTime * 10f));
                    // �������� �������� ����� ����
                    rigBatAttackTransform[0].SetActive(true);
                    rigBatAttackTransform[1].SetActive(true);
                    rigBatAttackTransform[2].SetActive(true);
                    rigBatAttackTransform[3].SetActive(true);
                    rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 1f, Time.deltaTime * 20f);
                }
                else if (playerGunSelector.GetGunType().Equals(GunType.None))
                {
                    ////// ������ ������ ��������
                    //// ����
                    // ��������� �������� ������������ �����
                    animator.SetLayerWeight(7, Mathf.Lerp(animator.GetLayerWeight(7), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������ � ����� ��� ������������
                    animator.SetLayerWeight(8, Mathf.Lerp(animator.GetLayerWeight(8), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������ � �����
                    animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ����� ����
                    rigBatAttackTransform[0].SetActive(false);
                    rigBatAttackTransform[1].SetActive(false);
                    rigBatAttackTransform[2].SetActive(false);
                    rigBatAttackTransform[3].SetActive(false);
                    rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);
                    //// ��������
                    // ��������� ������� ������������ �� ���������
                    rigShotgunTransform[0].SetActive(false);
                    rigShotgunTransform[1].SetActive(false);
                    rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ���� � ����������
                    animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������ � ����������
                    rigShotgunMovementTransform[0].SetActive(false);
                    rigShotgunMovementTransform[1].SetActive(false);
                    rigShotgunMovementTransform[2].SetActive(false);
                    rigShotgunMovementTransform[3].SetActive(false);
                    rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                    // ��������� �������� ������� � ����������
                    rigShotgunIdleTransform[0].SetActive(false);
                    rigShotgunIdleTransform[1].SetActive(false);
                    rigShotgunIdleTransform[2].SetActive(false);
                    rigShotgunIdleTransform[3].SetActive(false);
                    rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);
                    //// ��������
                    // ��������� �������� ������������ �� ���������
                    rigPistolTransform[0].SetActive(false);
                    rigPistolTransform[1].SetActive(false);
                    rigPistol.weight = Mathf.Lerp(rigPistol.weight, 0f, Time.deltaTime * 20f);
                    animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));
                    // ��������� ������� ���� � ����������
                    animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));

                    //// �������� ��������
                    // �������� �������� ������
                    animator.SetLayerWeight(0, Mathf.Lerp(animator.GetLayerWeight(0), 1f, Time.deltaTime * 10f));
                }

                if (playerInputActionsCode.aim)
                {
                    aimFreeLook.Priority = 10;
                    playerFollowCamera.Priority = 0;
                }

                //aimFreeLook.gameObject.SetActive(true);
                //playerFollowCamera.gameObject.SetActive(false);

                myThirdPersonController.SetSensivity(aimSensitivity);
                myThirdPersonController.SetAiming(true);

                Vector3 worldAimTarget = mouseWorldPosition;
                worldAimTarget.y = transform.position.y;
                Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

                transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);

                // ���������������� ������ ������������ � �������� �������
                /*CinemachineFreeLook pFC = playerFollowCamera.GetComponent<CinemachineFreeLook>();
                CinemachineFreeLook aFL = aimFreeLook.GetComponent<CinemachineFreeLook>();*/
                if (aimFreeLook != null && playerFollowCamera != null)
                {
                    playerFollowCamera.m_YAxis.Value = aimFreeLook.m_YAxis.Value;
                    playerFollowCamera.m_XAxis.Value = aimFreeLook.m_XAxis.Value;
                    //Debug.Log("Main Camera To Aim Camera");
                }
            }
            // �� ���������, ����� ������������
            else
            {
                // ���� ����� �� ������
                if (!myThirdPersonController.IsFalling())
                {
                    //rigBuilder.enabled = false;

                    // ���� ����� �����
                    if (playerInputActionsCode.sprint && (playerInputActionsCode.move.x != 0 || playerInputActionsCode.move.y != 0))
                    {
                        ////// ��������� ������ ��������
                        //// ����
                        // ��������� �������� ������������ �����
                        animator.SetLayerWeight(7, Mathf.Lerp(animator.GetLayerWeight(7), 0f, Time.deltaTime * 10f));
                        // ��������� �������� ������ � ����� ��� ������������
                        animator.SetLayerWeight(8, Mathf.Lerp(animator.GetLayerWeight(8), 0f, Time.deltaTime * 10f));
                        // ��������� �������� ������ � �����
                        animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                        // ��������� �������� ����� ����
                        rigBatAttackTransform[0].SetActive(false);
                        rigBatAttackTransform[1].SetActive(false);
                        rigBatAttackTransform[2].SetActive(false);
                        rigBatAttackTransform[3].SetActive(false);
                        rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);
                        //// ��������
                        // ��������� �������� ������������ ���������
                        rigShotgunTransform[0].SetActive(false);
                        rigShotgunTransform[1].SetActive(false);
                        rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 0f, Time.deltaTime * 20f);
                        animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 0f, Time.deltaTime * 10f));
                        // ��������� �������� ������ � ���������� (��������)
                        animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                        // ��������� �������� ������� � ����������
                        rigShotgunIdleTransform[0].SetActive(false);
                        rigShotgunIdleTransform[1].SetActive(false);
                        rigShotgunIdleTransform[2].SetActive(false);
                        rigShotgunIdleTransform[3].SetActive(false);
                        rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);
                        //// ��������
                        // ��������� �������� ������������ ���������
                        rigPistolTransform[0].SetActive(false);
                        rigPistolTransform[1].SetActive(false);
                        rigPistol.weight = Mathf.Lerp(rigPistol.weight, 0f, Time.deltaTime * 20f);
                        animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));

                        // �������� �������� � ����������� �� ���� ������
                        if (playerGunSelector.GetGunType().Equals(GunType.Shotgun))
                        {
                            ////// ��������� ��������
                            //// ��������
                            // ��������� �������� ���� � ����������
                            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));

                            /////// �������� ��������
                            //// ��������
                            // ���� ����� ��������������
                            if (playerAction.IsReloading)
                            {
                                ////// ��������� ��������
                                //// ��������
                                // ��������� �������� ������ � ����������
                                rigShotgunMovementTransform[0].SetActive(false);
                                rigShotgunMovementTransform[1].SetActive(false);
                                rigShotgunMovementTransform[2].SetActive(false);
                                rigShotgunMovementTransform[3].SetActive(false);
                                rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                                // ��������� �������� ���� � ����������
                                animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));

                                // �������� �������� ����������� ���������
                                animator.SetLayerWeight(10, 1f);
                            } // ���� ����� �� ��������������
                            else
                            {
                                ////// �������� ��������
                                //// ��������
                                // �������� �������� ������ � ����������
                                rigShotgunMovementTransform[0].SetActive(true);
                                rigShotgunMovementTransform[1].SetActive(true);
                                rigShotgunMovementTransform[2].SetActive(true);
                                rigShotgunMovementTransform[3].SetActive(true);
                                rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 1f, Time.deltaTime * 20f);
                                // �������� �������� ���� � ����������
                                animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 1f, Time.deltaTime * 10f));
                            }
                        }
                        else if (playerGunSelector.GetGunType().Equals(GunType.Pistol))
                        {
                            ////// ��������� ��������
                            //// ��������
                            // ��������� �������� ������ � ���������� (��������� ���)
                            rigShotgunMovementTransform[0].SetActive(false);
                            rigShotgunMovementTransform[1].SetActive(false);
                            rigShotgunMovementTransform[2].SetActive(false);
                            rigShotgunMovementTransform[3].SetActive(false);
                            rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                            // ��������� �������� ���� � ����������
                            animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));

                            /////// �������� ��������
                            //// ��������
                            // ���� ����� ��������������
                            if (playerAction.IsReloading)
                            {
                                ////// ��������� ��������
                                //// ��������
                                // ��������� �������� ���� � ����������
                                animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));

                                // �������� �������� ����������� ���������
                                animator.SetLayerWeight(9, 1f);
                            } // ���� ����� �� ��������������
                            else
                            {
                                ////// �������� ��������
                                //// ��������
                                // �������� �������� ���� � ����������
                                animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));
                            }
                        }
                        else if (playerGunSelector.GetGunType().Equals(GunType.BaseballBat))
                        {
                            ////// ��������� ��������
                            //// ��������
                            // ��������� �������� ������ � ����������
                            rigShotgunMovementTransform[0].SetActive(false);
                            rigShotgunMovementTransform[1].SetActive(false);
                            rigShotgunMovementTransform[2].SetActive(false);
                            rigShotgunMovementTransform[3].SetActive(false);
                            rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                            // ��������� �������� ���� � ����������
                            animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));

                            ////// �������� ��������
                            //// ��������
                            // �������� �������� ���� � ����������
                            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));
                        }
                        else if (playerGunSelector.GetGunType().Equals(GunType.None))
                        {
                            ////// ������ ������ ��������
                            //// ����
                            // ��������� �������� ������������ �����
                            animator.SetLayerWeight(7, Mathf.Lerp(animator.GetLayerWeight(7), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������ � ����� ��� ������������
                            animator.SetLayerWeight(8, Mathf.Lerp(animator.GetLayerWeight(8), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������ � �����
                            animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ����� ����
                            rigBatAttackTransform[0].SetActive(false);
                            rigBatAttackTransform[1].SetActive(false);
                            rigBatAttackTransform[2].SetActive(false);
                            rigBatAttackTransform[3].SetActive(false);
                            rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);
                            //// ��������
                            // ��������� ������� ������������ �� ���������
                            rigShotgunTransform[0].SetActive(false);
                            rigShotgunTransform[1].SetActive(false);
                            rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 0f, Time.deltaTime * 20f);
                            animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ���� � ����������
                            animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������ � ����������
                            rigShotgunMovementTransform[0].SetActive(false);
                            rigShotgunMovementTransform[1].SetActive(false);
                            rigShotgunMovementTransform[2].SetActive(false);
                            rigShotgunMovementTransform[3].SetActive(false);
                            rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                            animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������� � ����������
                            rigShotgunIdleTransform[0].SetActive(false);
                            rigShotgunIdleTransform[1].SetActive(false);
                            rigShotgunIdleTransform[2].SetActive(false);
                            rigShotgunIdleTransform[3].SetActive(false);
                            rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);
                            //// ��������
                            // ��������� �������� ������������ �� ���������
                            rigPistolTransform[0].SetActive(false);
                            rigPistolTransform[1].SetActive(false);
                            rigPistol.weight = Mathf.Lerp(rigPistol.weight, 0f, Time.deltaTime * 20f);
                            animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));

                            //// �������� ��������
                            // �������� �������� ����
                            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));
                        }

                        // ���������� ��� ����������� ��������� "������"
                        myThirdPersonController.SetSprint(true);
                    }
                    // ���� ����� ����� ��� �����
                    else
                    {
                        // ���������� ��� ����������� ��������� "�� ������"
                        myThirdPersonController.SetSprint(false);

                        ////// ��������� ��������
                        //// ����
                        // ��������� �������� ������������ �����
                        animator.SetLayerWeight(7, Mathf.Lerp(animator.GetLayerWeight(7), 0f, Time.deltaTime * 10f));
                        // ��������� �������� ������ � ����� ��� ������������
                        animator.SetLayerWeight(8, Mathf.Lerp(animator.GetLayerWeight(8), 0f, Time.deltaTime * 10f));
                        //// ��������
                        // ��������� �������� ���� � ����������
                        animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
                        // ��������� �������� ������������ ���������
                        rigPistolTransform[0].SetActive(false);
                        rigPistolTransform[1].SetActive(false);
                        rigPistol.weight = Mathf.Lerp(rigPistol.weight, 0f, Time.deltaTime * 20f);
                        animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));
                        //// ��������
                        // ��������� �������� ���� � ����������
                        animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));
                        // ��������� �������� ������������ ���������
                        rigShotgunTransform[0].SetActive(false);
                        rigShotgunTransform[1].SetActive(false);
                        rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 0f, Time.deltaTime * 20f);
                        animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 0f, Time.deltaTime * 10f));

                        // ��������� � ������������� �������� � ����������� �� ���� ������
                        if (playerGunSelector.GetGunType().Equals(GunType.Pistol))
                        {
                            ////// ��������� ��������
                            //// ����
                            // ��������� �������� ������ � �����
                            animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ����� ����
                            rigBatAttackTransform[0].SetActive(false);
                            rigBatAttackTransform[1].SetActive(false);
                            rigBatAttackTransform[2].SetActive(false);
                            rigBatAttackTransform[3].SetActive(false);
                            rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);
                            //// ��������
                            // ��������� �������� ������ � ����������
                            rigShotgunMovementTransform[0].SetActive(false);
                            rigShotgunMovementTransform[1].SetActive(false);
                            rigShotgunMovementTransform[2].SetActive(false);
                            rigShotgunMovementTransform[3].SetActive(false);
                            rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                            animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������� � ����������
                            rigShotgunIdleTransform[0].SetActive(false);
                            rigShotgunIdleTransform[1].SetActive(false);
                            rigShotgunIdleTransform[2].SetActive(false);
                            rigShotgunIdleTransform[3].SetActive(false);
                            rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);

                            /////// �������� ��������
                            //// ��������
                            // ���� ����� ��������������
                            if (playerAction.IsReloading)
                            {
                                // �������� �������� ����������� ���������
                                animator.SetLayerWeight(9, 1f);
                            }

                            // �������� �������� ������ � ����������
                            //animator.SetLayerWeight(0, Mathf.Lerp(animator.GetLayerWeight(0), 1f, Time.deltaTime * 10f));
                        }
                        else if (playerGunSelector.GetGunType().Equals(GunType.Shotgun))
                        {
                            ////// ��������� ��������
                            //// ����
                            // ��������� �������� ������ � �����
                            animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ����� ����
                            rigBatAttackTransform[0].SetActive(false);
                            rigBatAttackTransform[1].SetActive(false);
                            rigBatAttackTransform[2].SetActive(false);
                            rigBatAttackTransform[3].SetActive(false);
                            rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);

                            ////// �������� ��������
                            //// ��������
                            // �������� �������� ������
                            //animator.SetLayerWeight(0, Mathf.Lerp(animator.GetLayerWeight(0), 1f, Time.deltaTime * 10f));
                            //// ��������
                            // �������� �������� ������ � ���������� (��������)
                            animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 1f, Time.deltaTime * 10f));

                            // ���� ����� ����� � ����������
                            if ((playerInputActionsCode.move.x != 0 || playerInputActionsCode.move.y != 0) && !playerInputActionsCode.sprint)
                            {
                                ////// �������� ��������
                                // ��������
                                // ��������� �������� ������� � ���������� (��������� ���)
                                rigShotgunIdleTransform[0].SetActive(false);
                                rigShotgunIdleTransform[1].SetActive(false);
                                rigShotgunIdleTransform[2].SetActive(false);
                                rigShotgunIdleTransform[3].SetActive(false);
                                rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);

                                /////// �������� ��������
                                //// ��������
                                // ���� ����� ��������������
                                if (playerAction.IsReloading)
                                {
                                    ////// ��������� ��������
                                    // ��������
                                    // ������������ �������� ������ � ���������� (��������� ���)
                                    rigShotgunMovementTransform[0].SetActive(false);
                                    rigShotgunMovementTransform[1].SetActive(false);
                                    rigShotgunMovementTransform[2].SetActive(false);
                                    rigShotgunMovementTransform[3].SetActive(false);
                                    rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);

                                    // �������� �������� ����������� ���������
                                    animator.SetLayerWeight(10, 1f);
                                } // ���� ����� �� ��������������
                                else
                                {
                                    ////// �������� ��������
                                    // ��������
                                    // ������������ �������� ������ � ���������� (��������� ���)
                                    rigShotgunMovementTransform[0].SetActive(true);
                                    rigShotgunMovementTransform[1].SetActive(true);
                                    rigShotgunMovementTransform[2].SetActive(true);
                                    rigShotgunMovementTransform[3].SetActive(true);
                                    rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 1f, Time.deltaTime * 20f);
                                }
                            }
                            // ���� ����� �� ��������� � ����������
                            else if ((playerInputActionsCode.move.x == 0 || playerInputActionsCode.move.y == 0) && !playerInputActionsCode.sprint)
                            {
                                ////// �������� ��������
                                // ��������
                                // ��������� �������� ������ � ���������� (��������� ���)
                                rigShotgunMovementTransform[0].SetActive(false);
                                rigShotgunMovementTransform[1].SetActive(false);
                                rigShotgunMovementTransform[2].SetActive(false);
                                rigShotgunMovementTransform[3].SetActive(false);
                                rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);

                                /////// �������� ��������
                                //// ��������
                                // ���� ����� ��������������
                                if (playerAction.IsReloading)
                                {
                                    ////// ��������� ��������
                                    // ��������
                                    // ��������� �������� ������� � ���������� (��������� ���)
                                    rigShotgunIdleTransform[0].SetActive(false);
                                    rigShotgunIdleTransform[1].SetActive(false);
                                    rigShotgunIdleTransform[2].SetActive(false);
                                    rigShotgunIdleTransform[3].SetActive(false);
                                    rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);

                                    // �������� �������� ����������� ���������
                                    animator.SetLayerWeight(10, 1f);
                                } // ���� ����� �� ��������������
                                else
                                {
                                    ////// �������� ��������
                                    // ��������
                                    // �������� �������� ������� � ���������� (��������� ���)
                                    rigShotgunIdleTransform[0].SetActive(true);
                                    rigShotgunIdleTransform[1].SetActive(true);
                                    rigShotgunIdleTransform[2].SetActive(true);
                                    rigShotgunIdleTransform[3].SetActive(true);
                                    rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 1f, Time.deltaTime * 20f);
                                }
                            }
                        }
                        else if (playerGunSelector.GetGunType().Equals(GunType.BaseballBat))
                        {
                            ////// �������� ��������
                            //// ����
                            // �������� �������� ������
                            animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 1f, Time.deltaTime * 10f));

                            // ���� ����� ����� � �����
                            if ((playerInputActionsCode.move.x != 0 || playerInputActionsCode.move.y != 0) && !playerInputActionsCode.sprint)
                            {
                                // ��������� �������� ����� ����
                                rigBatAttackTransform[0].SetActive(false);
                                rigBatAttackTransform[1].SetActive(false);
                                rigBatAttackTransform[2].SetActive(false);
                                rigBatAttackTransform[3].SetActive(false);
                                rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);
                            }
                            // ���� ����� �� ��������� � �����
                            else if ((playerInputActionsCode.move.x == 0 || playerInputActionsCode.move.y == 0) && !playerInputActionsCode.sprint)
                            {
                                // �������� �������� ����� ����
                                rigBatAttackTransform[0].SetActive(true);
                                rigBatAttackTransform[1].SetActive(true);
                                rigBatAttackTransform[2].SetActive(true);
                                rigBatAttackTransform[3].SetActive(true);
                                rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 1f, Time.deltaTime * 20f);
                            }

                            ////// ��������� ��������
                            //// ��������
                            // ��������� �������� ������ � ����������
                            rigShotgunMovementTransform[0].SetActive(false);
                            rigShotgunMovementTransform[1].SetActive(false);
                            rigShotgunMovementTransform[2].SetActive(false);
                            rigShotgunMovementTransform[3].SetActive(false);
                            rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                            animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������� � ����������
                            rigShotgunIdleTransform[0].SetActive(false);
                            rigShotgunIdleTransform[1].SetActive(false);
                            rigShotgunIdleTransform[2].SetActive(false);
                            rigShotgunIdleTransform[3].SetActive(false);
                            rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);
                        }
                        else if (playerGunSelector.GetGunType().Equals(GunType.None))
                        {
                            ////// ������ ������ ��������
                            //// ����
                            // ��������� �������� ������������ �����
                            animator.SetLayerWeight(7, Mathf.Lerp(animator.GetLayerWeight(7), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������ � ����� ��� ������������
                            animator.SetLayerWeight(8, Mathf.Lerp(animator.GetLayerWeight(8), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������ � �����
                            animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ����� ����
                            rigBatAttackTransform[0].SetActive(false);
                            rigBatAttackTransform[1].SetActive(false);
                            rigBatAttackTransform[2].SetActive(false);
                            rigBatAttackTransform[3].SetActive(false);
                            rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);
                            //// ��������
                            // ��������� ������� ������������ �� ���������
                            rigShotgunTransform[0].SetActive(false);
                            rigShotgunTransform[1].SetActive(false);
                            rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 0f, Time.deltaTime * 20f);
                            animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ���� � ����������
                            animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������ � ����������
                            rigShotgunMovementTransform[0].SetActive(false);
                            rigShotgunMovementTransform[1].SetActive(false);
                            rigShotgunMovementTransform[2].SetActive(false);
                            rigShotgunMovementTransform[3].SetActive(false);
                            rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                            animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                            // ��������� �������� ������� � ����������
                            rigShotgunIdleTransform[0].SetActive(false);
                            rigShotgunIdleTransform[1].SetActive(false);
                            rigShotgunIdleTransform[2].SetActive(false);
                            rigShotgunIdleTransform[3].SetActive(false);
                            rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);
                            //// ��������
                            // ��������� �������� ������������ �� ���������
                            rigPistolTransform[0].SetActive(false);
                            rigPistolTransform[1].SetActive(false);
                            rigPistol.weight = Mathf.Lerp(rigPistol.weight, 0f, Time.deltaTime * 20f);
                            animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));
                            // ��������� ������� ���� � ����������
                            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));

                            //// �������� ��������
                            // �������� �������� ������
                            animator.SetLayerWeight(0, Mathf.Lerp(animator.GetLayerWeight(0), 1f, Time.deltaTime * 10f));
                        }
                    }
                    /*else
                    {
                        if (playerGunSelector.GetGunType().Equals(GunType.Shotgun))
                        {
                            animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));
                        }
                        else
                        {
                            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
                        }
                        myThirdPersonController.SetSprint(false);
                    }*/


                    if (!playerInputActionsCode.aim)
                    {
                        aimFreeLook.Priority = 0;
                        playerFollowCamera.Priority = 10;
                    }
                    //aimFreeLook.gameObject.SetActive(false);
                    //playerFollowCamera.gameObject.SetActive(true);

                    myThirdPersonController.SetSensivity(normalSensitivity);
                    myThirdPersonController.SetAiming(false);

                    /*CinemachineFreeLook pFC = playerFollowCamera.GetComponent<CinemachineFreeLook>();
                    CinemachineFreeLook aFL = aimFreeLook.GetComponent<CinemachineFreeLook>();*/
                    if (playerFollowCamera != null && aimFreeLook != null)
                    {
                        aimFreeLook.m_YAxis.Value = playerFollowCamera.m_YAxis.Value;
                        aimFreeLook.m_XAxis.Value = playerFollowCamera.m_XAxis.Value;
                        //Debug.Log("Aim Camera To Main Camera");
                    }
                }
            }

            // ���� ����� ������
            if (myThirdPersonController.IsFalling())
            {
                ////// ������ ������ ��������
                //// ����
                // ��������� �������� ������������ �����
                animator.SetLayerWeight(7, Mathf.Lerp(animator.GetLayerWeight(7), 0f, Time.deltaTime * 10f));
                // ��������� �������� ������ � ����� ��� ������������
                animator.SetLayerWeight(8, Mathf.Lerp(animator.GetLayerWeight(8), 0f, Time.deltaTime * 10f));
                // ��������� �������� ������ � �����
                animator.SetLayerWeight(6, Mathf.Lerp(animator.GetLayerWeight(6), 0f, Time.deltaTime * 10f));
                // ��������� �������� ����� ����
                rigBatAttackTransform[0].SetActive(false);
                rigBatAttackTransform[1].SetActive(false);
                rigBatAttackTransform[2].SetActive(false);
                rigBatAttackTransform[3].SetActive(false);
                rigBatAttack.weight = Mathf.Lerp(rigBatAttack.weight, 0f, Time.deltaTime * 20f);
                //// ��������
                // ��������� ������� ������������ �� ���������
                rigShotgunTransform[0].SetActive(false);
                rigShotgunTransform[1].SetActive(false);
                rigShotgun.weight = Mathf.Lerp(rigShotgun.weight, 0f, Time.deltaTime * 20f);
                animator.SetLayerWeight(3, Mathf.Lerp(animator.GetLayerWeight(3), 0f, Time.deltaTime * 10f));
                // ��������� �������� ���� � ����������
                animator.SetLayerWeight(5, Mathf.Lerp(animator.GetLayerWeight(5), 0f, Time.deltaTime * 10f));
                // ��������� �������� ������ � ����������
                rigShotgunMovementTransform[0].SetActive(false);
                rigShotgunMovementTransform[1].SetActive(false);
                rigShotgunMovementTransform[2].SetActive(false);
                rigShotgunMovementTransform[3].SetActive(false);
                rigShotgunMovement.weight = Mathf.Lerp(rigShotgunMovement.weight, 0f, Time.deltaTime * 20f);
                animator.SetLayerWeight(4, Mathf.Lerp(animator.GetLayerWeight(4), 0f, Time.deltaTime * 10f));
                // ��������� �������� ������� � ����������
                rigShotgunIdleTransform[0].SetActive(false);
                rigShotgunIdleTransform[1].SetActive(false);
                rigShotgunIdleTransform[2].SetActive(false);
                rigShotgunIdleTransform[3].SetActive(false);
                rigShotgunIdle.weight = Mathf.Lerp(rigShotgunIdle.weight, 0f, Time.deltaTime * 20f);
                //// ��������
                // ��������� �������� ������������ �� ���������
                rigPistolTransform[0].SetActive(false);
                rigPistolTransform[1].SetActive(false);
                rigPistol.weight = Mathf.Lerp(rigPistol.weight, 0f, Time.deltaTime * 20f);
                animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));
                // ��������� ������� ���� � ����������
                animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
            }
        } // ���� ����� ����
        else
        {
            StartCoroutine(EnemyHealthBarGone());

            playerInputActionsCode.aim = false;
            playerInputActionsCode.shoot = false;
            playerInputActionsCode.move = new Vector2(0f, 0f);

            ////// ������ ������ ��������
            //// ����
            // ��������� �������� ������������ �����
            animator.SetLayerWeight(7, 0f);
            // ��������� �������� ������ � ����� ��� ������������
            animator.SetLayerWeight(8, 0f);
            // ��������� �������� ������ � �����
            animator.SetLayerWeight(6, 0f);
            // ��������� �������� ����� ����
            rigBatAttackTransform[0].SetActive(false);
            rigBatAttackTransform[1].SetActive(false);
            rigBatAttackTransform[2].SetActive(false);
            rigBatAttackTransform[3].SetActive(false);
            rigBatAttack.weight = 0f;
            //// ��������
            // ��������� ������� ������������ �� ���������
            rigShotgunTransform[0].SetActive(false);
            rigShotgunTransform[1].SetActive(false);
            rigShotgun.weight = 0f;
            animator.SetLayerWeight(3, 0f);
            // ��������� �������� ���� � ����������
            animator.SetLayerWeight(5, 0f);
            // ��������� �������� ������ � ����������
            rigShotgunMovementTransform[0].SetActive(false);
            rigShotgunMovementTransform[1].SetActive(false);
            rigShotgunMovementTransform[2].SetActive(false);
            rigShotgunMovementTransform[3].SetActive(false);
            rigShotgunMovement.weight = 0f;
            animator.SetLayerWeight(4, 0f);
            // ��������� �������� ������� � ����������
            rigShotgunIdleTransform[0].SetActive(false);
            rigShotgunIdleTransform[1].SetActive(false);
            rigShotgunIdleTransform[2].SetActive(false);
            rigShotgunIdleTransform[3].SetActive(false);
            rigShotgunIdle.weight = 0f;
            //// ��������
            // ��������� �������� ������������ �� ���������
            rigPistolTransform[0].SetActive(false);
            rigPistolTransform[1].SetActive(false);
            rigPistol.weight = 0f;
            animator.SetLayerWeight(2, 0f);
            // ��������� ������� ���� � ����������
            animator.SetLayerWeight(1, 0f);

            aimFreeLook.m_XAxis.m_MaxSpeed = 0;
            aimFreeLook.m_YAxis.m_MaxSpeed = 0;

            playerFollowCamera.m_XAxis.m_MaxSpeed = 0;
            playerFollowCamera.m_YAxis.m_MaxSpeed = 0;
        }
    }

    private void EndReloading()
    {
        // ��������� �������� �����������
        if (playerGunSelector.GetGunType().Equals(GunType.Pistol))
        {
            animator.SetLayerWeight(9, 0f);
        }
        else if (playerGunSelector.GetGunType().Equals(GunType.Shotgun))
        {
            animator.SetLayerWeight(10, 0f);
        }

        playerAction.EndReload();
    }

    public Vector3 GetMouseWorldPosition()
    {
        return mouseWorldPosition;
    }

    /*private void SpawnVFX()
    {
        GameObject vfx;

        if (firePoint != null)
        {
            vfx = Instantiate(effectToSpawn, firePoint.transform.position, Quaternion.identity);
        }
    }*/
}
