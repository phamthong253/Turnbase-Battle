using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class UnitBase : MonoBehaviour
{
    private Animator animator;
    private Action onActionComplete;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
        yield return new WaitForSeconds(0.5f);
        onComplete?.Invoke();
    }
}
