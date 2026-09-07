using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<Inventory>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("Inventory");
                    _instance = go.AddComponent<Inventory>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    private static Inventory _instance;

    [Header("Inventory")]
    public List<ItemStack> items = new List<ItemStack>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Try to add the item. Returns true if all were added.</summary>
    public bool Add(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (item.isStackable)
        {
            int remaining = amount;

            // Fill existing stacks first
            foreach (var stack in items)
            {
                if (stack.item == item)
                {
                    int space = stack.item.maxStackSize - stack.count;
                    int toAdd = Mathf.Min(space, remaining);
                    stack.count += toAdd;
                    remaining -= toAdd;

                    if (remaining <= 0)
                    {
                        Debug.Log($"Added {amount}x {item.itemName} to inventory.");
                        return true;
                    }
                }
            }

            // Add new stacks for the remainder
            while (remaining > 0)
            {
                int toAdd = Mathf.Min(item.maxStackSize, remaining);
                items.Add(new ItemStack(item, toAdd));
                remaining -= toAdd;
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
                items.Add(new ItemStack(item, 1));
        }

        Debug.Log($"Picked up {amount}x {item.itemName}.");
        return true;
    }

    public bool Remove(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || !Has(item, amount)) return false;

        int remaining = amount;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].item == item)
            {
                int toRemove = Mathf.Min(items[i].count, remaining);
                items[i].count -= toRemove;
                remaining -= toRemove;

                if (items[i].count <= 0)
                    items.RemoveAt(i);

                if (remaining <= 0)
                    return true;
            }
        }

        return true;
    }

    public bool Has(ItemData item, int amount = 1)
    {
        return Count(item) >= amount;
    }

    public int Count(ItemData item)
    {
        if (item == null) return 0;
        int total = 0;
        foreach (var stack in items)
            if (stack.item == item)
                total += stack.count;
        return total;
    }

    public void Clear()
    {
        items.Clear();
    }
}
