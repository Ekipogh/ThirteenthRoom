using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    [Header("Hands")]
    [SerializeField] Transform hoursHand;
    [SerializeField] Transform minutesHand;

    [Header("Pendulum")]
    [SerializeField] Transform pendulum;

    [Header("Audio")]
    [SerializeField] AudioSource tickingSound;

    const float pendulumAmplitude = 9f; // Maximum angle for the pendulum swing
    const float pendulumFrequency = Mathf.PI;

    const float minutesAngleOffset = 90f; // Offset to align the minute hand correctly

    private bool _isBroken = false;
    public bool IsBroken
    {
        get { return _isBroken; }
        set { _isBroken = value; tickingSound.enabled = !value; }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsBroken)
        {
            return;
        }
        float hours = System.DateTime.Now.Hour;
        float minutes = System.DateTime.Now.Minute;

        float hoursAngle = (hours % 12 + minutes / 60f) * 30f;
        float minutesAngle = minutes * 6f + minutesAngleOffset;
        hoursHand.localRotation = Quaternion.Euler(0, 0, hoursAngle);
        minutesHand.localRotation = Quaternion.Euler(0, 0, minutesAngle);

        if (pendulum != null)
        {
            float pendulumAngle = Mathf.Sin(Time.time * pendulumFrequency) * pendulumAmplitude;
            pendulum.localRotation = Quaternion.Euler(0, 0, pendulumAngle);
        }
    }
}
