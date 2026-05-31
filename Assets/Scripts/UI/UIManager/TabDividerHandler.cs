using UnityEngine;

public class TabDividerHandler : MonoBehaviour
{
    public GameObject dividerObj; // Kéo object Divider vào đây

    void Start()
    {
        UpdateDivider();
    }

    // Gọi hàm này khi khởi tạo danh sách
    public void UpdateDivider()
    {
        // Kiểm tra xem nút này có phải là con cuối cùng trong danh sách không
        bool isLast = transform.GetSiblingIndex() == transform.parent.childCount - 1;

        // Nếu là cuối cùng thì tắt divider đi
        if (dividerObj != null)
        {
            dividerObj.SetActive(!isLast);
        }
    }
}