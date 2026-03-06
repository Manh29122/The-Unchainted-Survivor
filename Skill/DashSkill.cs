using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Lightning Dash Skill - Performs a lightning-fast dash forward, damaging all enemies in the path.
/// Perfect for Vampire Survivors-style gameplay where the player needs to clear waves of enemies.
/// </summary>
public class DashSkill : MonoBehaviour
{
    [Header("Dash Settings")]
    [Tooltip("How fast the player dashes (units per second)")]
    [SerializeField] private float dashSpeed = 25f;

    [Tooltip("How far the dash travels")]
    [SerializeField] private float dashDistance = 8f;

    [Tooltip("How long the dash lasts (calculated from speed/distance)")]
    [SerializeField] private float dashDuration = 0.32f;

    [Header("Damage Settings")]
    [Tooltip("Damage dealt to enemies during dash")]
    [SerializeField] private float dashDamage = 50f;

    [Tooltip("Radius of damage hitbox around player during dash")]
    [SerializeField] private float damageRadius = 2f;

    [Tooltip("How often damage is applied (seconds)")]
    [SerializeField] private float damageInterval = 0.1f;

    [Header("Cooldown Settings")]
    [Tooltip("Time between dashes (seconds)")]
    [SerializeField] private float cooldownTime = 2f;

    [Header("Visual Effects")]
    [Tooltip("Particle effect played during dash")]
    [SerializeField] private ParticleSystem dashEffect;

    [Tooltip("Trail renderer for dash visual")]
    [SerializeField] private TrailRenderer dashTrail;

    [Header("Audio")]
    [Tooltip("Sound played when dashing")]
    [SerializeField] private AudioClip dashSound;

    [Header("Invincibility")]
    [Tooltip("Player is invincible during dash")]
    [SerializeField] private bool invincibleDuringDash = true;

    [Tooltip("Reference to player's health component")]
    [SerializeField] private PlayerStats playerHealthComponent;

    [Header("Input")]
    [Tooltip("Key to trigger dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.Space;

    [Tooltip("Reference to joystick for directional dashing")]
    [SerializeField] private Joystick joystick;

    // Private variables
    private bool isDashing = false;
    private bool canDash = true;
    private float lastDashTime;
    private Vector3 dashStartPosition;
    private Vector3 dashDirection;
    private CharacterController characterController;
    private AudioSource audioSource;
    private Coroutine dashCoroutine;

    // Invincibility tracking
    private bool originalInvincibilityState;
    private System.Reflection.PropertyInfo invincibilityProperty;
    private System.Reflection.MethodInfo setInvincibleMethod;

    // Damage tracking
    private float lastDamageTime;
    private List<GameObject> hitEnemies = new List<GameObject>();

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Find joystick if not assigned
        if (joystick == null)
        {
            joystick = FindFirstObjectByType<Joystick>();
        }

        // Auto-calculate dash duration if not set
        if (dashDuration <= 0)
        {
            dashDuration = dashDistance / dashSpeed;
        }

        // Setup invincibility system
        SetupInvincibilitySystem();
    }

    private void Update()
    {
        // Check for dash input
        if (Input.GetKeyDown(dashKey) && canDash && !isDashing)
        {
            StartDash();
        }

        // Update cooldown
        if (!canDash && Time.time - lastDashTime >= cooldownTime)
        {
            canDash = true;
        }
    }

    /// <summary>
    /// Initiates the dash ability
    /// </summary>
    public void StartDash()
    {
        if (!canDash || isDashing) return;

        isDashing = true;
        canDash = false;
        lastDashTime = Time.time;
        dashStartPosition = transform.position;

        // Get dash direction (forward relative to camera or player facing)
        dashDirection = GetDashDirection();

        // Set invincibility
        SetPlayerInvincibility(true);

        // Play effects
        PlayDashEffects();

        // Start dash coroutine
        dashCoroutine = StartCoroutine(PerformDash());

        Debug.Log("Dash started! Speed: " + dashSpeed + ", Distance: " + dashDistance);
    }

    /// <summary>
    /// Determines the direction of the dash
    /// </summary>
    private Vector3 GetDashDirection()
    {
        // Check if joystick is available and being used
        if (joystick != null && joystick.IsTouching && joystick.Input != Vector2.zero)
        {
            // Dash in the direction of joystick input
            return new Vector3(joystick.Input.x, 0f, joystick.Input.y).normalized;
        }
        else
        {
            // Fallback: dash forward relative to player facing (for 2D top-down)
            return transform.right.normalized; // Changed from forward to right for 2D
        }
    }

    /// <summary>
    /// Coroutine that handles the dash movement and damage
    /// </summary>
    private IEnumerator PerformDash()
    {
        float elapsedTime = 0f;
        lastDamageTime = 0f;
        hitEnemies.Clear();

        while (elapsedTime < dashDuration)
        {
            float deltaTime = Time.deltaTime;

            // Calculate movement
            Vector3 movement = dashDirection * dashSpeed * deltaTime;

            // Move the player
            if (characterController != null)
            {
                characterController.Move(movement);
            }
            else
            {
                transform.position += movement;
            }

            // Apply damage to enemies in path
            if (Time.time - lastDamageTime >= damageInterval)
            {
                DamageEnemiesInPath();
                lastDamageTime = Time.time;
            }

            elapsedTime += deltaTime;
            yield return null;
        }

        // End dash
        EndDash();
    }

    /// <summary>
    /// Damages all enemies within the damage radius
    /// </summary>
    private void DamageEnemiesInPath()
    {
        // Find all enemies in damage radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius);

        foreach (Collider collider in hitColliders)
        {
            // Check if it's an enemy (you'll need to tag your enemies appropriately)
            if (collider.CompareTag("Enemy") && !hitEnemies.Contains(collider.gameObject))
            {
                // Damage the enemy
                EnemyHealth enemyHealth = collider.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(dashDamage);
                    hitEnemies.Add(collider.gameObject);

                    // Optional: Add hit effect
                    Debug.Log("Dashed through enemy for " + dashDamage + " damage!");
                }
            }
        }
    }

    /// <summary>
    /// Plays visual and audio effects for the dash
    /// </summary>
    private void PlayDashEffects()
    {
        // Play dash sound
        if (dashSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dashSound);
        }

        // Enable trail effect
        if (dashTrail != null)
        {
            dashTrail.enabled = true;
            dashTrail.Clear();
        }

        // Play particle effect
        if (dashEffect != null)
        {
            dashEffect.Play();
        }
    }

    /// <summary>
    /// Ends the dash and cleans up effects
    /// </summary>
    private void EndDash()
    {
        isDashing = false;

        // Restore invincibility state
        SetPlayerInvincibility(false);

        // Disable trail effect
        if (dashTrail != null)
        {
            dashTrail.enabled = false;
        }

        // Stop particle effect
        if (dashEffect != null)
        {
            dashEffect.Stop();
        }

        Debug.Log("Dash ended!");
    }

    /// <summary>
    /// Checks if the player can currently dash
    /// </summary>
    public bool CanDash()
    {
        return canDash && !isDashing;
    }

    /// <summary>
    /// Gets the remaining cooldown time
    /// </summary>
    public float GetCooldownRemaining()
    {
        if (canDash) return 0f;
        return cooldownTime - (Time.time - lastDashTime);
    }

    /// <summary>
    /// Forces the dash to end early (useful for stun effects, etc.)
    /// </summary>
    public void InterruptDash()
    {
        if (isDashing && dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            EndDash();
        }
    }

    /// <summary>
    /// Sets up the invincibility system by finding the appropriate health component
    /// </summary>
    private void SetupInvincibilitySystem()
    {
        if (!invincibleDuringDash) return;

        // Try to find health component automatically if not assigned
        if (playerHealthComponent == null)
        {
            playerHealthComponent = GetComponentInChildren<PlayerStats>();
            // Look for common health component names
            var healthComponents = GetComponents<PlayerStats>()
                .Where(c => c.GetType().Name.ToLower().Contains("health") ||
                           c.GetType().Name.ToLower().Contains("player"))
                .ToArray();

            if (healthComponents.Length > 0)
            {
                playerHealthComponent = healthComponents[0];
            }
        }

        if (playerHealthComponent != null)
        {
            var type = playerHealthComponent.GetType();

            // Look for common invincibility property names
            invincibilityProperty = type.GetProperty("IsInvincible") ??
                                   type.GetProperty("Invincible") ??
                                   type.GetProperty("isInvincible") ??
                                   type.GetProperty("invincible");

            // Look for common invincibility method names
            setInvincibleMethod = type.GetMethod("SetInvincible") ??
                                 type.GetMethod("SetInvincibility") ??
                                 type.GetMethod("MakeInvincible");

            if (invincibilityProperty != null || setInvincibleMethod != null)
            {
                Debug.Log("Invincibility system ready for: " + playerHealthComponent.GetType().Name);
            }
            else
            {
                Debug.LogWarning("No invincibility property/method found in: " + playerHealthComponent.GetType().Name);
            }
        }
        else
        {
            Debug.LogWarning("No health component found for invincibility system!");
        }
    }

    /// <summary>
    /// Sets the player's invincibility state
    /// </summary>
    private void SetPlayerInvincibility(bool invincible)
    {
        if (!invincibleDuringDash || playerHealthComponent == null) return;

        try
        {
            if (invincible)
            {
                // Store original state
                if (invincibilityProperty != null)
                {
                    originalInvincibilityState = (bool)invincibilityProperty.GetValue(playerHealthComponent);
                }
            }

            // Set new invincibility state
            if (invincibilityProperty != null)
            {
                invincibilityProperty.SetValue(playerHealthComponent, invincible);
                Debug.Log("Player invincibility set to: " + invincible);
            }
            else if (setInvincibleMethod != null)
            {
                setInvincibleMethod.Invoke(playerHealthComponent, new object[] { invincible });
                Debug.Log("Player invincibility set to: " + invincible + " (via method)");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to set player invincibility: " + e.Message);
        }
    }
}