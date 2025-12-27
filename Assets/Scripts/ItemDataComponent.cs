using UnityEngine;
using System.Collections.Generic;

public class ItemDataComponent : MonoBehaviour
{
    public static ItemDataComponent Instance { get; private set; }

    [Header("Drop Rates")]
    [SerializeField]
    public List<InteractableShelf.DropItem> shelfDropTable = new List<InteractableShelf.DropItem>()
    {
        new InteractableShelf.DropItem("nothing", "何もなし", 1200, "ただの埃だ。"),
        new InteractableShelf.DropItem("report", "研究資料", 100, "古い研究資料のようだ。 Score+100"),
        new InteractableShelf.DropItem("radar", "探知機", 100, "隠されたものを見つける装置。"),
        new InteractableShelf.DropItem("hallucinogen", "幻覚剤", 100, "飲むと意識が朦朧とする。"),
        new InteractableShelf.DropItem("warpcoin", "ワープコイン", 100, "不思議な力が宿るコイン。"),
        new InteractableShelf.DropItem("map", "マップ", 100, "この階層の地図。"),
        new InteractableShelf.DropItem("warp_gun", "転送銃", 100, "瞬間移動を可能にする銃。"),
        new InteractableShelf.DropItem("doll", "身代わり人形", 80, "致命傷を一度だけ防ぐ。"),
        new InteractableShelf.DropItem("radio", "ラジカセ", 80, "ノイズ混じりの音がする。"),
        new InteractableShelf.DropItem("talisman", "タリスマン", 50, "不吉な気配を遠ざけるお守り。"),
        new InteractableShelf.DropItem("bluebox", "青い箱", 50, "謎の青い箱。中身は？")
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional
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

    [ContextMenu("Reset Drop Table Defaults")]
    public void ResetDropTableDefaults()
    {
        shelfDropTable = new List<InteractableShelf.DropItem>()
        {
            new InteractableShelf.DropItem("nothing", "何もなし", 1200, "ただの埃だ。"),
            new InteractableShelf.DropItem("report", "研究資料", 100, "古い研究資料のようだ。 Score+100"),
            new InteractableShelf.DropItem("radar", "探知機", 100, "隠されたものを見つける装置。"),
            new InteractableShelf.DropItem("hallucinogen", "幻覚剤", 100, "飲むと意識が朦朧とする。"),
            new InteractableShelf.DropItem("warpcoin", "ワープコイン", 100, "不思議な力が宿るコイン。"),
            new InteractableShelf.DropItem("map", "マップ", 100, "この階層の地図。"),
            new InteractableShelf.DropItem("warp_gun", "転送銃", 100, "瞬間移動を可能にする銃。"),
            new InteractableShelf.DropItem("doll", "身代わり人形", 80, "致命傷を一度だけ防ぐ。"),
            new InteractableShelf.DropItem("radio", "ラジカセ", 80, "ノイズ混じりの音がする。"),
            new InteractableShelf.DropItem("talisman", "タリスマン", 50, "不吉な気配を遠ざけるお守り。"),
            new InteractableShelf.DropItem("bluebox", "青い箱", 50, "謎の青い箱。中身は？")
        };
        Debug.Log("[ItemDataComponent] Drop Table reset to defaults.");
    }
}
