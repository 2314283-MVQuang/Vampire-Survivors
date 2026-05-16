using UnityEngine;

public class DragonBossController : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D theRB;
    public SpriteRenderer bossSprite;
    
    [Header("Stats")]
    public float moveSpeed = 3f;
    public float attackRange = 7f;
    
    [Header("Attack Settings")]
    public GameObject firePrefab;
    public Transform fireSpawnPoint;
    public float telegraphTime = 1f; // Thời gian khựng lại nhắm bắn
    public float cooldownTime = 3f;  // Nghỉ giữa các lần phun lửa
    
    private Transform player;
    private enum State { Moving, Telegraphing, Cooldown }
    private State currentState;
    private float timer;

    // --- BIẾN MỚI: Dùng để chốt chết hướng ngắm ---
    private Vector2 lockedAimDirection; 

    void Start()
    {
        if (PlayerHealthController.instance != null)
            player = PlayerHealthController.instance.transform;
            
        currentState = State.Moving;
    }

    void Update()
    {
        if (player == null || !player.gameObject.activeSelf) 
        {
            theRB.velocity = Vector2.zero;
            return;
        }

        switch (currentState)
        {
            case State.Moving:
                MoveToPlayer();
                break;
            case State.Telegraphing:
                HandleTelegraphing();
                break;
            case State.Cooldown:
                HandleCooldown();
                break;
        }
    }

    private void MoveToPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        theRB.velocity = direction * moveSpeed;

        // Xoay mặt rồng cho đúng
        if (direction.x > 0)
            transform.localScale = new Vector3(1f, 1f, 1f);  
        else if (direction.x < 0)
            transform.localScale = new Vector3(-1f, 1f, 1f); 

        // Nếu vào tầm thì dừng lại chuẩn bị khạc lửa
        if (Vector3.Distance(transform.position, player.position) < attackRange)
        {
            theRB.velocity = Vector2.zero;
            currentState = State.Telegraphing;
            timer = telegraphTime;

            // --- BÍ QUYẾT LÀ ĐÂY: Chốt luôn hướng ngắm ngay khi vừa khựng lại ---
            if (fireSpawnPoint != null)
            {
                lockedAimDirection = (player.position - fireSpawnPoint.position).normalized;
            }
        }
    }

    private void HandleTelegraphing()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            SpawnFireBreath();
            currentState = State.Cooldown;
            timer = cooldownTime;
        }
    }

    private void SpawnFireBreath()
    {
        if (firePrefab != null && fireSpawnPoint != null)
        {
            // --- Dùng góc đã chốt từ 1 giây trước để bắn, kệ người chơi chạy đi đâu ---
            float angle = Mathf.Atan2(lockedAimDirection.y, lockedAimDirection.x) * Mathf.Rad2Deg;
            Instantiate(firePrefab, fireSpawnPoint.position, Quaternion.Euler(0, 0, angle));
        }
    }

    private void HandleCooldown()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            currentState = State.Moving;
        }
    }
}