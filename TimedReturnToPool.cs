using UnityEngine;

public class TimedReturnToPool : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;

    private float timer;

    private void OnEnable()
    {
        timer = Mathf.Max(0.01f, lifetime);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            return;
        }

        if (!PoolManager.Return(gameObject))
        {
            Destroy(gameObject);
        }
    }
}