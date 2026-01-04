using UnityEngine;

public class TrashCan : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.SetSellMode(true);
                GameUIManager.Instance.ShowMessage("不要アイテムを長押しでPtに代える事ができるようだ。", "trash");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.SetSellMode(false);
                GameUIManager.Instance.ShowMessage("");
            }
        }
    }
}
