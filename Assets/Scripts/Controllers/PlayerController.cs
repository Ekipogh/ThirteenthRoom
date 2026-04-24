using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputActionAsset InputActions;

    public Transform HeadJoint;
    public Camera PlayerCamera;
    Vector2 _moveInput = new();

    float _moveSpeed = 5f;
    float _lookSpeed = 0.150f;

    float _maxPitch = 50f;
    float _pitch;

    public bool IsHeadBobEnabled = true;

    float headHeight;
    float headBobFrequency = 10f;
    float headBobAmplitude = 0.01f;

    void Start()
    {
        _pitch = NormalizePitch(GetPitchTransform().localEulerAngles.x);
        Cursor.lockState = CursorLockMode.Locked;

        headHeight = GetPitchTransform().localPosition.y;

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
    }

    void Update()
    {
        ProcessMovement();
        HeadBob();
    }

    private void ProcessMovement()
    {
        Vector3 move = new(_moveInput.x, 0, _moveInput.y);
        transform.Translate(_moveSpeed * Time.deltaTime * move);
    }

    void ProcessLook(InputValue value)
    {
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
            bobAmount = Mathf.Sin(Time.time * headBobFrequency) * headBobAmplitude;
        }
        else
        {
            headJointPosition.y = Mathf.Lerp(headJointPosition.y, headHeight, Time.deltaTime * headBobFrequency);
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

    Transform GetPitchTransform()
    {
        return HeadJoint != null ? HeadJoint : PlayerCamera.transform;
    }

    float NormalizePitch(float pitch)
    {
        return pitch > 180f ? pitch - 360f : pitch;
    }
}
