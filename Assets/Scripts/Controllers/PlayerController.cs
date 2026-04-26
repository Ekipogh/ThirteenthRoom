using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public InputActionAsset InputActions;

    public Transform HeadJoint;
    public Camera PlayerCamera;
    Vector2 _moveInput = new();

    float _moveSpeed = 5f;
    float _lookSpeed = 0.150f;

    bool _isSprinting = false;
    float _sprintMultiplier = 2f;

    float _maxPitch = 50f;
    float _pitch;

    public bool IsHeadBobEnabled = true;

    float _headHeight;
    float _headBobFrequency = 10f;
    float _headBobAmplitude = 0.005f;

    // Stats
    float _stamina = _maxStamina;

    public float Stamina => _stamina;
    const float _maxStamina = 100f;

    const float _staminaRegenRate = 5f;

    CharacterController _characterController;

    Vector3 _velocity;

    void Start()
    {
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

        _velocity = Vector3.zero;
    }

    void Update()
    {
        ProcessMovement();
        HeadBob();
    }

    private void ProcessMovement()
    {
        _stamina = _isSprinting
            ? Mathf.Max(0, _stamina - Time.deltaTime * 10f)
            : Mathf.Min(_maxStamina, _stamina + Time.deltaTime * _staminaRegenRate);

        if (_stamina <= 0) _isSprinting = false;

        float speed = _isSprinting ? _moveSpeed * _sprintMultiplier : _moveSpeed;

        // Build movement in world space (transform.right/forward respects yaw rotation)
        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;

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
        _isSprinting = value.isPressed;
    }

    Transform GetPitchTransform()
    {
        return HeadJoint != null ? HeadJoint : PlayerCamera.transform;
    }

    float NormalizePitch(float pitch)
    {
        return pitch > 180f ? pitch - 360f : pitch;
    }
}
