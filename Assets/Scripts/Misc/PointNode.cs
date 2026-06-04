using UnityEngine;

public class PointNode : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        const float sphereRadius = 0.25f;
        var sphereTransformPosition = transform.position + Vector3.up * sphereRadius;
        Gizmos.DrawWireSphere(sphereTransformPosition, sphereRadius);
        Gizmos.DrawLine(sphereTransformPosition, sphereTransformPosition + transform.forward);
    }
}