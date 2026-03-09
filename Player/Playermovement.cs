using UnityEngine;
using UnityEngine.EventSystems;

// ══════════════════════════════════════════════════════════
//
//  JOYSTICK - Hỗ trợ 3 loại:
//
//  ┌─────────────┬──────────────────────────────────────────┐
//  │ Fixed       │ Cố định 1 chỗ, không di chuyển           │
//  │ Floating    │ Hiện tại chỗ ngón tay chạm xuống         │
//  │ Dynamic     │ Background đi theo ngón tay khi kéo ra   │
//  └─────────────┴──────────────────────────────────────────┘
//
//  Setup:
//    1. Canvas (Screen Space - Overlay)
//    2. Image "JoystickBackground" → gắn Joystick.cs
//    3. Image con "JoystickKnob"   → kéo vào field knob
//    4. Chọn JoystickType trong Inspector
//
// ══════════════════════════════════════════════════════════



// ══════════════════════════════════════════════════════════
//  PLAYER MOVEMENT
//  Đọc input từ Joystick → di chuyển Rigidbody2D
// ══════════════════════════════════════════════════════════

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Joystick joystick;

    [Header("Stats")]
    public float moveSpeed = 5f;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    // ── Animator hashes ──────────────────────
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
    private static readonly int AnimMoveY = Animator.StringToHash("MoveY");

    // ── Runtime ──────────────────────────────
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveDir;
    private Vector3 originalLocalScale;

    // ─────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalLocalScale = transform.localScale;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (joystick == null)
            joystick = FindFirstObjectByType<Joystick>();
        Debug.Log(joystick);   
    }

    void Update()
    {
        ReadInput();
        UpdateAnimation();
        FlipSprite();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveDir * moveSpeed;
    }

    // ─────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────

    void ReadInput()
    {
         
        if (joystick != null && joystick.IsTouching)
        {
            moveDir = joystick.Input;
        }
           
        else
            // Fallback WASD để test trên Editor
            moveDir = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical")
            ).normalized;
    }

    // ─────────────────────────────────────────
    //  ANIMATION
    // ─────────────────────────────────────────

    void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = moveDir.magnitude;
        animator.SetFloat(AnimSpeed, speed);

        if (speed > 0.01f)
        {
            animator.SetFloat(AnimMoveX, moveDir.x);
            animator.SetFloat(AnimMoveY, moveDir.y);
        }
    }

    void FlipSprite()
    {
        if (Mathf.Abs(moveDir.x) < 0.01f) return;

        Vector3 nextScale = transform.localScale;
        float baseScaleX = Mathf.Abs(originalLocalScale.x);
        nextScale.x = moveDir.x < 0f ? baseScaleX : -baseScaleX;
        transform.localScale = nextScale;
    }

    // ─────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────

    public void SetSpeed(float speed) => moveSpeed = speed;
    public void SetMovementEnabled(bool enabled)
    {
        if (!enabled) { moveDir = Vector2.zero; rb.linearVelocity = Vector2.zero; }
        this.enabled = enabled;
    }

    public bool IsMoving => moveDir.magnitude > 0.01f;
    public Vector2 MoveDirection => moveDir;
}