using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameDataService : MonoBehaviour
{
    public static GameDataService Instance { get; private set; }
    [SerializeField] private List<UnitSO> allUnits;
    [SerializeField] private List<ItemSO> allItems;
    private Dictionary<string, UnitSO> unitLookup = new Dictionary<string, UnitSO>();
    private Dictionary<string, ItemSO> itemLookup = new Dictionary<string, ItemSO>();

    private void Awake()
    {
        if(Instance == null) {  
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        foreach (var unit in allUnits)
        {
            unitLookup.Add(unit.unitID, unit);
        }
        foreach (var item in allItems)
        {
            itemLookup.Add(item.itemID, item);
        }
    }

    public UnitSO GetUnitSO(string unitID) => unitLookup.ContainsKey(unitID) ? unitLookup[unitID] : null;
    public ItemSO GetItemSO(string itemID) => itemLookup.ContainsKey(itemID) ? itemLookup[itemID] : null;
}
