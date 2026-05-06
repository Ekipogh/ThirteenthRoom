using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Room StartingRoom;
    [SerializeField] RoomTracker RoomTracker;

    public InputActionAsset InputActions;

    public Transform HeadJoint;
    public Camera PlayerCamera;
    Vector2 _moveInput = new();

    float _moveSpeed = 5f;
    float _lookSpeed = 0.150f;

    bool _isSprinting = false;
    float _sprintMultiplier = 2f;

    float _maxPitch = 70f;
    float _pitch;

    public bool IsHeadBobEnabled = true;

    float _headHeight;
    readonly float _sprintHeadForwardOffset = 0.7f;
    float _headBobFrequency = 10f;
    float _headBobAmplitude = 0.005f;

    // Stats
    float _stamina = _maxStamina;
    float _lastDisplayedStamina = -1f;

    public float Stamina => _stamina;
    const float _maxStamina = 100f;
    const float _staminaRegenRate = 5f;
    const float _staminaDrainRate = 10f;
    const float _staminaChangeEpsilon = 0.01f;

    const float _staminaRegenDelay = 2f; // in seconds
    const float _exhaustedStaminaRegenDelay = 5f; // longer delay when stamina is fully drained
    float _staminaRegenTimer = 0f;

    CharacterController _characterController;
    private float _colliderRadius;

    Vector3 _velocity;

    public Microlight.MicroBar.MicroBar StaminaBar;

    // Audio
    [SerializeField] PlayerAudioManager AudioManager;
    float _distanceTraveled = 0f;
    const float FootstepDistanceThreshold = 2.5f; // distance player must travel before triggering next footstep sound

    void InitializeStartingRoom()
    {
        if (StartingRoom == null)
        {
            Debug.LogWarning("PlayerController: StartingRoom is not assigned.");
            return;
        }
        if (RoomTracker == null)
        {
            Debug.LogWarning("PlayerController: RoomTracker is not assigned.");
            return;
        }

        RoomTracker.Initialize(StartingRoom);

        if (StartingRoom.PlayerSpawnPoint != null)
        {
            transform.SetPositionAndRotation(StartingRoom.PlayerSpawnPoint.position, StartingRoom.PlayerSpawnPoint.rotation);
        }
        else
        {
            Debug.LogWarning($"PlayerController: StartingRoom '{StartingRoom.name}' has no PlayerSpawnPoint assigned.");
        }
    }

    void Start()
    {
        InitializeStartingRoom();
        _pitch = NormalizePitch(GetPitchTransform().localEulerAngles.x);
        Cursor.lockState = CursorLockMode.Locked;

        _headHeight = GetPitchTransform().localPosition.y;

        if (InputActions == null)
        {
            Debug.LogError("InputActions asset is not assigned in the inspector.");
            return;
        }
        // disable all action maps and then enable the Player action map
        foreach (var map in InputActions.actionMaps)
        {
            map.Disable();
        }
        InputActions.FindActionMap("Player").Enable();

        _characterController = GetComponent<CharacterController>();
        _colliderRadius = _characterController.radius;

        _velocity = Vector3.zero;

        if (StaminaBar != null)
        {
            StaminaBar.Initialize(_maxStamina);
            StaminaBar.UpdateBar(_stamina, true);
            StaminaBar.gameObject.SetActive(false);
            _lastDisplayedStamina = _stamina;
        }
    }

    void UpdateStaminaBar()
    {
        if (StaminaBar == null) return;

        if (Mathf.Abs(_stamina - _lastDisplayedStamina) <= _staminaChangeEpsilon)
        {
            return;
        }

        bool shouldShowBar = _stamina < _maxStamina;
        if (shouldShowBar && !StaminaBar.gameObject.activeSelf)
        {
            StaminaBar.gameObject.SetActive(true);
        }

        Microlight.MicroBar.UpdateAnim updateAnim = _stamina < _lastDisplayedStamina
            ? Microlight.MicroBar.UpdateAnim.Damage
            : Microlight.MicroBar.UpdateAnim.Heal;

        StaminaBar.UpdateBar(_stamina, updateAnim);
        _lastDisplayedStamina = _stamina;

        if (!shouldShowBar && StaminaBar.gameObject.activeSelf)
        {
            StaminaBar.gameObject.SetActive(false);
        }
    }

    bool IsMoving()
    {
        return _moveInput.sqrMagnitude > 0.01f;
    }

    void Update()
    {
        ProcessMovement();
        SprintingHeadForwardOffset();
        HeadBob();
        UpdateStaminaBar();
    }

    void SprintingHeadForwardOffset()
    {
        if (HeadJoint == null) return;

        Vector3 localPosition = GetPitchTransform().localPosition;
        float targetZ = _moveInput.magnitude > 0.1f ? _sprintHeadForwardOffset : 0f;
        localPosition.z = Mathf.Lerp(localPosition.z, targetZ, Time.deltaTime * 5f);
        GetPitchTransform().localPosition = localPosition;
        // if moving widen the collider to match the head forward offset, otherwise reset it
        _characterController.radius = Mathf.Lerp(_characterController.radius, _moveInput.magnitude > 0.1f ? _colliderRadius + _sprintHeadForwardOffset : _colliderRadius, Time.deltaTime * 5f);
    }

    private void ProcessMovement()
    {
        bool isSprinting = _isSprinting && IsMoving() && _stamina > 0f;

        // Update audio based on sprinting state
        if (AudioManager != null)
        {
            AudioManager.SetSprinting(isSprinting);
        }

        // Drain stamin if sprinting
        // Regen stamina if not sprinting, but only after a delay
        if (isSprinting)
        {
            _stamina = Mathf.Max(0, _stamina - Time.deltaTime * _staminaDrainRate);
            _staminaRegenTimer = 0f; // reset regen timer when sprinting
        }
        else
        {
            if (_stamina < _maxStamina)
            {
                _staminaRegenTimer += Time.deltaTime;
                float regenDelay = _stamina <= 0 ? _exhaustedStaminaRegenDelay : _staminaRegenDelay;
                if (_staminaRegenTimer >= regenDelay)
                {
                    _stamina = Mathf.Min(_maxStamina, _stamina + Time.deltaTime * _staminaRegenRate);
                }
            }
        }

        if (_stamina <= 0) _isSprinting = false;

        float speed = isSprinting ? _moveSpeed * _sprintMultiplier : _moveSpeed;

        // Build movement in world space (transform.right/forward respects yaw rotation)
        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        // Footstep audio
        if (AudioManager != null && IsMoving())
        {
            _distanceTraveled += speed * Time.deltaTime;
            if (_distanceTraveled >= FootstepDistanceThreshold)
            {
                AudioManager.PlayRandomFootstep();
                _distanceTraveled = 0f;
            }
        }

        // Apply gravity
        if (_characterController.isGrounded)
            _velocity.y = -2f; // small downward force to keep grounded
        else
            _velocity.y += Physics.gravity.y * Time.deltaTime;

        Vector3 delta = (move * speed + _velocity) * Time.deltaTime;
        _characterController.Move(delta);
    }

    void ProcessLook(InputValue value)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        Vector2 input = value.Get<Vector2>();
        float mouseX = input.x * _lookSpeed;
        float mouseY = input.y * _lookSpeed;

        _pitch = Mathf.Clamp(_pitch - mouseY, -_maxPitch, _maxPitch);
        float yaw = transform.localEulerAngles.y + mouseX;

        GetPitchTransform().localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        transform.localEulerAngles = new Vector3(0, yaw, 0);
    }

    void HeadBob()
    {
        if (!IsHeadBobEnabled) return;

        Vector3 headJointPosition = GetPitchTransform().localPosition;
        float bobAmount = 0f;
        if (_moveInput.magnitude > 0.1f)
        {
            bobAmount = Mathf.Sin(Time.time * _headBobFrequency) * _headBobAmplitude;
        }
        else
        {
            headJointPosition.y = Mathf.Lerp(headJointPosition.y, _headHeight, Time.deltaTime * _headBobFrequency);
        }
        headJointPosition.y += bobAmount;
        GetPitchTransform().localPosition = headJointPosition;
    }

    void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        _moveInput = input;
    }

    void OnLook(InputValue value)
    {
        ProcessLook(value);
    }

    void OnSprint(InputValue value)
    {
        _isSprinting = value.isPressed && _stamina > 0f;
    }

    Transform GetPitchTransform()
    {
        return HeadJoint != null ? HeadJoint : PlayerCamera.transform;
    }

    float NormalizePitch(float pitch)
    {
        return pitch > 180f ? pitch - 360f : pitch;
    }

    public int CurrentGait
    {
        get
        {
            if (_moveInput.magnitude < 0.1f)
                return 0; // Idle
            else if (_isSprinting)
                return 2; // Run
            else
                return 1; // Walk
        }
    }

    public float Speed()
    {
        Debug.Log($"Current velocity: {_velocity}");
        return new Vector3(_moveInput.x * _moveSpeed, 0, _moveInput.y * _moveSpeed).magnitude;
    }
}
