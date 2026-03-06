using UnityEngine;
using TMPro;

/// <summary>
/// Simple floating text that rises and fades over a short time.
/// Attach to a prefab containing a TextMeshPro component.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float duration = 0.8f;
    public Vector3 offset = Vector3.up * 0.5f;

    private TextMeshPro tmp;
    private float elapsed;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    public void SetText(string message, Color color)
    {
        if (tmp == null) tmp = GetComponent<TextMeshPro>();
        tmp.text = message;
        tmp.color = color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += offset * (moveSpeed * Time.deltaTime);

        // fade out over time
        if (tmp != null)
        {
            Color c = tmp.color;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            tmp.color = c;
        }

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }
}