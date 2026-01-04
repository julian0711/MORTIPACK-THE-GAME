using UnityEngine;
using System.Collections.Generic;
using System;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // Item Key -> Count
    private Dictionary<string, int> inventory = new Dictionary<string, int>();

    // Event when inventory changes
    public event Action OnInventoryChanged;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugStartWithKey = false;
    [SerializeField] private bool debugStartWithAllItems = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (debugStartWithKey)
        {
            AddItem("key");
        }

        if (debugStartWithAllItems)
        {
            if (ItemDatabase.Instance != null)
            {
                foreach (var itemData in ItemDatabase.Instance.shelfDropTable)
                {
                    if (itemData.key != "nothing" && itemData.key != "report") // 'nothing' is empty, 'report' is score
                    {
                        // Add 1 of each item
                        AddItem(itemData.key);
                    }
                }
            }
        }
    }

    [SerializeField] private int maxItemCount = 9;

    public bool AddItem(string key)
    {
        if (string.IsNullOrEmpty(key) || key == "nothing") return false;

        if (key == "report")
        {
            if (GameUIManager.Instance != null) GameUIManager.Instance.AddScore(100);
            return true; // Treated as success (action consumed)
        }

        // Check Current Count
        int currentCount = 0;
        if (inventory.ContainsKey(key))
        {
            currentCount = inventory[key];
        }

        // Check Limit (Skip if full)
        if (currentCount >= maxItemCount)
        {
            if (GameUIManager.Instance != null)
            {
                // Get Item Name
                string itemName = key;
                if (ItemDatabase.Instance != null)
                {
                    itemName = ItemDatabase.Instance.GetItemName(key);
                }
                
                // Show Warning Message with Item Name
                GameUIManager.Instance.ShowMessage($"{itemName}はこれ以上は持てない！", key);
            }
            Debug.Log($"[Inventory] Cannot add {key}. Max limit ({maxItemCount}) reached.");
            return false;
        }

        if (inventory.ContainsKey(key))
        {
            inventory[key]++;
        }
        else
        {
            inventory[key] = 1;
        }

        if (GameUIManager.Instance != null) GameUIManager.Instance.AddScore(10);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public Dictionary<string, int> GetInventory()
    {
        return new Dictionary<string, int>(inventory);
    }

    public bool HasItem(string key)
    {
        return inventory.ContainsKey(key) && inventory[key] > 0;
    }

    public void RemoveItem(string key)
    {
        if (inventory.ContainsKey(key))
        {
            inventory[key]--;
            if (inventory[key] <= 0)
            {
                inventory.Remove(key);
            }
            OnInventoryChanged?.Invoke();
        }
    }

    public void ClearInventory()
    {
        inventory.Clear();
        OnInventoryChanged?.Invoke();
    }
}
