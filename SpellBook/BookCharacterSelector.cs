using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn vào mỗi "vùng nhân vật" trong trang sách.
/// Khi chạm vào → highlight → gọi CharacterManager.ApplyCharacter()
///
/// Setup:
///   Mỗi trang sách tạo 1 GameObject "CharacterSlot_Left" / "CharacterSlot_Right"
///   Gắn script này + Image (làm hitbox) vào mỗi slot
/// </summary>
public class BookCharacterSelector : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    // ─────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────
    [Header("Character Data")]
    [Tooltip("ScriptableObject của nhân vật ở trang này")]
    public CharacterData characterData;

  
    [Tooltip("Border/outline hiển thị khi selected")]
    public GameObject selectedBorder;

    
    [Header("Animation")]
    public float pressScaleAmount = 0.93f;
    public float animDuration = 0.1f;

    // ─────────────────────────────────────────
    //  STATE
    // ─────────────────────────────────────────
    public bool IsSelected { get; private set; }

    private static BookCharacterSelector currentSelected; // Track slot đang được chọn
    private static BookCharacterSelector firstSelected;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private int countSelected = 0; // Đếm số lần đã chọn (dùng để debug)

    // ─────────────────────────────────────────
    void Awake()
    {
        originalScale = transform.localScale;
        selectedBorder = transform.Find("ChooseIcon")?.gameObject;
        if (selectedBorder != null)
            selectedBorder.SetActive(false);       
    }
    void Start()
    {

    }
    // ─────────────────────────────────────────
    //  POINTER EVENTS
    // ─────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {

        ScaleTo(originalScale * pressScaleAmount, animDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ScaleTo(originalScale, animDuration);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectThisSlot();
    }

    // ─────────────────────────────────────────
    //  SELECTION LOGIC
    // ─────────────────────────────────────────

    public void SelectThisSlot()
    {
        if (characterData == null)
        {
            Debug.LogWarning($"[BookSelector] '{name}' chưa gán CharacterData!");
            //return;
        }
       
        // Deselect slot cũ
        if (currentSelected != null && currentSelected.GetEntityId() != this.GetEntityId())
            currentSelected.Deselect();

        IsSelected = true;
        currentSelected = this;
        firstSelected = currentSelected;
        if (selectedBorder != null)
            selectedBorder.SetActive(true);


        // Không có confirm button → apply luôn
        ApplyCharacter();

        Debug.Log($"[BookSelector] Đã chọn: {characterData.characterName}");
    }

    public void Deselect()
    {
        IsSelected = false;
        if (selectedBorder != null)
            selectedBorder.SetActive(false);
        ScaleTo(originalScale, animDuration);

    }

    public void ApplyCharacter()
    {
        if (!IsSelected && characterData == null) return;
        GameManager.Instance._characterManager.ApplyCharacter(characterData);
    }

    // ─────────────────────────────────────────
    //  ANIMATIONS
    // ─────────────────────────────────────────

    void ScaleTo(Vector3 target, float duration)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);


        scaleCoroutine = StartCoroutine(ScaleCoroutine(target, duration));

    }

    IEnumerator ScaleCoroutine(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.localScale = Vector3.Lerp(start, target, EaseOutBack(t));
            yield return null;
        }
        transform.localScale = target;
    }


    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}