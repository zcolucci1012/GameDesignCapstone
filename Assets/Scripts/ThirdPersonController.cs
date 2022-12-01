﻿using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
using UnityEngine.UI;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
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

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        public AudioClip Punching;
        public AudioClip Shooting;
        public AudioClip Kicking;
        public AudioClip Swiping;
        public AudioClip GotHit;
        public AudioClip GotHitHard;
        bool soundPlayed = false;

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

        //timeout for attacks
        private float _basicAttackTimeout = 0.2f;
        private float _basicAttackTimer = 0.0f;
        private bool _basicAttackEnabled = false;

        private float _specialAttackRTimeout = 0.5f;
        private float _specialAttackRTimer = 0.0f;
        private bool _specialAttackREnabled = false;

        private float _specialAttackFTimeout = 2f;
        private float _specialAttackFTimer = 0.0f;
        private bool _specialAttackFEnabled = false;


        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDBasicAttack;
        private int _animIDSpecialAttackR;
        private int _animIDSpecialAttackF;
        private int _animIDBasicAttackFire;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool isPresent = true;
        private float transportTimer = 0;
        private float transportTime = 15;
        private bool almostReady = true;
        private bool invincible = false;

        public Cinemachine.CinemachineVirtualCamera _cinemachineCamera;
        public Camera otherTimeCamera;
        public GameObject otherTimeOverlay;

        public GameObject cursorImage;

        private Vector3 aimLocation;
        public GameObject bullet;
        private bool canFire = true;
        public GameObject gun;

        public GameObject winText;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        private bool canMove = true;

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            otherTimeOverlay.SetActive(false);
            transportTime = Random.Range(15f, 30f);
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
            BasicAttack();
            SpecialAttackR();
            SpecialAttackF();
            Invincibility();
            CursorMovement();
            Aim();
            CheckWin();
        }

        private void LateUpdate()
        {
            CameraRotation();
            Transport();
            OtherCameraUpdate();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDBasicAttack = Animator.StringToHash("BasicAttack");
            _animIDSpecialAttackR = Animator.StringToHash("SpecialR");
            _animIDSpecialAttackF = Animator.StringToHash("SpecialF");
            _animIDBasicAttackFire = Animator.StringToHash("BasicAttackFire");
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

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Invincibility()
        {
            //flash, hopefully
        }

        private void CheckWin()
        {
            if(GameObject.FindGameObjectsWithTag("Robot").Length == 0 &&
                GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
            {
                winText.SetActive(true);
            }
        }

        private void CursorMovement()
        {
            Cursor.visible = false;
            this.cursorImage.transform.position = Mouse.current.position.ReadValue();
        }

        private void Aim()
        {
            Vector2 mouse = Mouse.current.position.ReadValue();

            Ray ray = Camera.main.ScreenPointToRay(mouse);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, Mathf.Infinity))
            {
                aimLocation = raycastHit.point;
                aimLocation.y = 1f;
            }
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

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
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                            new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
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
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    //_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    //if (_hasAnimator)
                    //{
                    //    _animator.SetBool(_animIDJump, true);
                    //}
                }

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
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void Fire()
        {
            GameObject newBullet = Instantiate(bullet, this.transform.position + new Vector3(0, 1.5f, 0), this.transform.rotation);
            Vector3 directionToCursor = (aimLocation - (this.transform.position + new Vector3(0, 1.5f, 0))).normalized;
            this.transform.LookAt(aimLocation);
            newBullet.GetComponent<Rigidbody>().AddForce(directionToCursor * 50, ForceMode.Impulse);
        }

        private void BasicAttack()
        {
            _animator.SetBool(isPresent ? _animIDBasicAttackFire : _animIDBasicAttack, false);
            _basicAttackTimeout = isPresent ? 0.2f : 0.6f;
            if(_input.basicAttack)
            {
                if (isPresent)
                {
                    _animator.SetBool(_animIDBasicAttack, true);
                    if (soundPlayed == false) {
                    AudioSource.PlayClipAtPoint(Punching, transform.TransformPoint(_controller.center));
                    soundPlayed = true;
                    }
                } else
                {
                    if (canFire)
                    {
                        _animator.SetBool(_animIDBasicAttackFire, true);
                        if (soundPlayed == false) {
                           AudioSource.PlayClipAtPoint(Shooting, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                           soundPlayed = true;
                        }
                        Invoke("Fire", 0.1f);
                        _input.basicAttack = false;
                        canFire = false;
                    }
                }
                _basicAttackEnabled = true;
                _basicAttackTimer += Time.deltaTime;
    
            }

            if(_basicAttackEnabled)
            {
                _basicAttackTimer += Time.deltaTime;
            }

            if(_basicAttackTimer >= _basicAttackTimeout)
            {
                if (isPresent)
                {
                    _animator.SetBool(_animIDBasicAttack, false);
                } else
                {
                    _animator.SetBool(_animIDBasicAttackFire, false);
                    canFire = true;
                }
                _basicAttackEnabled = false;    
                _basicAttackTimer = 0.0f;
                _input.basicAttack = false;
                soundPlayed = false;
            }
            
        }

        private void SpecialAttackR()
        {
            if (_input.specialR)
            {
                _animator.SetBool(_animIDSpecialAttackR, true);
                _specialAttackREnabled = true;
                if (soundPlayed == false) {
                    AudioSource.PlayClipAtPoint(Swiping, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                    soundPlayed = true;
                }
                _specialAttackRTimer += Time.deltaTime;
                
            }

            if (_specialAttackREnabled)
            {
                _specialAttackRTimer += Time.deltaTime;
            }

            if (_specialAttackRTimer >= _specialAttackRTimeout)
            {
                _animator.SetBool(_animIDSpecialAttackR, false);
                _specialAttackREnabled = false;
                _specialAttackRTimer = 0.0f;
                _input.specialR = false;
                soundPlayed = false;
            }

        }

        private void SpecialAttackF()
        {
            if (_input.specialF)
            {
                _animator.SetBool(_animIDSpecialAttackF, true);
                _specialAttackFEnabled = true;
                _specialAttackFTimer += Time.deltaTime;
                if (soundPlayed == false) {
                    AudioSource.PlayClipAtPoint(Kicking, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                    soundPlayed = true;
                }
            }

            if (_specialAttackFEnabled)
            {
                _specialAttackFTimer += Time.deltaTime;
            }

            if (_specialAttackFTimer >= _specialAttackFTimeout)
            {
                _animator.SetBool(_animIDSpecialAttackF, false);
                _specialAttackFEnabled = false;
                _specialAttackFTimer = 0.0f;
                _input.specialF = false;
                soundPlayed = false;
            }

        }

        private void Transport()
        {
            if (transportTimer > transportTime)
            {
                _controller.enabled = false;
                _cinemachineCamera.GetCinemachineComponent<Cinemachine.CinemachineFramingTransposer>().m_XDamping = 0;
                _cinemachineCamera.GetCinemachineComponent<Cinemachine.CinemachineFramingTransposer>().m_YDamping = 0;
                _cinemachineCamera.GetCinemachineComponent<Cinemachine.CinemachineFramingTransposer>().m_ZDamping = 0;
                if (isPresent)
                {
                    this.transform.position = new Vector3(this.transform.position.x + 100f,
                        this.transform.position.y,
                        this.transform.position.z + 100f);
                    isPresent = false;
                }
                else
                {
                    this.transform.position = new Vector3(this.transform.position.x - 100f,
                        this.transform.position.y,
                        this.transform.position.z - 100f);
                    isPresent = true;
                }
                transportTimer = 0;
                transportTime = Random.Range(15f, 30f);
                Invoke("ResetControllerAndDamping", 0.1f);
                ToggleInvincible();
                Invoke("ToggleInvincible", 1.0f);
                _input.cursorLocked = false;
                cursorImage.SetActive(!cursorImage.activeSelf);
                gun.SetActive(!gun.activeSelf);
                
            }
            else if (almostReady && transportTimer > transportTime - 2)
            {
                ToggleDisplay();
                Invoke("ToggleDisplay", 0.33f);
                Invoke("ToggleDisplay", 0.66f);
                Invoke("ToggleDisplay", 1.0f);
                Invoke("ToggleDisplay", 1.33f);
                Invoke("ToggleDisplay", 1.66f);
                almostReady = false;
            }
            else
            {
                transportTimer += Time.deltaTime;
            }                
        }

        public void ToggleInvincible()
        {
            invincible = !invincible;
        }

        public void OtherCameraUpdate()
        {
            otherTimeCamera.transform.position = new Vector3(_mainCamera.transform.position.x + (isPresent ? 100f: -100f),
                _mainCamera.transform.position.y,
                _mainCamera.transform.position.z + (isPresent ? 100f : -100f));
            otherTimeCamera.transform.rotation = _mainCamera.transform.rotation;
        }

        public void ToggleDisplay()
        {
            otherTimeOverlay.SetActive(!otherTimeOverlay.activeSelf);
        }

        private void ResetControllerAndDamping()
        {
            _controller.enabled = true;
            _cinemachineCamera.GetCinemachineComponent<Cinemachine.CinemachineFramingTransposer>().m_XDamping = 1;
            _cinemachineCamera.GetCinemachineComponent<Cinemachine.CinemachineFramingTransposer>().m_YDamping = 1;
            _cinemachineCamera.GetCinemachineComponent<Cinemachine.CinemachineFramingTransposer>().m_ZDamping = 1;
            otherTimeOverlay.SetActive(false);
            almostReady = true;
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

        public void CanMove()
        {
            canMove = true;
        }

        public void CantMove()
        {
            canMove = false;
        }

        public bool GetInvincible()
        {
            return this.invincible;
        }

        public void BasicAttackHit()
        {
            Collider[] enemiesInRange = Physics.OverlapSphere(this.transform.position + this.transform.forward, 1f);

            foreach (Collider c in enemiesInRange)
            {
                if (c.tag == "Enemy")
                {
                    c.gameObject.GetComponent<EnemyAI>().EnemyHit(4, 25f, this.transform.forward);
                    AudioSource.PlayClipAtPoint(GotHit, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
                else if (c.tag == "Robot")
                {
                    c.gameObject.GetComponent<RobotAI>().EnemyHit(4, 25f, this.transform.forward);
                }
                    
            }
        }

        private void SpecialAttackRHit()
        {
            Collider[] enemiesInRange = Physics.OverlapSphere(this.transform.position + this.transform.forward - this.transform.up, 2f);

            foreach (Collider c in enemiesInRange)
            {
                if (c.tag == "Enemy")
                {
                    c.gameObject.GetComponent<EnemyAI>().EnemyHit(6, 25f, this.transform.up);
                    AudioSource.PlayClipAtPoint(GotHit, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
                else if (c.tag == "Robot")
                {
                    c.gameObject.GetComponent<RobotAI>().EnemyHit(6, 25f, this.transform.up);
                }

            }

        }

        private void SpecialAttackFHit()
        {
            Collider[] enemiesInRange = Physics.OverlapSphere(this.transform.position + this.transform.forward, 1f);

            foreach (Collider c in enemiesInRange)
            {
                if (c.tag == "Enemy")
                {
                    c.gameObject.GetComponent<EnemyAI>().EnemyHit(8, 10f, this.transform.forward);
                    AudioSource.PlayClipAtPoint(GotHit, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                    AudioSource.PlayClipAtPoint(GotHitHard, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
                else if (c.tag == "Robot")
                {
                    c.gameObject.GetComponent<RobotAI>().EnemyHit(8, 10f, this.transform.forward);
                }

            }

        }
    }
}