using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public string itemId;
    public int price = 500;
    
    // Attempt to purchase the item
    public bool TryPurchase(int currentScore)
    {
        if (currentScore >= price)
        {
            return true;
        }
        return false;
    }

    public void Setup(string id, int cost)
    {
        itemId = id;
        price = cost;
    }
}
