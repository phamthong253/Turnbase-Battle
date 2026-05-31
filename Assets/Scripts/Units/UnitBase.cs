using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

public class UnitBase : MonoBehaviour
{
    private Animator animator;
    private Action onActionComplete;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (animator == null)
        {
            // Thay GetComponent bằng GetComponentInChildren
            // Nó sẽ tìm trên chính GameObject này, sau đó tìm xuống tất cả các con của nó.
            animator = GetComponent<Animator>();
        }
    }
    void Start()
    {
        animator = GetComponent<Animator>();

    }
    public void OnActionComplete()
    {
        if (onActionComplete != null)
        {
            onActionComplete();
        }
    }
    public void PlayAnimation(string AnimationName)
    {
        if (animator == null)
        {
            return; // Không làm gì cả và thoát khỏi hàm để không bị lỗi
        }
        animator.SetTrigger(AnimationName);
    }
    public void PlayForceAnimation(string AnimationName, Action onActionComplete)
    {
        this.onActionComplete = onActionComplete;
        PlayAnimation(AnimationName);
        StartCoroutine(WaitForAnimation(AnimationName, onActionComplete));
    }
    private IEnumerator WaitForAnimation(string AnimationName, Action onComplete)
    {
        yield return new UnityEngine.YieldInstruction(); // Đợi một frame để đảm bảo animation đã bắt đầu
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        while (stateInfo.IsName(AnimationName) && stateInfo.normalizedTime < 1.0f)
        {
            yield return null; // Chờ cho đến khi animation kết thúc
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }
        onComplete?.Invoke();
    }
    public float GetAnimationDuration(string AnimationName)
    {
        return 0.5f;
    }
}
