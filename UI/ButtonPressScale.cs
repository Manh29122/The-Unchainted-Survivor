using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private Vector3 pressedScale = new Vector3(0.9f, 0.9f, 0.9f);
    [SerializeField] private bool useCustomOriginalScale = false;
    [SerializeField] private Vector3 originalScale = Vector3.one;

    [Header("Canvas Items")]
    [SerializeField] private GameObject[] objectsToActivateOnPress;
    [SerializeField] private GameObject[] objectsToDeactivateOnPress;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (!useCustomOriginalScale)
        {
            originalScale = transform.localScale;
        }
    }

    private void OnEnable()
    {
        ResetScale();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyPressedScale();
        ApplyCanvasItemState(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetScale();
    }

    public void ApplyPressedScale()
    {
        transform.localScale = pressedScale;
    }

    public void ApplyCanvasItemState(bool pressed)
    {
        if (!pressed)
        {
            return;
        }

        SetObjectsActive(objectsToActivateOnPress, true);
        SetObjectsActive(objectsToDeactivateOnPress, false);
    }

    public void ResetScale()
    {
        transform.localScale = originalScale;
    }

    private void SetObjectsActive(GameObject[] targets, bool activeState)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetActive(activeState);
            }
        }
    }

    public void SetPressedScale(Vector3 newScale)
    {
        pressedScale = newScale;
    }

    public void SetPressedScale(float uniformScale)
    {
        float clampedScale = Mathf.Max(0.01f, uniformScale);
        pressedScale = new Vector3(clampedScale, clampedScale, clampedScale);
    }

    public void SetOriginalScale(Vector3 newScale)
    {
        originalScale = newScale;
        useCustomOriginalScale = true;
        ResetScale();
    }

    public void SetObjectsToActivate(GameObject[] targets)
    {
        objectsToActivateOnPress = targets;
    }

    public void SetObjectsToDeactivate(GameObject[] targets)
    {
        objectsToDeactivateOnPress = targets;
    }
}
