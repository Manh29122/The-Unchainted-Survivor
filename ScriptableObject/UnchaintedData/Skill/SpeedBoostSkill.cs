using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Speed Boost Skill - Greatly increases movement speed for a brief moment.
/// Perfect for dodging enemies, chasing down targets, or escaping dangerous situations.
/// Works in combination with PlayerMovement.cs to temporarily boost movement speed.
/// </summary>
public class SpeedBoostSkill : MonoBehaviour
{
    [Header("Speed Boost Settings")]
    [Tooltip("How much to multiply movement speed (2x = 200% speed)")]
    [SerializeField] private float speedMultiplier = 2.5f;

    [Tooltip("How long the speed boost lasts (seconds)")]
    [SerializeField] private float boostDuration = 1.5f;

    [Header("Cooldown Settings")]
    [Tooltip("Time between speed boosts (seconds)")]
    [SerializeField] private float cooldownTime = 5f;

    [Header("Visual Effects")]
    [Tooltip("Particle effect played during speed boost")]
    [SerializeField] private ParticleSystem speedBoostEffect;

    [Tooltip("Color tint applied to player during boost")]
    [SerializeField] private Color boostTintColor = new Color(1f, 1f, 0.5f, 1f);

    [Tooltip("Trail renderer for speed boost visual")]
    [SerializeField] private TrailRenderer speedTrail;

    [Header("Audio")]
    [Tooltip("Sound played when activating speed boost")]
    [SerializeField] private AudioClip boostSound;

    [Tooltip("Sound played when speed boost ends")]
    [SerializeField] private AudioClip boostEndSound;

    [Header("Input")]
    [Tooltip("Key to trigger speed boost")]
    [SerializeField] private KeyCode boostKey = KeyCode.LeftShift;

    // Private variables
    private bool isBoosting = false;
    private bool canBoost = true;
    private float lastBoostTime;
    private float originalSpeed;
    private Coroutine boostCoroutine;

    // Component references
    private PlayerMovement playerMovement;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Color originalColor;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (playerMovement == null)
        {
            Debug.LogError("SpeedBoostSkill requires PlayerMovement component!");
            enabled = false;
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Store original values
        originalSpeed = playerMovement.moveSpeed;
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        // Check for boost input
        if (Input.GetKeyDown(boostKey) && canBoost && !isBoosting)
        {
            StartSpeedBoost();
        }

        // Update cooldown
        if (!canBoost && Time.time - lastBoostTime >= cooldownTime)
        {
            canBoost = true;
        }
    }

    /// <summary>
    /// Initiates the speed boost ability
    /// </summary>
    public void StartSpeedBoost()
    {
        if (!canBoost || isBoosting || playerMovement == null) return;

        isBoosting = true;
        canBoost = false;
        lastBoostTime = Time.time;

        // Store original speed and apply boost
        originalSpeed = playerMovement.moveSpeed;
        playerMovement.SetSpeed(originalSpeed * speedMultiplier);

        // Apply visual effects
        ApplyBoostEffects();

        // Start boost coroutine
        boostCoroutine = StartCoroutine(PerformSpeedBoost());

        Debug.Log($"Speed boost activated! Speed: {originalSpeed} → {playerMovement.moveSpeed} (x{speedMultiplier})");
    }

    /// <summary>
    /// Coroutine that handles the speed boost duration
    /// </summary>
    private IEnumerator PerformSpeedBoost()
    {
        yield return new WaitForSeconds(boostDuration);

        // End the speed boost
        EndSpeedBoost();
    }

    /// <summary>
    /// Ends the speed boost and restores normal speed
    /// </summary>
    private void EndSpeedBoost()
    {
        if (!isBoosting) return;

        isBoosting = false;

        // Restore original speed
        playerMovement.SetSpeed(originalSpeed);

        // Remove visual effects
        RemoveBoostEffects();

        // Play end sound
        if (boostEndSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(boostEndSound);
        }

        Debug.Log($"Speed boost ended! Speed restored to: {originalSpeed}");
    }

    /// <summary>
    /// Applies visual effects for the speed boost
    /// </summary>
    private void ApplyBoostEffects()
    {
        // Play boost sound
        if (boostSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(boostSound);
        }

        // Apply color tint
        if (spriteRenderer != null)
        {
            spriteRenderer.color = boostTintColor;
        }

        // Enable trail effect
        if (speedTrail != null)
        {
            speedTrail.enabled = true;
            speedTrail.Clear();
        }

        // Play particle effect
        if (speedBoostEffect != null)
        {
            speedBoostEffect.Play();
        }
    }

    /// <summary>
    /// Removes visual effects after speed boost ends
    /// </summary>
    private void RemoveBoostEffects()
    {
        // Restore original color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Disable trail effect
        if (speedTrail != null)
        {
            speedTrail.enabled = false;
        }

        // Stop particle effect
        if (speedBoostEffect != null)
        {
            speedBoostEffect.Stop();
        }
    }

    /// <summary>
    /// Checks if the player can currently boost
    /// </summary>
    public bool CanBoost()
    {
        return canBoost && !isBoosting;
    }

    /// <summary>
    /// Gets the remaining cooldown time
    /// </summary>
    public float GetCooldownRemaining()
    {
        if (canBoost) return 0f;
        return cooldownTime - (Time.time - lastBoostTime);
    }

    /// <summary>
    /// Gets the current speed multiplier (1.0 = normal speed)
    /// </summary>
    public float GetCurrentSpeedMultiplier()
    {
        if (!isBoosting) return 1f;
        return speedMultiplier;
    }

    /// <summary>
    /// Forces the speed boost to end early
    /// </summary>
    public void InterruptSpeedBoost()
    {
        if (isBoosting && boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
            EndSpeedBoost();
        }
    }

    /// <summary>
    /// Checks if speed boost is currently active
    /// </summary>
    public bool IsBoosting()
    {
        return isBoosting;
    }

    // Gizmos for debugging in editor
    private void OnDrawGizmosSelected()
    {
        if (playerMovement == null) return;

        // Draw speed comparison
        Vector3 position = transform.position;
        float normalSpeed = playerMovement.moveSpeed / speedMultiplier;

        // Normal speed range (white)
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(position, normalSpeed * 0.5f);

        // Boost speed range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, playerMovement.moveSpeed * 0.5f);
    }
}