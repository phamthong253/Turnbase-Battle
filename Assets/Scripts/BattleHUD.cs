using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour
{
    private static BattleHUD instance;
    public TextMeshProUGUI unitName;
    public TextMeshProUGUI unitLevel;
    public GameObject popupDamagePrefab;
    public Slider hpSlider, mpSlider;
    private Transform target;
    public bool isEnemyHUD = false;

    //private void Awake()
    //{
    //    if (instance == null)
    //    {
    //        instance = this;
    //        DontDestroyOnLoad(gameObject);
    //    }
    //    else
    //    {
    //        Destroy(gameObject);
    //    }
    //}
    public IEnumerator StartHUD(BattleHUD battleHUD, UnitController unitController)
    {
        yield return new WaitUntil(() => unitController.currentHealth != 0);
        if (!isEnemyHUD)
        {
            unitName.text = unitController.unitSO.name;
            unitLevel.text = "Lv " + unitController.unitSO.level;
            hpSlider.maxValue = unitController.unitSO.hp;
            mpSlider.maxValue = unitController.unitSO.mp;
        }
        else
        {
            mpSlider.enabled = false;
        }
        unitLevel.text = "Lv " + unitController.unitSO.level;
        hpSlider.maxValue = unitController.unitSO.hp;
        UpdateHUD(unitController);
        AssignHUD(battleHUD, unitController);
    }
    public void UpdateHUD(UnitController unitController)
    {
        hpSlider.value = unitController.currentHealth;
        mpSlider.value = unitController.currentMana;
    }
    public void AssignHUD(BattleHUD battleHUD, UnitController unitController)
    {
        unitController.SetHUD(battleHUD);
    }
    public void DamageText( int damage, UnitController unitController) {
        PopupDamage popup = popupDamagePrefab.GetComponent<PopupDamage>();
        popup.CreatePopup(unitController.transform.position + new Vector3(0f, 5f, 0f), damage, Color.blue);
    }
    
    public void SetTarget(Transform enemyTransform)
    {
        target = enemyTransform;
    }
    void Update()
    {
        if (target != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position + new Vector3(0, 2.5f, 0));
            transform.position = screenPos;
        }
    }
}
