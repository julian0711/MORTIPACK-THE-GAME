using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    // Singleton Instance
    public static ItemDatabase Instance { get; private set; }

    [Header("ドロップリスト設定 (Drop Rates)")]
    [SerializeField]
    public List<InteractableShelf.DropItem> shelfDropTable = new List<InteractableShelf.DropItem>()
    {
        new InteractableShelf.DropItem("nothing", "何もない", 1200, "", ""),
        new InteractableShelf.DropItem("report", "研究資料", 100, "", "これは特ダネだ！(Score+100)"),
        new InteractableShelf.DropItem("radar", "探知機", 100, "隠されたものを見つける装置。", "見えない物が見えるようになったぞ！"),
        new InteractableShelf.DropItem("warpcoin", "ワープコイン", 100, "ランダムな場所にワープする。", "導いてくれ！"),
        new InteractableShelf.DropItem("map", "マップ", 100, "", "この階の事が少しわかった！"),
        new InteractableShelf.DropItem("warp_gun", "転送銃", 100, "当てたモノをワープさせる。", "転送銃を構えた。方向キーで発射！"),
        new InteractableShelf.DropItem("doll", "身代わり人形", 80, "致命傷を一度だけ防ぐ。", ""),
        new InteractableShelf.DropItem("radio", "ラジカセ", 80, "音楽を流してエネミーを停止させる。", 
            "８０年代の名作ロックが流れた！",
            "中毒性抜群の激ヤバヒップホップが流れた！",
            "心躍る極上ポップスが流れた！",
            "オシャレな名曲ジャズが流れた！",
            "重低音が響く硬派なEDMが流れた！"),
        new InteractableShelf.DropItem("talisman", "タリスマン", 50, "敵を1体以上消す。", "不吉な気配が消え去った！"),
        new InteractableShelf.DropItem("bluebox", "青い箱", 50, "ドアを呼び出す事ができる", "扉が現れた…！")
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        shelfDropTable = new List<InteractableShelf.DropItem>()
        {
            new InteractableShelf.DropItem("nothing", "何もなし", 1200, "ただの埃だ。", ""),
            new InteractableShelf.DropItem("report", "研究資料", 100, "古い研究資料のようだ。 Score+100", "レポートを記録した。(Score+100)"),
            new InteractableShelf.DropItem("radar", "探知機", 100, "隠されたものを見つける装置。", "エネミーの気配が可視化された！（10ターン）"),
            // new InteractableShelf.DropItem("hallucinogen", "幻覚剤", 100, "飲むと意識が朦朧とする。", "意識がぼんやりしてきた…"),
            new InteractableShelf.DropItem("warpcoin", "ワープコイン", 100, "不思議な力が宿るコイン。", "ワープした！"),
            new InteractableShelf.DropItem("map", "マップ", 100, "この階層の地図。", "周辺の地図が書き込まれた！"),
            new InteractableShelf.DropItem("warp_gun", "転送銃", 100, "瞬間移動を可能にする銃。", "転送銃を構えた。方向キーで発射！"),
            new InteractableShelf.DropItem("doll", "身代わり人形", 80, "致命傷を一度だけ防ぐ。", "（所持しているだけで効果発揮）"),
            new InteractableShelf.DropItem("radio", "ラジカセ", 80, "ノイズ混じりの音がする。", 
                "怪音波を流した！全敵10ターン停止！",
                "激しいノイズが響き渡る…！(全敵停止)",
                "不快な電子音が敵の動きを封じた！",
                "奇妙な音楽が流れている…(全敵停止)",
                "ラジオから叫び声のような音がした！"),
            new InteractableShelf.DropItem("talisman", "タリスマン", 50, "不吉な気配を遠ざけるお守り。", "不吉な気配が消え去った。"),
            new InteractableShelf.DropItem("bluebox", "青い箱", 50, "謎の青い箱。中身は？", "謎の扉が現れた…")
        };
        Debug.Log("[ItemDatabase] Drop Table reset to defaults.");
    }
}
