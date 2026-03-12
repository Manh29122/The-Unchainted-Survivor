using UnityEngine;
using TMPro;

/// <summary>
/// Simple floating text that rises and fades over a short time.
/// Attach to a prefab containing a TMP text component.
/// </summary>
public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float duration = 0.8f;
    public Vector3 offset = Vector3.up * 0.5f;

    private TMP_Text tmp;
    private RectTransform rectTransform;
    private float elapsed;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        rectTransform = transform as RectTransform;
    }

    private void OnEnable()
    {
        elapsed = 0f;

        if (tmp == null)
        {
            tmp = GetComponent<TMP_Text>();
        }
    }

    public void SetText(string message, Color color)
    {
        if (tmp == null) tmp = GetComponent<TMP_Text>();
        tmp.text = message;
        tmp.color = color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += (Vector2)(offset * (moveSpeed * Time.deltaTime));
        }
        else
        {
            transform.position += offset * (moveSpeed * Time.deltaTime);
        }

        // fade out over time
        if (tmp != null)
        {
            Color c = tmp.color;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            tmp.color = c;
        }

        if (elapsed >= duration)
        {
            if (!PoolManager.Return(gameObject))
            {
                Destroy(gameObject);
            }
        }
    }
}