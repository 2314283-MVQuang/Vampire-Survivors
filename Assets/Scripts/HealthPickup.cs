using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Settings")]
    public float pickupRange = 1.5f; // Tầm hút
    public float moveSpeed = 5f; // Tốc độ hút
    private bool isMovingToPlayer = false;

    void Update()
    {
        // Cơ chế hút về phía người chơi (giống Exp và Coin)
        if (PlayerHealthController.instance != null && PlayerHealthController.instance.gameObject.activeSelf)
        {
            float distance = Vector3.Distance(transform.position, PlayerHealthController.instance.transform.position);

            if (distance < pickupRange)
            {
                isMovingToPlayer = true;
            }

            if (isMovingToPlayer)
            {
                transform.position = Vector3.MoveTowards(transform.position, PlayerHealthController.instance.transform.position, moveSpeed * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            float maxHP = PlayerHealthController.instance.maxHealth;
            float currentHP = PlayerHealthController.instance.currentHealth;

            // Nếu máu đã đầy sẵn thì giữ lại bình máu, không ăn lãng phí
            if (currentHP >= maxHP) return;

            // --- THAY ĐỔI TẠI ĐÂY: HỒI 100% ĐẦY CÂY MÁU ---
            PlayerHealthController.instance.currentHealth = maxHP;
            // ----------------------------------------------

            // Cập nhật UI thanh máu dài ra tối đa
            if (PlayerHealthController.instance.healthSlider != null)
            {
                PlayerHealthController.instance.healthSlider.value = PlayerHealthController.instance.currentHealth;
            }

            // Phát âm thanh hồi máu
            if (SFXManager.instance != null)
            {
                SFXManager.instance.PlaySFXPitched(2);
            }

            // Biến mất bình máu sau khi ăn
            Destroy(gameObject);
        }
    }
}