using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject boxPrefab;         // Kéo Prefab Cái Hộp (Crate) vào đây
    public float spawnInterval = 40f;    // Thay đổi từ 7 giây thành 20 giây mặc định
    
    [Header("Distance From Player")]
    public float minSpawnRadius = 6f;    // Khoảng cách tối thiểu
    public float maxSpawnRadius = 12f;   // Khoảng cách tối đa

    private float spawnTimer;

    void Start()
    {
        // Đặt thời gian đếm ngược ban đầu là 20 giây
        spawnTimer = spawnInterval;
    }

    void Update()
    {
        // Chỉ đẻ hộp khi người chơi còn sống
        if (PlayerHealthController.instance != null && PlayerHealthController.instance.gameObject.activeSelf)
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0)
            {
                // VÒNG LẶP CHẠY 3 LẦN: Sinh ra đúng 3 hộp cùng một lúc tại các vị trí ngẫu nhiên khác nhau
                for (int i = 0; i < 3; i++)
                {
                    SpawnBoxAroundPlayer();
                }

                spawnTimer = spawnInterval; // Đặt lại đồng hồ 20 giây cho lần tiếp theo
            }
        }
    }

    void SpawnBoxAroundPlayer()
    {
        if (boxPrefab == null)
        {
            Debug.LogWarning("⚠️ BoxSpawner chưa được kéo thả Box Prefab kìa bạn ơi!");
            return;
        }

        // 1. Lấy vị trí hiện tại của nhân vật
        Vector3 playerPos = PlayerHealthController.instance.transform.position;

        // 2. Tính toán một tọa độ ngẫu nhiên dạng vòng tròn xung quanh nhân vật
        Vector2 randomDirection = Random.insideUnitCircle.normalized; // Chọn một hướng ngẫu nhiên (360 độ)
        float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius); // Chọn khoảng cách ngẫu nhiên từ 6 đến 12 mét
        
        Vector3 spawnPosition = playerPos + new Vector3(randomDirection.x * randomDistance, randomDirection.y * randomDistance, 0f);

        // 3. Tiến hành tạo ra cái hộp tại tọa độ đó
        Instantiate(boxPrefab, spawnPosition, Quaternion.identity);
    }
}