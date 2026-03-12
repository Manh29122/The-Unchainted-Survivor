using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingProjectileSkill : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int quantity = 3;
    [Tooltip("Radians per second each projectile orbits at")]
    [SerializeField] private float orbitVelocity = 2f; // radians per second
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float projectileDamage = 50f;
    [Tooltip("Starting offset angle for the first projectile, in degrees")]
    [SerializeField] private float startAngle = 0f; // degrees
    [SerializeField] private int projectilePoolPreload = 8;

    [Header("Skill Settings")]
    [SerializeField] private float cooldownTime = 5f;
    [SerializeField] private float skillDuration = 10f;
    [SerializeField] private KeyCode activateKey = KeyCode.E;

    [Header("Effects")]
    [SerializeField] private AudioClip activateSound;

    private bool isActive = false;
    private bool canActivate = true;
    private float lastActivateTime;
    private List<GameObject> projectiles = new List<GameObject>();
    private List<float> angles = new List<float>();
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(activateKey) && canActivate && !isActive)
        {
            ActivateSkill();
        }

        if (!canActivate && Time.time - lastActivateTime >= cooldownTime)
        {
            canActivate = true;
        }

        if (isActive)
        {
            UpdateProjectiles();
        }
    }

    private void ActivateSkill()
    {
        isActive = true;
        canActivate = false;
        lastActivateTime = Time.time;

        SpawnProjectiles();

        if (activateSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activateSound);
        }

        StartCoroutine(DeactivateAfterDuration());

        Debug.Log("Orbiting Projectile Skill Activated!");
    }

    private void SpawnProjectiles()
    {
        for (int i = 0; i < quantity; i++)
        {
            if (projectilePrefab != null)
            {
                GameObject proj = PoolManager.Spawn(projectilePrefab, transform.position, Quaternion.identity, projectilePoolPreload);
                projectiles.Add(proj);
                float angleDeg = startAngle + ((360f / quantity) * i);
                angles.Add(angleDeg * Mathf.Deg2Rad); // even spacing with offset

                // Set up damage component
                ProjectileDamage pd = proj.GetComponent<ProjectileDamage>();
                if (pd == null)
                {
                    pd = proj.AddComponent<ProjectileDamage>();
                }
                pd.SetDamage(projectileDamage);
                pd.SetOwner(this);
            }
        }
    }

    private void UpdateProjectiles()
    {
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i] != null)
            {
                // Use per-projectile orbit velocity if available
                float projOrbitVelocity = orbitVelocity;
                OrbitingProjectileData opd = projectiles[i].GetComponent<OrbitingProjectileData>();
                if (opd != null)
                {
                    projOrbitVelocity = opd.orbitVelocity;
                }
                // Update orbit angle
                angles[i] += projOrbitVelocity * Time.deltaTime;
                Vector3 offset = new Vector3(Mathf.Cos(angles[i]), Mathf.Sin(angles[i]), 0) * orbitRadius;
                projectiles[i].transform.position = transform.position + offset;

                // Rotate around Z axis
                projectiles[i].transform.Rotate(0, 0, projOrbitVelocity * Mathf.Rad2Deg * Time.deltaTime, Space.Self);
            }
        }
    }

    private void DeactivateSkill()
    {
        isActive = false;
        foreach (GameObject proj in projectiles)
        {
            if (proj != null)
            {
                if (!PoolManager.Return(proj))
                {
                    Destroy(proj);
                }
            }
        }
        projectiles.Clear();
        angles.Clear();
        Debug.Log("Orbiting Projectile Skill Deactivated!");
    }

    private IEnumerator DeactivateAfterDuration()
    {
        yield return new WaitForSeconds(skillDuration);
        DeactivateSkill();
    }

    public void SetVelocity(float velocity)
    {
        orbitVelocity = Mathf.Max(0, velocity);
        Debug.Log($"[OrbitingProjectileSkill] Velocity set to: {orbitVelocity}");
    }

    public void SetDamage(float damage)
    {
        projectileDamage = Mathf.Max(0, damage);
        // Update existing projectiles
        foreach (GameObject proj in projectiles)
        {
            if (proj != null)
            {
                ProjectileDamage pd = proj.GetComponent<ProjectileDamage>();
                if (pd != null)
                {
                    pd.SetDamage(projectileDamage);
                }
            }
        }
        Debug.Log($"[OrbitingProjectileSkill] Damage set to: {projectileDamage}");
    }

    public void SetDamageByPercentage(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 1000f);
        projectileDamage *= percent / 100f;
        // Update existing
        foreach (GameObject proj in projectiles)
        {
            if (proj != null)
            {
                ProjectileDamage pd = proj.GetComponent<ProjectileDamage>();
                if (pd != null)
                {
                    pd.SetDamage(projectileDamage);
                }
            }
        }
        Debug.Log($"[OrbitingProjectileSkill] Damage adjusted by {percent}% of current value. New damage: {projectileDamage}");
    }

    public void SetQuantity(int qty)
    {
        quantity = Mathf.Max(1, qty);
        if (isActive)
        {
            // Respawn with new quantity
            DeactivateSkill();
            SpawnProjectiles();
        }
        Debug.Log($"[OrbitingProjectileSkill] Quantity set to: {quantity}");
    }

    // Add percentage setters if needed

    // Set custom direction for each orbiting projectile
    // directions: list of angles in degrees, length must match quantity
    public void SetOrbitDirections(List<float> directions)
    {
        if (directions == null || directions.Count != quantity)
        {
            Debug.LogWarning("[OrbitingProjectileSkill] Directions list must match quantity.");
            return;
        }
        for (int i = 0; i < quantity; i++)
        {
            angles[i] = directions[i] * Mathf.Deg2Rad;
        }
        Debug.Log("[OrbitingProjectileSkill] Orbit directions updated.");
    }

    // Set individual orbit velocity for each projectile by percentage
    // percentages: list of percentage values, length must match quantity
    public void SetIndividualOrbitVelocityByPercentage(List<float> percentages)
    {
        if (percentages == null || percentages.Count != quantity)
        {
            Debug.LogWarning("[OrbitingProjectileSkill] Percentage list must match quantity.");
            return;
        }
        for (int i = 0; i < projectiles.Count; i++)
        {
            float newVelocity = orbitVelocity * Mathf.Clamp(percentages[i], 0f, 1000f) / 100f;
            // Store per-projectile velocity (add a new List<float> if needed)
            if (projectiles[i] != null)
            {
                OrbitingProjectileData opd = projectiles[i].GetComponent<OrbitingProjectileData>();
                if (opd == null)
                {
                    opd = projectiles[i].AddComponent<OrbitingProjectileData>();
                }
                opd.orbitVelocity = newVelocity;
            }
        }
        Debug.Log("[OrbitingProjectileSkill] Individual orbit velocities updated by percentage.");
    }

    // Set individual damage for each projectile by percentage
    // percentages: list of percentage values, length must match quantity
    public void SetIndividualDamageByPercentage(List<float> percentages)
    {
        if (percentages == null || percentages.Count != quantity)
        {
            Debug.LogWarning("[OrbitingProjectileSkill] Percentage list must match quantity.");
            return;
        }
        for (int i = 0; i < projectiles.Count; i++)
        {
            float newDamage = projectileDamage * Mathf.Clamp(percentages[i], 0f, 1000f) / 100f;
            if (projectiles[i] != null)
            {
                ProjectileDamage pd = projectiles[i].GetComponent<ProjectileDamage>();
                if (pd != null)
                {
                    pd.SetDamage(newDamage);
                }
            }
        }
        Debug.Log("[OrbitingProjectileSkill] Individual projectile damages updated by percentage.");
    }
}