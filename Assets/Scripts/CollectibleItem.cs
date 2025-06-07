using UnityEngine;

public class CollectibleItem : Item
{
    public override void Interact()
    {
        InventorySystem inventory = FindObjectOfType<InventorySystem>();
        if (inventory != null)
        {
            if (inventory.AddItem(this))
            {
                Debug.Log($"Collected: {itemName}");
            }
        }
    }
}
