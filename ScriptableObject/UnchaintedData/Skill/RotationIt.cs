using UnityEngine;

public class RotationIt : MonoBehaviour
{
    [SerializeField] private float orbitVelocity = 1f; // Radians per second

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0, orbitVelocity * Mathf.Rad2Deg * Time.deltaTime, Space.Self);
    }
}
