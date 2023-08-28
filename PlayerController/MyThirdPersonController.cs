using Unity.VisualScripting;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace MyStarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class MyThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Tooltip("Sensitivity")]
        public float Sensitivity = 1f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Header("Player Attack")]
        [Tooltip("If the character is attack")]
        public bool Attack = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Tooltip("Rotate character")]
        public float turnSpeed = 15f;

        float timePassed; // Прошедшее время с момента последней атаки
        float clipLength; // Длина времени анимации атаки
        float clipSpeed; // Скорость анимации

        public float cooldownTime = 2f;
        public static int noOffClicks = 0;
        float lastClickTime = 0f;
        float maxComboDelay = 1;
        /*private float nextFireTime = 0f;*/

        private float targetSpeed = 1f;

        private bool aiming = false;
        private bool sprint = false;
        private bool shooting = false;
        private bool move = false;
        private bool attackNearWeapon = false;
        private bool isShootWeapon = false;

        private PlayerHealth playerHealth;
        [SerializeField]
        private int countAudioSteps = 0;
        [SerializeField]
        private int countAudioFallEnd = 0;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDInputX;
        private int _animIDInputY;
        private int _animIDAttack1;
        private int _animIDAttack2;
        //private int _animIDAttack3;
        private int _animIDAttackNone;
        private int _animIDMove;
        private int _animIDAim;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private PlayerInputActionsCode _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();    

            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            //_cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputActionsCode>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            attackNearWeapon = false;
            timePassed = 0f;
        }

        private void Update()
        {
            // Пока игрок жив
            if(!playerHealth.isDead && !PauseController.instance.isPause)
            {
                _hasAnimator = TryGetComponent(out _animator);
                /*if (_hasAnimator)
                {
                    if (_animator.GetFloat(_animIDSpeed) == 0f)
                    {
                        _animator.SetFloat("InputX", 0f, 0.1f, Time.deltaTime);
                        _animator.SetFloat("InputY", 0f, 0.1f, Time.deltaTime);
                    }
                }*/

                if (aiming)
                    _animator.SetBool(_animIDAim, true);
                else
                    _animator.SetBool(_animIDAim, false);


                if (aiming && !isShootWeapon)
                {
                    timePassed += Time.deltaTime;
                    clipLength = _animator.GetCurrentAnimatorClipInfo(7)[0].clip.length;
                    clipSpeed = _animator.GetCurrentAnimatorStateInfo(7).speed;

                    if (shooting && attackNearWeapon)
                    {
                        _animator.ResetTrigger(_animIDAttackNone);
                        _animator.SetTrigger(_animIDAttack1);
                    }

                    if (timePassed >= clipLength / clipSpeed &&
                        _animator.GetCurrentAnimatorStateInfo(7).IsName("Hit1") &&
                        attackNearWeapon)
                    {
                        attackNearWeapon = false;
                        _animator.ResetTrigger(_animIDAttack1);
                        _animator.SetTrigger(_animIDAttackNone);
                    }
                    /*timePassed += Time.deltaTime;
                    clipLength = _animator.GetCurrentAnimatorClipInfo(7)[0].clip.length;
                    clipSpeed = _animator.GetCurrentAnimatorStateInfo(7).speed;

                    // Зафиксировать количество ударов
                    if (shooting)
                    {
                        shooting = false;
                        _animator.ResetTrigger(_animIDAttackNone);
                        lastClickTime = Time.time;
                        noOffClicks++;
                        if (noOffClicks >= 4)
                            noOffClicks = 3;
                    }

                    // Если есть первое нажатие кнопки удара при прицеливании
                    if (noOffClicks == 1 && attackNearWeapon)
                    {
                        _animator.SetTrigger(_animIDAttack1);
                        _animator.ResetTrigger(_animIDAttack2);
                        //_animator.ResetTrigger(_animIDAttack3);
                    }
                    // Если закончилась 1 анимация удара и есть ещё клики мышью - воспроизвести 2 анимацию удара
                    if (noOffClicks >= 2 && timePassed >= clipLength / clipSpeed && 
                            _animator.GetCurrentAnimatorStateInfo(7).IsName("Hit1") &&
                            attackNearWeapon)
                    {
                        _animator.ResetTrigger(_animIDAttack1);
                        _animator.SetTrigger(_animIDAttack2);
                        //_animator.ResetTrigger(_animIDAttack3);
                    }
                    // Если закончилась 2 анимация удара и есть ещё клики мышью - воспроизвести 1 анимацию удара
                    if (noOffClicks >= 3 && timePassed >= clipLength / clipSpeed &&
                            _animator.GetCurrentAnimatorStateInfo(7).IsName("Hit2") &&
                            attackNearWeapon)
                    {
                        //_animator.SetTrigger(_animIDAttackNone);
                        _animator.SetTrigger(_animIDAttack1);
                        _animator.ResetTrigger(_animIDAttack2);
                        //_animator.ResetTrigger(_animIDAttackNone);
                        noOffClicks = 1;
                        //_animator.ResetTrigger(_animIDAttack2);
                    }
                        // Если закончилась 3 анимация удара и есть ещё клики мышью - воспроизвести 1 анимацию удара
                        *//*else if (timePassed >= clipLength / clipSpeed &&
                            _animator.GetCurrentAnimatorStateInfo(7).IsName("Hit3") &&
                            attackNearWeapon)
                        {
                            _animator.ResetTrigger(_animIDAttack1);
                            _animator.ResetTrigger(_animIDAttack3);
                            _animator.ResetTrigger(_animIDAttack2);
                            noOffClicks = 0;
                        }*//*

                        // Если закончилась анимация и кликов болше нет
                        if (Time.time - lastClickTime > clipLength / clipSpeed)
                    {
                        _animator.ResetTrigger(_animIDAttack1);
                        _animator.ResetTrigger(_animIDAttack2);
                        //_animator.ResetTrigger(_animIDAttack3);
                        _animator.SetTrigger(_animIDAttackNone);
                        attackNearWeapon = false;
                        noOffClicks = 0;
                    }*/
                }
                else if (!aiming && !isShootWeapon)
                {
                    attackNearWeapon = false;
                    timePassed = 0f;
                    lastClickTime = 0f;
                    noOffClicks = 0;
                    _animator.ResetTrigger(_animIDAttack1);
                    _animator.SetTrigger(_animIDAttackNone);
                }

                JumpAndGravity();
                GroundedCheck();
                Move();
            }
        }

        /*private void OnClick()
        {
            lastClickTime = Time.time;
            noOffClicks++;
            Debug.Log("Click: " + noOffClicks);

            if (noOffClicks == 1)
            {
                _animator.SetBool("Hit2", false);
                _animator.SetBool("Hit3", false);
                _animator.SetBool("Hit1", true);
            }
            noOffClicks = Mathf.Clamp(noOffClicks, 0, 3);

            if (noOffClicks >= 2 &&
                _animator.GetCurrentAnimatorStateInfo(7).normalizedTime > 0.7f &&
                _animator.GetCurrentAnimatorStateInfo(7).IsName("Hit1"))
            {
                _animator.SetBool("Hit3", false);
                _animator.SetBool("Hit1", false);
                _animator.SetBool("Hit2", true);
            }

            if (noOffClicks >= 3 &&
                _animator.GetCurrentAnimatorStateInfo(7).normalizedTime > 0.7f &&
                _animator.GetCurrentAnimatorStateInfo(7).IsName("Hit2"))
            {
                _animator.SetBool("Hit2", false);
                _animator.SetBool("Hit1", false);
                _animator.SetBool("Hit3", true);

                attackNearWeapon = false;
            }
        }*/

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

            _animIDInputX = Animator.StringToHash("InputX");
            _animIDInputY = Animator.StringToHash("InputY");

            _animIDAttack1 = Animator.StringToHash("Attack1");
            _animIDAttack2 = Animator.StringToHash("Attack2");
            //_animIDAttack3 = Animator.StringToHash("Attack3");
            _animIDAttackNone = Animator.StringToHash("AttackNone");
            _animIDMove = Animator.StringToHash("Move");
            _animIDAim = Animator.StringToHash("Aim");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void OnDrawGizmos()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);

            Gizmos.DrawSphere(spherePosition, GroundedRadius);
        }

        private void CameraRotation()
        {
            /*// if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier * Sensitivity;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier * Sensitivity;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);*/

            if (!aiming)
            {
                float yawCamera = _mainCamera.transform.rotation.eulerAngles.y;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yawCamera, 0), turnSpeed * Time.fixedDeltaTime);
            }
        }

        private void Move()
        {
            if(aiming && sprint)
            {
                _animator.SetLayerWeight(1, Mathf.Lerp(_animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
                sprint = false;
            }

            // set target speed based on move speed, sprint speed and if sprint is pressed
            targetSpeed = _input.sprint && !aiming ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                /*float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);*/
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator) { 
                Vector2 input;

                input.x = Input.GetAxis("Horizontal");
                input.y = Input.GetAxis("Vertical");

                _animator.SetFloat("InputX", input.x);
                _animator.SetFloat("InputY", input.y);
                if (input.x != 0f || input.y != 0f)
                    _animator.SetTrigger(_animIDMove);
                else
                    _animator.ResetTrigger(_animIDMove);
               
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    //_animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                /*if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }*/

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                //_input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        public void PlayAnimAttack()
        {
            if (_hasAnimator)
            {
                Debug.Log("Animation attack");
                _animator.SetBool(_animIDAttack1, true);
            }
        }

        public void StopAnimAttack()
        {
            if (_hasAnimator)
            {
                Debug.Log("Animation stop attack");
                _animator.SetBool(_animIDAttack1, false);
            }
        }

        public void SetSensivity(float newSensivity)
        {
            Sensitivity = newSensivity;
        }

        public void SetAiming(bool isAim)
        {
            aiming = isAim;
        }

        public void SetSprint(bool isSprint)
        {
            sprint = isSprint;
        }

        public void SetShooting(bool isShooting)
        {
            shooting = isShooting;
        }

        public void SetShootWeapon(bool isShootWeapon)
        {
            this.isShootWeapon = isShootWeapon;
        }


        public void SetAttackNearWeapon(bool isAttackNearWeapon)
        {
            attackNearWeapon = isAttackNearWeapon;
        }

        public void SetMove(bool isMove)
        {
            move = isMove;
        }

        public Animator GetAnimator()
        {
            return _animator;
        }

        public bool IsFalling()
        {
            if (_animator != null && _animator.GetBool(_animIDFreeFall))
                return true;
            else
                return false;
        }

        public float GetMoveSpeed()
        {
            return MoveSpeed;
        }

        public float GetSprintSpeed()
        {
            return SprintSpeed;
        }

        public void SetTargetSpeed(float newSpeed)
        {
            targetSpeed = newSpeed;
        }

        private void PlayAudioSteps()
        {
            if(_input.sprint)
            {
                AudioManager.instance.Play("Run" + UnityEngine.Random.Range(0, countAudioSteps));
            } 
            else
            {
                AudioManager.instance.Play("Step" + UnityEngine.Random.Range(0, countAudioSteps));
            }
        }

        private void PlayAudioFallEnd()
        {
            AudioManager.instance.Play("FallEnd" + UnityEngine.Random.Range(0, countAudioFallEnd));
        }
    }
}