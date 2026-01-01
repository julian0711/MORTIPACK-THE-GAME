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

    [Header("Debug")]
    [SerializeField] private bool debugStartWithKey = false;

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
            Debug.Log("[InventoryManager] Debug: Added initial key.");
        }
    }

    public void AddItem(string key)
    {
        if (string.IsNullOrEmpty(key) || key == "nothing") return;

        if (key == "report")
        {
            if (GameUIManager.Instance != null) GameUIManager.Instance.AddScore(100);
            Debug.Log("[InventoryManager] Found Report. Score +100.");
            return; 
        }

        if (inventory.ContainsKey(key))
        {
            inventory[key]++;
        }
        else
        {
            inventory[key] = 1;
        }

        Debug.Log($"[InventoryManager] Added {key}. Total: {inventory[key]}");
        if (GameUIManager.Instance != null) GameUIManager.Instance.AddScore(10);
        OnInventoryChanged?.Invoke();
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
        Debug.Log("[InventoryManager] Inventory Cleared.");
        OnInventoryChanged?.Invoke();
    }
}
