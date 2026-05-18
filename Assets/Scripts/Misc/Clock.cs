using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField] Transform hoursHand;
    [SerializeField] Transform minutesHand;
    [SerializeField] Transform pendulum;

    const float pendulumAmplitude = 10f; // Maximum angle for the pendulum swing
    const float pendulumFrequency = 2f; // Speed of the pendulum swing

    const float minutesAngleOffset = 90f; // Offset to align the minute hand correctly

    // Update is called once per frame
    void Update()
    {
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
