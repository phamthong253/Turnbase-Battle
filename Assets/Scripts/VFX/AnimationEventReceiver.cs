using System;
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    public event Action OnAnimationActionTrigger;
    public void OnAnimationTrigger()
    {
        // Ghi log để xác nhận đã nhận tín hiệu từ animation
        Debug.Log("<color=orange>[AnimationEventReceiver]</color> Đã nhận tín hiệu từ Animation. Bắt đầu phát sự kiện!");
        Debug.Log("Event Triggered");
        // Gọi sự kiện khi có trigger từ animation
        OnAnimationActionTrigger?.Invoke();
    }
}
