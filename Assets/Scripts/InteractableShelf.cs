using UnityEngine;
using System.Collections.Generic;

public class InteractableShelf : MonoBehaviour
{
    private Sprite searchedSprite;
    private bool isSearched = false;
    private SpriteRenderer spriteRenderer;
    
    // Fixed item override (if null or empty, use random drop)
    private string fixedItemKey = null;
    
    [System.Serializable]
    public class DropItem
    {
        public string key;
        public string name;
        public int weight;          // Noraml Shelf Weight
        public int specialTileWeight; // Special Tile Weight
        public int price; // Shop Price
        public int sellPrice; // Sell Price for Trash Can
        [Header("Effect Settings")]
        [Tooltip("Effect Magnitude (Turns, Counts, Areas etc.)")]
        [SerializeField]
        public int effectValue; // Effect Magnitude (Turns, Counts, etc.)
        [TextArea]
        public string description;
        public List<string> usageMessages; // Supports multiple messages
        
        public DropItem(string k, string n, int w, int sw, int p, int sp, int ev, string d = "", params string[] u) 
        { 
            key = k; 
            name = n; 
            weight = w;
            specialTileWeight = sw;
            price = p;
            sellPrice = sp;
            effectValue = ev;
            description = d; 
            usageMessages = new List<string>(u);
            if (usageMessages.Count == 0) usageMessages.Add(""); // Ensure not null/empty
        }
    }

    private List<DropItem> dropTable = new List<DropItem>();
    private int totalWeight;

    public void Setup(Sprite searched)
    {
        searchedSprite = searched;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetFixedItem(string itemKey)
    {
        fixedItemKey = itemKey;
        // Debug.Log($"[InteractableShelf] Fixed item set to: {fixedItemKey}");
    }

    public void SetDropTable(List<DropItem> newTable)
    {
        dropTable = new List<DropItem>(newTable); // Copy list
        CalculateTotalWeight();
    }

    private void CalculateTotalWeight()
    {
        totalWeight = 0;
        foreach (var item in dropTable)
        {
            totalWeight += item.weight;
        }
    }
    
    public void Interact(Vector3 playerPosition)
    {
        if (isSearched) return;
        
        isSearched = true;
        
        // Change sprite to searched state
        if (searchedSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = searchedSprite;
        }
        

        string resultName = "";
        string resultKey = "nothing";

        // Check for fixed item first
        if (!string.IsNullOrEmpty(fixedItemKey))
        {
            resultKey = fixedItemKey;
            // Find name for fixed key (simple lookup or hardcoded for now since key is special)
            if (resultKey == "key") resultName = "鍵";
            else 
            {
                // Fallback search in drop table for name
                var item = dropTable.Find(x => x.key == resultKey);
                if (item != null) resultName = item.name;
                else resultName = "未知のアイテム";
            }
        }
        else
        {
            // Calculate random drop
            if (totalWeight <= 0) CalculateTotalWeight();

            if (totalWeight > 0)
            {
                int roll = Random.Range(0, totalWeight);
                int currentWeight = 0;
        
                foreach (var item in dropTable)
                {
                    currentWeight += item.weight;
                    if (roll < currentWeight)
                    {
                        resultName = item.name;
                        resultKey = item.key;
                        break;
                    }
                }
            }
            else
            {
            }
        }

        // 0. Play Sound Effect via GlobalSoundManager
        if (GlobalSoundManager.Instance != null)
        {
            if (resultKey == "nothing")
            {
                GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.searchSE);
            }
            else
            {
                GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.getSE);
            }
        }
        else
        {
        }

        // 1. Handle Item Acquisition & Special Effects
        GameUIManager ui = GameUIManager.Instance;

        if (resultKey == "map")
        {
             // Map: Special Sequence with Wait
             StartCoroutine(ProcessMapAcquisition(resultKey, playerPosition, ui));
             return; // Coroutine handles the rest (message, float item)
        }
        else if (resultKey == "report")
        {
             // Report: Immediate Score Effect (Handled in InventoryManager)
             if (InventoryManager.Instance != null) InventoryManager.Instance.AddItem(resultKey);
             
             if (ui != null) 
             {
                 ui.ShowMessage("レポートを見つけた！ (Score +100)", resultKey);
                 ui.ShowFloatingItem(resultKey, playerPosition);
             }
        }
        else if (InventoryManager.Instance != null && resultKey != "nothing")
        {
             // Normal Item: Add to Inventory
             bool check = InventoryManager.Instance.AddItem(resultKey);
             
             if (check)
             {
                 // Success: Show Message and Effect
                 if (ui != null) ui.ShowMessage($"{resultName} を入手した", resultKey);
                 
                 // Display Result (Animation)
                 if (ui != null)
                 {
                    ui.ShowFloatingItem(resultKey, playerPosition);
                 }
             }
             else
             {
                 // Failed (Full): Message already shown by InventoryManager
                 // Do not show "Obtained" message
             }
        }else if (InventoryManager.Instance == null && resultKey != "nothing")
        {
        }
		// Handle "nothing" case explicitly or just skip floating item
		else if (resultKey == "nothing" && ui != null)
		{
			ui.ShowMessage("何もなかった", "nothing");
		}
    }

    private System.Collections.IEnumerator ProcessMapAcquisition(string key, Vector3 playerPos, GameUIManager ui)
    {
        // 1. Disable Input
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) pm.SetInputEnabled(false);

        // 2. Show Acquisition Message & Icon (Standard feedback)
        if (ui != null) 
        {
            ui.ShowMessage("マップを入手した", key);
            ui.ShowFloatingItem(key, playerPos);
        }

        // 3. Wait 0.5s (Simulate looking at map)
        yield return new WaitForSeconds(0.5f);

        // 4. Trigger Effect (Reveal Random Areas - Configurable)
        DungeonGeneratorV2 dungeon = FindFirstObjectByType<DungeonGeneratorV2>();
        if (dungeon != null)
        {
            int revealCount = 3; // Default
            if (ItemDatabase.Instance != null)
            {
                revealCount = ItemDatabase.Instance.GetItemEffectValue(key);
                if (revealCount <= 0) revealCount = 3;
            }

            dungeon.RevealRandomAreas(revealCount);
            if (ui != null) ui.ShowMessage("周辺の地図が書き込まれた！", key);
        }

        // 5. Re-enable Input
        if (pm != null) pm.SetInputEnabled(true);

        
        // Play sound effect if needed (future implementation)
    }
    
    public bool IsSearched()
    {
        return isSearched;
    }
}
