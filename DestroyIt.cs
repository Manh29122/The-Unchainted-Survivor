using UnityEngine;

public class DestroyIt : MonoBehaviour
{
    public void DestroyThis()
    {
        if (!PoolManager.Return(gameObject))
        {
            Destroy(gameObject);
        }
    }
}
