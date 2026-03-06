using System;
using UnityEngine;

/// <summary>
/// Phát hi?n vu?t trái/ph?i trên màn hình mobile (landscape).
/// G?n vào b?t k? GameObject nào trong scene.
/// </summary>
public class SwipeDetector : MonoBehaviour
{
    public Animator animator;    // Th?m để debug, có thể xóa sau khi test swipe ổn định

    [Header("Swipe Settings")]
    [Tooltip("Kho?ng cách t?i thi?u (px) ?? tính là swipe, tránh nh?m v?i tap")]
    public float minSwipeDistance = 80f;

    [Tooltip("Gi?i h?n góc l?ch d?c (??). Vu?t l?ch quá s? b? b? qua")]
    public float maxVerticalAngle = 35f;

    [Tooltip("Th?i gian t?i ?a (giây) ?? hoàn thành 1 swipe")]
    public float maxSwipeTime = 0.4f;

    // ?? Events ???????????????????????????????
    public event Action OnSwipeLeft;
    public event Action OnSwipeRight;

    // ?? Runtime ??????????????????????????????
    private Vector2 startPos;
    private float startTime;
    private bool isTouching;

    // ?????????????????????????????????????????
    void Update()
    {
        //animator = GetComponent<Animator>();   // Th?m để debug, có thể xóa sau khi test swipe ổn định
#if UNITY_EDITOR
        HandleMouseInput();   // Test trên editor b?ng chu?t
#else
        HandleTouchInput();   // Thi?t b? th?t
#endif
    }

    // ?????????????????????????????????????????
    //  TOUCH (thi?t b? th?t)
    // ?????????????????????????????????????????
    void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                startPos = touch.position;
                startTime = Time.realtimeSinceStartup;
                isTouching = true;
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (isTouching)
                {
                    EvaluateSwipe(touch.position);
                    isTouching = false;
                }
                break;
        }
    }

    // ?????????????????????????????????????????
    //  MOUSE (test trên Unity Editor)
    // ?????????????????????????????????????????
    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            startTime = Time.realtimeSinceStartup;
            isTouching = true;
        }

        if (Input.GetMouseButtonUp(0) && isTouching)
        {
            EvaluateSwipe(Input.mousePosition);
            isTouching = false;
        }
    }

    // ?????????????????????????????????????????
    //  ?ÁNH GIÁ SWIPE
    // ?????????????????????????????????????????
    void EvaluateSwipe(Vector2 endPos)
    {
        if (animator.GetBool("CanChangePage") == false) return;
        Vector2 delta = endPos - startPos;
        float elapsedTime = Time.realtimeSinceStartup - startTime;

        // 1. Quá ch?m ? không tính
        if (elapsedTime > maxSwipeTime) return;

        // 2. Quá ng?n ? có th? là tap, b? qua
        if (delta.magnitude < minSwipeDistance) return;

        // 3. Ki?m tra góc l?ch d?c
        //    Trên landscape, swipe h?p l? ph?i g?n n?m ngang
        float angle = Mathf.Abs(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        //    angle = 0° là vu?t ph?i, 180° là vu?t trái
        //    L?ch d?c quá maxVerticalAngle ? b? qua
        bool isHorizontal = angle <= maxVerticalAngle              // G?n 0° (ph?i)
                         || angle >= (180f - maxVerticalAngle);    // G?n 180° (trái)

        if (!isHorizontal) return;

        // 4. Xác ??nh h??ng
        if (delta.x < 0)
        {
            //GameManager.Instance._spellBookController.currentPage += 1;
            //if (GameManager.Instance._spellBookController.currentPage > (int)((GameManager.Instance._characterManager.CharacterList.Count - 1) / 2))
            //{
            //    GameManager.Instance._spellBookController.currentPage = (int)GameManager.Instance._characterManager.CharacterList.Count / 2;
            //    return;
            //}
            //animator.SetTrigger("NextPage");
            //Debug.Log("[Swipe] ? TRÁI");
            //OnSwipeLeft?.Invoke();                   
            
        }
        else
        {
            //GameManager.Instance._spellBookController.currentPage -= 1;
            //if (GameManager.Instance._spellBookController.currentPage <= -1)
            //{
            //    GameManager.Instance._spellBookController.currentPage = 0;
            //    return;
            //}
            //animator.SetTrigger("PreviousPage");
            //Debug.Log("[Swipe] ? PH?I");
            //OnSwipeRight?.Invoke();           
           
        }
        //ResetAndLoadCharacter(GameManager.Instance._spellBookController.currentPage);
        //Debug.Log(GameManager.Instance._spellBookController.currentPage);
    }
    public void ResetAndLoadCharacter(int currentPage)
    {
        GameManager.Instance._characterManager.DisplayCharacterInSpellBook(currentPage);
    }
}