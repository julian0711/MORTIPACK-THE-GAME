using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public void ToggleInventory()
    {
        // Toggle Inventory Screen via GameUIManager
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ToggleInventoryScreen();
        }
        else
        {
            Debug.LogWarning("[InventoryToggle] GameUIManager Instance not found!");
        }
    }
}
