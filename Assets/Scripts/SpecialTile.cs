using UnityEngine;
using System.Collections.Generic;

public class SpecialTile : MonoBehaviour
{
    private SpriteRenderer sr;
    [SerializeField] private bool isRevealed = false;
    private bool isSearched = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Collider2Dを追加（検出用）
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);
        }
        
        // Default State: Invisible
        Hide();
    }

    public void Hide()
    {
        if (sr != null)
        {
            Color c = Color.white;
            c.a = 0f; // Transparent
            sr.color = c;
            sr.sortingOrder = 5; // Back to floor level
        }
        isRevealed = false;
    }

    public void Reveal()
    {
        if (sr != null)
        {
            Color c = Color.white;
            c.a = 1f; // Visible
            sr.color = c;
            sr.sortingOrder = 101; // Above Fog (Order 100)
        }
        else
        {
            Debug.LogError($"[SpecialTile] SpriteRenderer is missing on {this.name}!");
        }
        isRevealed = true;
    }
    
    // 調査可能かどうか
    public bool CanInteract()
    {
        return isRevealed && !isSearched;
    }

    // 調査処理
    public void Interact(Vector3 playerPosition)
    {
        if (!CanInteract()) return;
        
        isSearched = true;
        
        // ItemDatabaseからドロップテーブルを取得
        List<InteractableShelf.DropItem> dropTable = null;

        if (ItemDatabase.Instance != null)
        {
            dropTable = ItemDatabase.Instance.shelfDropTable;
        }
        
        string resultKey = "nothing";
        string resultName = "何もない";
        
        // SpecialTileWeightに基づいてドロップ抽選
        if (dropTable != null && dropTable.Count > 0)
        {
            int totalWeight = 0;
            foreach (var item in dropTable)
            {
                totalWeight += item.specialTileWeight;
            }
            
            if (totalWeight > 0)
            {
                int roll = Random.Range(0, totalWeight);
                int currentWeight = 0;
                
                foreach (var item in dropTable)
                {
                    currentWeight += item.specialTileWeight;
                    if (roll < currentWeight)
                    {
                        resultKey = item.key;
                        resultName = item.name;
                        break;
                    }
                }
            }
        }
        
        // サウンド再生
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
        
        // アイテム取得処理
        GameUIManager ui = GameUIManager.Instance;
        
        if (resultKey == "report")
        {
            if (InventoryManager.Instance != null) InventoryManager.Instance.AddItem(resultKey);
            if (ui != null)
            {
                ui.ShowMessage("貴重な研究資料を発見した！ (Score +100)", resultKey);
                ui.ShowFloatingItem(resultKey, playerPosition);
            }
        }
        else if (resultKey != "nothing")
        {
            bool check = false;
            if (InventoryManager.Instance != null) 
            {
                check = InventoryManager.Instance.AddItem(resultKey);
            }
            
            if (check)
            {
                if (ui != null)
                {
                    ui.ShowMessage($"貴重な {resultName} を発見した！", resultKey);
                    ui.ShowFloatingItem(resultKey, playerPosition);
                }
            }
        }
        else
        {
            if (ui != null) ui.ShowMessage("特別なものは見つからなかった…", "nothing");
        }
        
        // 消滅アニメーション
        StartCoroutine(FadeOutAndDestroy());
    }

    private System.Collections.IEnumerator FadeOutAndDestroy()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Color startColor = sr != null ? sr.color : Color.white;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (sr != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
