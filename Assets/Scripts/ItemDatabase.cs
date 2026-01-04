using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    // Singleton Instance
    public static ItemDatabase Instance { get; private set; }

    [Header("ドロップリスト設定 (Drop Rates)")]
    [SerializeField]
    public List<InteractableShelf.DropItem> shelfDropTable = GetDefaultDropTable();

    // Shared method to define defaults in one place
    private static List<InteractableShelf.DropItem> GetDefaultDropTable()
    {
        return new List<InteractableShelf.DropItem>()
        {
            new InteractableShelf.DropItem("nothing", "何もない", 1200, 0, "", ""),
            new InteractableShelf.DropItem("report", "研究資料", 100, 0, "", "これは特ダネだ！(Score+100)"),
            new InteractableShelf.DropItem("radar", "探知機", 100, 3000, "隠されたものを見つける装置。", "見えない物が見えるようになったぞ！"),
            new InteractableShelf.DropItem("warpcoin", "ワープコイン", 100, 4000, "ランダムな場所にワープする。", "導いてくれ！"),
            new InteractableShelf.DropItem("map", "マップ", 100, 0, "", "この階の事が少しわかった！"),
            new InteractableShelf.DropItem("warp_gun", "転送銃", 100, 4000, "当てたモノをワープさせる。", "転送銃を構えた。方向キーで発射！"),
            new InteractableShelf.DropItem("migawari", "身代わり人形", 80, 8000, "致命傷を一度だけ防ぐ。", ""),
            new InteractableShelf.DropItem("radio", "ラジカセ", 80, 6000, "音楽を流してエネミーを停止させる。", 
                "８０年代の名作ロックが流れた！",
                "中毒性抜群の激ヤバヒップホップが流れた！",
                "心躍る極上ポップスが流れた！",
                "オシャレな名曲ジャズが流れた！",
                "重低音が響く硬派なEDMが流れた！"),
            new InteractableShelf.DropItem("talisman", "タリスマン", 50, 10000, "敵を1体以上消す。", "不吉な気配が消え去った！"),
            new InteractableShelf.DropItem("bluebox", "青い箱", 50, 12000, "ドアを呼び出す事ができる", "扉が現れた…！")
        };
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Runtime fix for stale Inspector data
            if (shelfDropTable != null)
            {
                foreach (var item in shelfDropTable)
                {
                    if (item.key == "doll")
                    {
                        item.key = "migawari";
                        Debug.Log("[ItemDatabase] Auto-migrated key 'doll' to 'migawari'");
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

    [ContextMenu("Reset Drop Table Defaults")]
    public void ResetDropTableDefaults()
    {
        shelfDropTable = GetDefaultDropTable();
        Debug.Log("[ItemDatabase] Drop Table reset to defaults.");
    }
}
