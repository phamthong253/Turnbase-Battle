using System;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public enum UnitState
    {
        IDLE,
        BUSY,
        SLIDE
    }
    public BattleHUD battleHUD;
    public UnitState unitState;
    public UnitSO unitSO;
    public UnitBase unitBase;
    public int currentHealth, currentMana, currentDamage, currentArmor;

    private Vector3 otherPos;
    private Action onMoveComplete;
    public void SetHUD(BattleHUD battleHUD)
    {
        this.battleHUD = battleHUD;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unitState = UnitState.IDLE;
        unitBase = GetComponent<UnitBase>();
        currentHealth = unitSO.hp;
        currentMana = unitSO.mp;
        currentDamage = unitSO.damage;
        currentArmor = unitSO.armor;
    }

    // Update is called once per frame
    void Update()
    {
        switch (unitState) { 
            case UnitState.IDLE:
                unitBase.PlayAnimation("idle");
                break;
            case UnitState.BUSY:
                break;
            case UnitState.SLIDE:
                float moveSpeed = 5f;
                // giữ giá trị Z cố định
                Vector3 moveDirection = (otherPos - transform.position).normalized;
                moveDirection.z = -1; // Giữ nguyên Z
                transform.position += (otherPos - transform.position) * moveSpeed * Time.deltaTime;
                if (Vector3.Distance(transform.position, otherPos) < 0.1f)
                {
                    transform.position = otherPos;
                    onMoveComplete();
                }
                break;
        }
    }

    public void AssignOtherPosition(Transform otherSpawner)
    {
        otherPos = otherSpawner.position;
    }
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    //unitController la unitController cua doi phuong
    public void AttackTurn(UnitController unitController, Action onMoveComplete)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = unitController.GetPosition() - (GetPosition() - unitController.GetPosition()).normalized * 2f;
        MoveToPosition(targetPos, () =>
        {
            unitState = UnitState.BUSY;
            unitBase.PlayForceAnimation("attack", () =>
            {
                battleHUD.DamageText(Math.Max(0, currentDamage - unitController.currentArmor), unitController);
                unitController.currentHealth -= Math.Max(0, currentDamage - unitController.currentArmor);
                unitController.currentMana += Math.Max(0, currentDamage - unitController.currentArmor) *2;
                Debug.Log(Math.Max(0, currentDamage - unitController.currentArmor ) + "+ mana "  + "mana hien tai: " + currentMana);
                unitController.battleHUD.UpdateHUD(unitController);
                unitController.unitBase.PlayAnimation("hit");
                transform.Rotate(0f, 180f, 0f, Space.Self);
                MoveToPosition(startPos, () => {
                    unitState = UnitState.IDLE;
                    unitBase.PlayAnimation("move");
                    transform.Rotate(0f, 180f, 0f, Space.Self);
                    onMoveComplete();
                });
            });

        });
    }
    public void MoveToPosition(Vector3 otherPos, Action onMoveComplete)
    {
        unitBase.PlayAnimation("move");
        this.otherPos = otherPos;
        this.onMoveComplete = onMoveComplete;
        unitState = UnitState.SLIDE;
    }
    public void Delete()
    {
        Destroy(this.gameObject, 1);
    }
}
