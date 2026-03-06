using UnityEngine;

/// <summary>
/// Simple camera follow behaviour. Attach to the main camera and it will
/// track the player GameObject with optional smoothing and offset.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Transform of the object to follow (automatically finds Tag=Player if null)")]
    public Transform target;

    [Tooltip("World-space offset from the target position")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("Smoothing factor (0 = instant, 1 = no movement)")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.125f;

    private void Awake()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        if (smoothSpeed <= 0f)
        {
            transform.position = desiredPosition;
        }
        else
        {
            Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition, 1f - smoothSpeed);
            transform.position = smoothed;
        }
    }
}
