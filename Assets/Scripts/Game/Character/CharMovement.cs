using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif
using StarterAssets;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
//#if ENABLE_INPUT_SYSTEM 
//[RequireComponent(typeof(PlayerInput))]
//#endif
public class CharMovement : NetworkBehaviour {
    [Header("Player")]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 5.335f;
    [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;
    public float SpeedChangeRate = 10.0f;
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    [Space(10)]
    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    public bool Grounded = true;

    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
    //private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private Rigidbody _rb;
    private StarterAssetsInputs _input;
    private GameObject _mainCamera;
    private MovementControl _movementControl;

    private const float _threshold = 0.01f;

    private bool _hasAnimator;

    //private bool IsCurrentDeviceMouse => _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
    [ClientCallback]
    private void Awake() {
        if (!authority)
            return;
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        _hasAnimator = TryGetComponent(out _animator);
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _input = StarterAssetsInputs.Instance;
        //_playerInput = GetComponent<PlayerInput>();
        _movementControl = GetComponent<MovementControl>();

        AssignAnimationIDs();
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }
    [ClientCallback]
    private void FixedUpdate() {
        if (!authority)
            return;
        if (_animator != null && !_animator.enabled) return;

        JumpAndGravity();
        GroundedCheck();
        Move();
    }

    private void AssignAnimationIDs() {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck() {
        Grounded = _movementControl.IsGrounded;
        if (_hasAnimator) {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void Move() {
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // Smooth animation blend
        float currentSpeed = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).magnitude;
        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.fixedDeltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // Movement direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // Rotate player to face movement direction
        if (_input.move != Vector2.zero) {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        Vector3 move = targetDirection.normalized * (_animationBlend * inputMagnitude);
        move.y = _rb.linearVelocity.y;
        _rb.linearVelocity = move;

        // Animation updates
        if (_hasAnimator) {
            _animator.SetFloat(_animIDSpeed, _animationBlend); // smoothed
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }
    //private float wasJumping = 0f;
    private void JumpAndGravity() {
        if (Grounded) {
            _fallTimeoutDelta = FallTimeout;

            if (_hasAnimator) {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            if (_verticalVelocity < 0.0f)
                _verticalVelocity = -2f;

            if (_input.jump && _jumpTimeoutDelta <= 0.0f) {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                Vector3 velocity = _rb.linearVelocity;
                velocity.y = _verticalVelocity;
                _rb.linearVelocity = velocity;

                if (_hasAnimator)
                    _animator.SetBool(_animIDJump, true);
                //wasJumping = Time.fixedTime;
            }

            if (_jumpTimeoutDelta >= 0.0f)
                _jumpTimeoutDelta -= Time.fixedDeltaTime;
        }
        else {
            _jumpTimeoutDelta = JumpTimeout;

            if (_fallTimeoutDelta >= 0.0f) {
                _fallTimeoutDelta -= Time.fixedDeltaTime;
            }
            else {
                if (_hasAnimator) {
                    _animator.SetBool(_animIDFreeFall, true);
                    _animator.SetBool(_animIDJump, false);
                }
            }
        }

        if (_rb.linearVelocity.y < _terminalVelocity) {
            _rb.linearVelocity += new Vector3(0f, Gravity * Time.fixedDeltaTime, 0f);
        }
    }

    private void OnFootstep(AnimationEvent animationEvent) {
        if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0) {
            int index = Random.Range(0, FootstepAudioClips.Length);
            var clip = FootstepAudioClips[index];
            var audioGO = new GameObject("FootstepAudio");
            audioGO.transform.position = transform.position;
            var source = audioGO.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = FootstepAudioVolume;
            source.pitch = _input.sprint ? (1.25f) : 1.0f;
            source.Play();
            Destroy(audioGO, clip.length / source.pitch);
        }
    }

    private void OnLand(AnimationEvent animationEvent) {
        if (animationEvent.animatorClipInfo.weight > 0.5f) {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume * 8);
        }
    }
}