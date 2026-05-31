using System.Collections;
using UnityEngine;

public class SummonUnit : MonoBehaviour
{
    private UnitController summonUnit;
    public BattleHUD summonUnitHUD;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        summonUnit = GetComponent<UnitController>();
    }
    public void Initialize(float duration)
    {
        StartCoroutine(StartedTimelife(duration));
    }

    private IEnumerator StartedTimelife(float duration)
    {
        float elapsedTime = 0f;

        // Coroutine sẽ chạy cho đến khi hết thời gian
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float remainingTime = duration - elapsedTime;

            // RA LỆNH CHO HUD CẬP NHẬT THANH THỜI GIAN MỖI FRAME
            if (summonUnitHUD != null)
            {
                summonUnitHUD.UpdateDurationBar(remainingTime, duration);
            }
            yield return null;
        }
        // Hết giờ, ra lệnh cho unit chết
        if (summonUnit != null)
        {
            // (Tùy chọn) Ẩn thanh HUD đi trước khi chết
            if (summonUnitHUD != null) summonUnitHUD.HideDurationBar();
            summonUnit.Delete();
        }
    }
}
