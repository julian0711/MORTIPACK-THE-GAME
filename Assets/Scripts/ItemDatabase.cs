using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    // Singleton Instance
    public static ItemDatabase Instance { get; private set; }

    [Header("Drop List Settings")]
    [Header("ドロップリスト設定 (Drop Rates)")]
    // Tooltip: weight=通常棚の出現率, specialTileWeight=SpecialTileでの出現率
    [SerializeField]
    public List<InteractableShelf.DropItem> shelfDropTable = new List<InteractableShelf.DropItem>();

    [Header("ショップ設定")]
    [Header("ショップ入れ替え価格")]
    [SerializeField]
    public int shopResetPrice = 5000;

    [System.Serializable]
    public class VendorMessageConfig
    {
        [Header("入店挨拶")]
        public string welcome = "いらっしゃい～！";

        [Header("商品インタラクション")]
        public string checkPrice = "{0}Ptです！ご購入は長押しで！";
        public string checkReroll = "{0}Ptで商品を入れ替えますか？？(長押し)";

        [Header("購入・入替成功")]
        public string thanksBuy = "ありがとうございました～！";
        public string thanksReroll = "商品を入れ替えました！";

        [Header("失敗（ポイント不足）")]
        public string noPointItem = "おや、Ptが足りないね ({0}Pt必要)";
        public string noPointReroll = "ポイントが足りないようだね… (必要: {0}Pt)";
    }
    
    [Header("店員のセリフ設定")]
    [SerializeField]
    public VendorMessageConfig vendorMessageConfig = new VendorMessageConfig();

    // エディタでコンポーネントがリセットされた時にデフォルト値を設定
    private void Reset()
    {
        shelfDropTable = GetDefaultDropTable();
    }

    // Shared method to define defaults in one place
    private static List<InteractableShelf.DropItem> GetDefaultDropTable()
    {
            // DropItem(key, name, weight, specialTileWeight, price, sellPrice, effectValue, description, usageMessages)
        return new List<InteractableShelf.DropItem>()
        {
            // Nothing
            new InteractableShelf.DropItem("nothing", "何もない", 1200, 10, 0, 0, 0, "", ""),
            
            // Common Items
            new InteractableShelf.DropItem("report", "研究資料", 100, 10, 0, 0, 0, "", "これは特ダネだ！(Score+100)"),
            
            // Valuable Items
            new InteractableShelf.DropItem("radar", "探知機", 100, 15, 3000, 1500, 10, "隠されたものを見つける装置。", "見えない物が見えるようになったぞ！"), // 10 Turns
            new InteractableShelf.DropItem("warpcoin", "ワープコイン", 100, 15, 4000, 2000, 0, "ランダムな場所にワープする。", "導いてくれ！"),
            new InteractableShelf.DropItem("map", "マップ", 100, 5, 0, 0, 3, "", "この階の事が少しわかった！"), // 3 Areas
            new InteractableShelf.DropItem("warp_gun", "転送銃", 100, 10, 4000, 2000, 0, "当てたモノをワープさせる。", "転送銃を構えた。方向キーで発射！"),
            new InteractableShelf.DropItem("migawari", "身代わり人形", 80, 20, 8000, 4000, 1, "致命傷を一度だけ防ぐ。", ""), // 1 Life
            new InteractableShelf.DropItem("radio", "ラジカセ", 80, 5, 6000, 3000, 10, "音楽を流してエネミーを停止させる。", // 10 Turns
                "８０年代の名作ロックが流れた！",
                "中毒性抜群の激ヤバヒップホップが流れた！",
                "心躍る極上ポップスが流れた！",
                "オシャレな名曲ジャズが流れた！",
                "重低音が響く硬派なEDMが流れた！"),
            new InteractableShelf.DropItem("talisman", "タリスマン", 50, 20, 10000, 5000, 3, "敵を1体以上消す。", "不吉な気配が消え去った！"), // 3 Enemies
            new InteractableShelf.DropItem("bluebox", "青い箱", 50, 5, 12000, 6000, 0, "ドアを呼び出す事ができる", "扉が現れた…！")
        };
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // リストが空の場合はデフォルト値を設定
            if (shelfDropTable == null || shelfDropTable.Count == 0)
            {
                shelfDropTable = GetDefaultDropTable();
            }
            
            // Runtime fix for stale Inspector data
            if (shelfDropTable != null)
            {
                foreach (var item in shelfDropTable)
                {
                    if (item.key == "doll")
                    {
                        item.key = "migawari";
                    }
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetItemDescription(string key)
    {
        var item = shelfDropTable.Find(x => x.key == key);
        if (item != null) return item.description;
        return "";
    }

    public string GetItemUsageMessage(string key)
    {
        var item = shelfDropTable.Find(x => x.key == key);
        if (item != null && item.usageMessages != null && item.usageMessages.Count > 0)
        {
            // Return a random message from the list
            return item.usageMessages[Random.Range(0, item.usageMessages.Count)];
        }
        return "";
    }
    
    public int GetItemEffectValue(string key)
    {
        var item = shelfDropTable.Find(x => x.key == key);
        if (item != null) return item.effectValue;
        return 0;
    }

    public int GetItemSellPrice(string key)
    {
        var item = shelfDropTable.Find(x => x.key == key);
        if (item != null) return item.sellPrice;
        return 0;
    }

    [ContextMenu("Reset Drop Table Defaults")]
    public void ResetDropTableDefaults()
    {
        shelfDropTable = GetDefaultDropTable();
        Debug.Log("[ItemDatabase] Drop Table reset to defaults.");
    }



    public string GetItemName(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (key == "key") return "鍵"; 

        var item = shelfDropTable.Find(x => x.key == key);
        if (item != null) return item.name;
        
        return key; // Fallback to key if name not found
    }
}
