using UnityEngine;
using UnityEngine.EventSystems;

public class ToopTipTrigger : MonoBehaviour
{
    private UnitSO unitData;
    private SkillSO skillData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SetUnitData(UnitSO unit, SkillSO skillUnit)
    {
        this.unitData = unit;
        this.skillData = skillUnit;
    }

    public void MousePointEnter(PointerEventData evenData) {
        if (unitData != null && skillData != null)
        {
            ToolTipManager.instance.ShowToolTip(unitData, skillData);
        }
    }
    public void MousePointExit(PointerEventData evenData)
    {
        ToolTipManager.instance.HideToolTip();
    }
}
