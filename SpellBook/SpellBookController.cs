using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Điều khiển quyển sách: Mở → Lật trang → Đóng
/// Hỗ trợ: Button bấm + Swipe mobile
/// Animator triggers: Opening, Closing, NextPage, PreviousPage
/// </summary>
public class SpellBookController : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  ANIMATOR TRIGGER NAMES (khớp với Animator)
    // ─────────────────────────────────────────
    private static class Triggers
    {
        public const string Opening = "Opening";
        public const string Closing = "Closing";
        public const string NextPage = "NextPage";
        public const string PreviousPage = "PreviousPage";
    }

    // ─────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────
    [Header("References")]
    public Animator bookAnimator;

    [Header("Page Content")]
    [Tooltip("Danh sách nội dung từng trang (nhân vật, mô tả...)")]
    public List<PageData> pages = new List<PageData>();

    //[Header("UI Hiển Thị Trang")]
    //public Image characterImage;            // Ảnh nhân vật
    //public TextMeshProUGUI characterNameTxt;
    //public TextMeshProUGUI characterDescTxt;
    //public TextMeshProUGUI pageNumberTxt;   // "2 / 5"

    //[Header("Buttons")]
    //public Button openButton;
    //public Button closeButton;
    //public Button nextButton;
    //public Button prevButton;

    [Header("Swipe Settings (Mobile)")]
    public bool enableSwipe = true;
    public float swipeThreshold = 80f;      // pixel tối thiểu để tính là swipe

    [Header("Timing")]
    [Tooltip("Thời gian chờ animation xong mới cho phép thao tác tiếp")]
    public float openCloseDuration = 0.8f;
    public float flipDuration = 0.5f;

    // ─────────────────────────────────────────
    //  STATE
    // ─────────────────────────────────────────
    public bool IsOpen { get; private set; } = false;
    public int currentPage = 0;
    private bool isAnimating = false;

    // Swipe tracking
    private Vector2 swipeStartPos;
    private bool isSwiping = false;

    // Events
    public event Action OnBookOpened;
    public event Action OnBookClosed;
    public event Action<int> OnPageChanged;     // page index

    // ─────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────
    void Start()
    {
        if (bookAnimator == null)
            bookAnimator = GetComponent<Animator>();

        // Gán button callbacks
        //openButton?.onClick.AddListener(OpenBook);
        //closeButton?.onClick.AddListener(CloseBook);
        //nextButton?.onClick.AddListener(GoNextPage);
        //prevButton?.onClick.AddListener(GoPrevPage);

        // Trạng thái ban đầu
        RefreshUI();
        UpdateButtonStates();
        ResetAndLoadCharacter();
    }

    void Update()
    {
        if (enableSwipe && IsOpen)
            HandleSwipeInput();
    }

    // ─────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────

    /// <summary>Mở sách (Entry → Opening)</summary>
    public void OpenBook()
    {
        if (IsOpen || isAnimating) return;

        currentPage = 0;
        StartCoroutine(PlayAnimation(Triggers.Opening, openCloseDuration, () =>
        {
            IsOpen = true;
            RefreshUI();
            UpdateButtonStates();
            OnBookOpened?.Invoke();
        }));
    }

    /// <summary>Đóng sách</summary>
    public void CloseBook()
    {
        if (!IsOpen || isAnimating) return;

        StartCoroutine(PlayAnimation(Triggers.Closing, openCloseDuration, () =>
        {
            IsOpen = false;
            UpdateButtonStates();
            OnBookClosed?.Invoke();
        }));
    }

    /// <summary>Lật sang trang kế</summary>
    public void GoNextPage()
    {
        if (!IsOpen || isAnimating) return;
        if (currentPage >= pages.Count - 1) return;

        StartCoroutine(PlayAnimation(Triggers.NextPage, flipDuration, () =>
        {
            currentPage++;
            RefreshUI();
            UpdateButtonStates();
            OnPageChanged?.Invoke(currentPage);
        }));
    }

    /// <summary>Lật về trang trước</summary>
    public void GoPrevPage()
    {
        if (!IsOpen || isAnimating) return;
        if (currentPage <= 0) return;

        StartCoroutine(PlayAnimation(Triggers.PreviousPage, flipDuration, () =>
        {
            currentPage--;
            RefreshUI();
            UpdateButtonStates();
            OnPageChanged?.Invoke(currentPage);
        }));
    }

    /// <summary>Nhảy thẳng đến trang chỉ định</summary>
    public void GoToPage(int index)
    {
        if (!IsOpen || isAnimating) return;
        index = Mathf.Clamp(index, 0, pages.Count - 1);
        if (index == currentPage) return;

        string trigger = index > currentPage ? Triggers.NextPage : Triggers.PreviousPage;
        int steps = Mathf.Abs(index - currentPage);

        StartCoroutine(FlipMultiplePages(trigger, steps, index));
    }

    // ─────────────────────────────────────────
    //  COROUTINES
    // ─────────────────────────────────────────

    IEnumerator PlayAnimation(string trigger, float duration, Action onComplete = null)
    {
        isAnimating = true;
        UpdateButtonStates();

        bookAnimator.SetTrigger(trigger);
        yield return new WaitForSeconds(duration);

        isAnimating = false;
        onComplete?.Invoke();
        UpdateButtonStates();
    }

    IEnumerator FlipMultiplePages(string trigger, int steps, int targetPage)
    {
        isAnimating = true;
        UpdateButtonStates();

        for (int i = 0; i < steps; i++)
        {
            bookAnimator.SetTrigger(trigger);
            yield return new WaitForSeconds(flipDuration);

            currentPage += trigger == Triggers.NextPage ? 1 : -1;
            RefreshUI();
        }

        isAnimating = false;
        currentPage = targetPage;
        RefreshUI();
        UpdateButtonStates();
        OnPageChanged?.Invoke(currentPage);
    }

    // ─────────────────────────────────────────
    //  SWIPE INPUT (Mobile)
    // ─────────────────────────────────────────

    void HandleSwipeInput()
    {
        // Touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                swipeStartPos = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                ProcessSwipeDelta(touch.position - swipeStartPos);
                isSwiping = false;
            }
        }

        // Mouse (test trên editor)
        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPos = Input.mousePosition;
            isSwiping = true;
        }
        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            ProcessSwipeDelta((Vector2)Input.mousePosition - swipeStartPos);
            isSwiping = false;
        }
    }

    void ProcessSwipeDelta(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) < swipeThreshold) return; // Quá ngắn → bỏ qua

        if (delta.x < 0)
            GoNextPage();       // Swipe trái → trang sau
        else
            GoPrevPage();       // Swipe phải → trang trước
        
    }

    // ─────────────────────────────────────────
    //  UI REFRESH
    // ─────────────────────────────────────────

    void RefreshUI()
    {
        if (pages.Count == 0) return;
        if (currentPage < 0 || currentPage >= pages.Count) return;
        
        PageData page = pages[currentPage];
        //GetComponentInChildren<BookPageDisplay>()?.Display(pages[currentPage].characterData);
        //if (characterImage != null && page.characterSprite != null)
        //    characterImage.sprite = page.characterSprite;

        //if (characterNameTxt != null)
        //    characterNameTxt.text = page.characterName;

        //if (characterDescTxt != null)
        //    characterDescTxt.text = page.description;

        //if (pageNumberTxt != null)
        //    pageNumberTxt.text = $"{currentPage + 1} / {pages.Count}";
    }

    public void ResetAndLoadCharacter()
    {
        GameManager.Instance._characterManager.DisplayCharacterInSpellBook(currentPage);
    }
    void UpdateButtonStates()
    {
        bool canInteract = IsOpen && !isAnimating;

        //if (openButton) openButton.interactable = !IsOpen && !isAnimating;
        //if (closeButton) closeButton.interactable = canInteract;
        //if (nextButton) nextButton.interactable = canInteract && currentPage < pages.Count - 1;
        //if (prevButton) prevButton.interactable = canInteract && currentPage > 0;
    }

    // ─────────────────────────────────────────
    //  DEBUG
    // ─────────────────────────────────────────
#if UNITY_EDITOR
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        GUILayout.Label($"Book: {(IsOpen ? "OPEN" : "CLOSED")}");
        GUILayout.Label($"Page: {currentPage + 1}/{pages.Count}");
        GUILayout.Label($"Animating: {isAnimating}");
        GUILayout.EndArea();
    }
#endif
}

// ─────────────────────────────────────────────
//  DATA CLASS: Nội dung 1 trang sách
// ─────────────────────────────────────────────
[Serializable]
public class PageData
{
    public string characterName;
    [TextArea(2, 4)]
    public string description;
    public Sprite characterSprite;
    // Thêm field tuỳ ý: stat, skill icon, level requirement...
}