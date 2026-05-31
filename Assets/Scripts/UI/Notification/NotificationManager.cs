using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    private static NotificationManager _instance;

    // ĐÂY LÀ PHẦN PHÉP THUẬT
    public static NotificationManager Instance
    {
        get
        {
            // Nếu Manager chưa tồn tại trên Scene, tự động đi tìm và sinh ra nó
            if (_instance == null)
            {
                // Tìm Prefab có tên "GlobalNotificationCanvas" trong thư mục Resources
                GameObject prefab = Resources.Load<GameObject>("GlobalNotificationCanvas");

                if (prefab != null)
                {
                    GameObject obj = Instantiate(prefab);
                    DontDestroyOnLoad(obj); // Bấm nút "Bất tử" ngay khi vừa sinh ra
                    _instance = obj.GetComponent<NotificationManager>();
                }
                else
                {
                    Debug.LogError("Không tìm thấy Prefab 'GlobalNotificationCanvas' trong thư mục Resources!");
                }
            }
            return _instance;
        }
    }
    public GameObject notificationPrefab;
    public Transform notiContainer;
    [Header("Queue Settings")]
    [Tooltip("Thời gian chờ giữa 2 thông báo liên tiếp (giây)")]
    public float delayBetweenMessages = 0.2f;

    // Hàng đợi chứa các tin nhắn đang chờ được hiển thị
    private Queue<string> _messageQueue = new Queue<string>();
    private bool _isProcessingQueue = false;
    private void Awake()
    {
        // Chống đẻ nhánh: Nếu lỡ có 2 cái Manager xuất hiện, hủy cái mới giữ cái cũ
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
    }
    /// <summary>
    /// Thêm tin nhắn vào hàng đợi thay vì hiển thị ngay lập tức
    /// </summary>
    public void ShowNotification(string message)
    {
        _messageQueue.Enqueue(message);

        // Nếu hàng đợi chưa chạy, hãy khởi động nó
        if (!_isProcessingQueue)
        {
            StartCoroutine(ProcessMessageQueue());
        }
    }
    // --- THÊM HÀM NÀY VÀO NOTIFICATION MANAGER ---
    public void ShowCombatPowerChange(int oldCP, int newCP)
    {
        if (notificationPrefab == null || notiContainer == null) return;

        if (oldCP == newCP) return; // Nếu không thay đổi thì không hiện

        // Sinh ra Prefab ngay lập tức
        GameObject notifObj = Instantiate(notificationPrefab, notiContainer);
        notifObj.transform.localScale = Vector3.one;

        NotificationItem itemObj = notifObj.GetComponent<NotificationItem>();
        if (itemObj != null)
        {
            bool isIncrease = newCP > oldCP;

            // Gọi hàm nhảy số vừa viết
            itemObj.SetupRollingNumber("Total Power:", oldCP, newCP, isIncrease);
        }
    }

    /// <summary>
    /// Coroutine xử lý lần lượt từng tin nhắn trong hàng đợi
    /// </summary>
    private IEnumerator ProcessMessageQueue()
    {
        _isProcessingQueue = true;

        while (_messageQueue.Count > 0)
        {
            // Lấy tin nhắn đầu tiên ra khỏi hàng đợi
            string msg = _messageQueue.Dequeue();

            // Sinh ra UI thông báo
            SpawnNotificationUI(msg);

            // Tạm dừng một khoảng thời gian thực (không bị ảnh hưởng bởi Time.timeScale)
            yield return new WaitForSecondsRealtime(delayBetweenMessages);
        }

        // Hàng đợi đã rỗng, dừng xử lý
        _isProcessingQueue = false;
    }

    private void SpawnNotificationUI(string message)
    {
        if (notificationPrefab == null || notiContainer == null) return;

        GameObject notifObj = Instantiate(notificationPrefab, notiContainer);
        notifObj.transform.localScale = Vector3.one;

        NotificationItem itemObj = notifObj.GetComponent<NotificationItem>();
        if (itemObj != null) itemObj.Setup(message);
    }
}
