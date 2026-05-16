using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectile that falls from sky and explodes on ground.
/// Creates AoE damage zone on impact.
/// </summary>
public class RainProjectile : Projectile
{
    public EnemyDamager damager;            // AoE damage zone template
    public float explosionRadius = 1.5f;      // Radius of explosion
    public float fallSpeed = 3f;           // Speed of falling
    public float damage = 10f;              // Damage amount
    public LayerMask whatIsEnemy;           // Enemy layer mask (from RainWeapon)
    public float colliderRadiusMultiplier = 1f;  // Mở rộng collision radius (để enemies dễ hit)
    
    private bool hasExploded = false;       // Did it already explode?
    private float fallCounter;              // Counter for destruction
    
    private GameObject aoeIndicator;        // Visual circle showing AoE area
    private CircleCollider2D indicatorCollider;
    
    [Header("Animation")]
    public Sprite[] animationFrames;        // 8 sprite frames for meteor animation
    private SpriteRenderer spriteRenderer;  // SpriteRenderer for animation
    private int currentFrameIndex = 0;      // Current animation frame
    private float frameTimer = 0f;          // Timer for sprite animation
    public float frameDuration = 0.1f;      // Duration per frame (0.1s)

    void Start()
    {
        // ✅ B1: Scale down to 0.4x (smaller like Vampire Survivors)
        transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        
        // ✅ Setup sprite animation
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        
        // ✅ Meteor flies at angle like rain (diagonal fall)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
            // Random horizontal direction (left/right) + downward
            float horizontalSpeed = Random.Range(-3f, 3f);  // Random left/right speed
            rb.velocity = new Vector2(horizontalSpeed, -fallSpeed);
        }
        
        fallCounter = 0f;
        frameTimer = 0f;
        currentFrameIndex = 0;
        
        // Create visual indicator circle
        CreateAoEIndicator();
    }
    
    /// <summary>
    /// Creates a visual circle showing where AoE will hit
    /// </summary>
    void CreateAoEIndicator()
    {
        aoeIndicator = new GameObject("AoE_Indicator");
        aoeIndicator.transform.SetParent(transform);
        aoeIndicator.transform.localPosition = Vector3.zero;
        
        // Add sprite renderer for circle
        SpriteRenderer spriteRenderer = aoeIndicator.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCircleSprite(explosionRadius);
        spriteRenderer.color = new Color(1f, 0.5f, 0f, 0.4f);  // Orange, semi-transparent
        spriteRenderer.sortingOrder = -1;  // Behind projectile
        
        // Add collider for reference (visual only)
        CircleCollider2D collider = aoeIndicator.AddComponent<CircleCollider2D>();
        collider.radius = explosionRadius;
        collider.enabled = false;  // Just for visualization
        
        indicatorCollider = collider;
    }
    
    /// <summary>
    /// Creates a simple circle sprite for the AoE indicator
    /// </summary>
    Sprite CreateCircleSprite(float radius)
    {
        // Try to use a simple circle sprite if available
        // Otherwise, we'll create a simple visualization
        Texture2D circleTex = new Texture2D((int)(radius * 2), (int)(radius * 2), TextureFormat.RGBA32, false);
        
        for (int x = 0; x < circleTex.width; x++)
        {
            for (int y = 0; y < circleTex.height; y++)
            {
                float dx = x - radius;
                float dy = y - radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Draw circle border (thickness ~0.3)
                if (dist > radius - 0.3f && dist < radius + 0.3f)
                    circleTex.SetPixel(x, y, new Color(1, 0.5f, 0, 0.8f));
                else
                    circleTex.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }
        
        circleTex.Apply();
        return Sprite.Create(circleTex, new Rect(0, 0, circleTex.width, circleTex.height), 
                            new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        // ✅ Sprite animation (8 frames)
        if (animationFrames != null && animationFrames.Length > 0 && spriteRenderer != null)
        {
            frameTimer += Time.deltaTime;
            
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Length;
                spriteRenderer.sprite = animationFrames[currentFrameIndex];
            }
        }
        
        // Check if hit ground or enemies
        fallCounter += Time.deltaTime;
        
        // ✅ Auto-explode if projectile falls too low (y < -180)
        if (!hasExploded && transform.position.y < -180f)
        {
            Debug.Log($"⚠️ Projectile fell below -180, auto-exploding at {transform.position}");
            Explode();
        }
        
        // Auto-explode after 3 seconds if not hit
        if (fallCounter > 3f && !hasExploded)
        {
            Debug.Log($"⚠️ Projectile timeout after 3s, auto-exploding at {transform.position}");
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"🔵 OnTriggerEnter2D: {collision.name}");
        
        // ✅ Explode on hitting ANYTHING (ground, enemies, walls)
        if (!hasExploded)
        {
            Debug.Log($"✅ Hit something! Exploding at {transform.position}");
            Explode();
        }
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // ✅ Keep meteor visible 0.5s during explosion (don't hide immediately)
        
        // ✅ Stop animation when exploding
        frameTimer = 0f;
        currentFrameIndex = 0;
        
        // Remove AoE indicator
        if (aoeIndicator != null)
            Destroy(aoeIndicator);

        // Create explosion effect at current position
        if (damager != null)
        {
            EnemyDamager explosionZone = Instantiate(damager, transform.position, Quaternion.identity);
            explosionZone.transform.localScale = Vector3.one * explosionRadius * 0.6f;  // ✅ Scale down to 0.6x so not too big
            explosionZone.damageAmount = damage;
            explosionZone.damageOverTime = true;  // ✅ Enable continuous damage
            explosionZone.timeBetweenDamage = 0.2f;  // Damage every 0.2s
            explosionZone.shouldKnockBack = true;  // Enable knockback
            explosionZone.lifeTime = 1.5f;  // ✅ Extend explosion time so enemies can trigger
            explosionZone.gameObject.SetActive(true);
            
            // ✅ Ensure Rigidbody2D exists (REQUIRED for trigger collision)
            Rigidbody2D rb = explosionZone.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = explosionZone.gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.isKinematic = true;
            }
            
            // ✅ Ensure CircleCollider2D exists and is trigger
            CircleCollider2D collider = explosionZone.GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = explosionZone.gameObject.AddComponent<CircleCollider2D>();
            }
            collider.isTrigger = true;
            collider.radius = explosionRadius * colliderRadiusMultiplier;  // ✅ Mở rộng radius
            
            // ✅ Check if damager component exists
            EnemyDamager damagerComponent = explosionZone.GetComponent<EnemyDamager>();
            
            
            // ✅ Find all enemies in explosion radius manually
            Collider2D[] enemiesInExplosion = Physics2D.OverlapCircleAll(explosionZone.transform.position, explosionRadius, whatIsEnemy);
            Debug.Log($"🔍 Enemies (layer search): {enemiesInExplosion.Length}");
            
            Debug.Log($"📍 Explosion pos: {explosionZone.transform.position}, radius: {explosionRadius}");
            
            // Fallback: if layer search fails, find by EnemyController component
            if (enemiesInExplosion.Length == 0)
            {
                // Search with small radius first
                Collider2D[] allColliders = Physics2D.OverlapCircleAll(explosionZone.transform.position, explosionRadius);
                Debug.Log($"🔍 Colliders in radius {explosionRadius}: {allColliders.Length}");
                
                // If not enough found, search bigger radius to debug
                if (allColliders.Length < 2)
                {
                    allColliders = Physics2D.OverlapCircleAll(explosionZone.transform.position, 50f);
                    Debug.Log($"🔍 Colliders in radius 50: {allColliders.Length}");
                }
                
                // List all colliders found
                for (int j = 0; j < allColliders.Length; j++)
                {
                    Debug.Log($"  Collider {j}: {allColliders[j].name} at {allColliders[j].transform.position}, distance: {Vector3.Distance(explosionZone.transform.position, allColliders[j].transform.position)}");
                }
                
                List<Collider2D> enemyColliders = new List<Collider2D>();
                
                for (int i = 0; i < allColliders.Length; i++)
                {
                    Debug.Log($"  Collider {i}: {allColliders[i].name}, hasEnemyController: {allColliders[i].GetComponent<EnemyController>() != null}");
                    
                    if (allColliders[i].GetComponent<EnemyController>() != null)
                    {
                        enemyColliders.Add(allColliders[i]);
                        Debug.Log($"  ✅ Found enemy: {allColliders[i].name} at {allColliders[i].transform.position}");
                    }
                }
                
                enemiesInExplosion = enemyColliders.ToArray();
            }
            
            for (int i = 0; i < enemiesInExplosion.Length; i++)
            {
                EnemyController enemy = enemiesInExplosion[i].GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, true);  // Apply damage immediately
                    Debug.Log($"✅ Damaged enemy: {enemy.name}, damage: {damage}");
                }
            }
            
            // Add temporary visual effect for explosion
            CreateExplosionEffect(explosionZone);
            
            // Play explosion sound
            if (SFXManager.instance != null)
                SFXManager.instance.PlaySFXPitched(7);
        }
        else
        {
            Debug.LogWarning("⚠️ Damager is NULL in Explode!");
        }

        // ✅ Delay destruction by 0.5 seconds so player can see meteor during explosion
        Invoke(nameof(DestroyProjectile), 0.5f);
    }
    
    void DestroyProjectile()
    {
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Creates a temporary visual flash for explosion impact
    /// </summary>
    void CreateExplosionEffect(EnemyDamager explosionZone)
    {
        GameObject explosionVFX = new GameObject("Explosion_Flash");
        explosionVFX.transform.position = explosionZone.transform.position;
        explosionVFX.transform.SetParent(explosionZone.transform);
        
        // Add sprite renderer for flash effect
        SpriteRenderer flashRenderer = explosionVFX.AddComponent<SpriteRenderer>();
        
        // Try to get sprite from explosionZone, otherwise use projectile's sprite
        Sprite explosionSprite = explosionZone.GetComponent<SpriteRenderer>()?.sprite;
        if (explosionSprite == null && spriteRenderer != null)
        {
            explosionSprite = spriteRenderer.sprite;
        }
        
        flashRenderer.sprite = explosionSprite;
        flashRenderer.color = new Color(1f, 0.8f, 0f, 0.6f);  // Yellow flash
        flashRenderer.sortingOrder = 0;
        
        // Scale up for visual impact
        explosionVFX.transform.localScale = Vector3.one * 1.3f;
        
        // Fade out effect
        StartCoroutine(FadeOutFlash(flashRenderer, 0.3f));
    }
    
    /// <summary>
    /// Coroutine to fade out explosion flash
    /// </summary>
    IEnumerator FadeOutFlash(SpriteRenderer renderer, float duration)
    {
        float elapsed = 0f;
        Color startColor = renderer.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        if (renderer.gameObject != null)
            Destroy(renderer.gameObject);
    }
}
