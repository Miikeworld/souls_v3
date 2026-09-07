using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemName = "New Item";
    public Sprite icon;
    [TextArea(3, 6)]
    public string description;

    [Header("Inventory Rules")]
    public bool isStackable = true;
    public int maxStackSize = 99;

    [Header("Optional Effects")]
    public bool isConsumable;
    public float healthRestore;
    public float manaRestore;
    public float staminaRestore;
}
