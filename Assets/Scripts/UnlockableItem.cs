using UnityEngine;

public class UnlockableItem : Item
{
    [Header("Unlock Settings")]
    public string requiredItemName;
    public bool isUnlocked = false;

    public override void Interact()
    {
        if (isUnlocked)
        {
            Debug.Log("Already unlocked.");
            return;
        }

        InventorySystem inventory = FindObjectOfType<InventorySystem>();
        if (inventory != null)
        {
            if (inventory.GetCurrentlyHeldSymbol().name == requiredItemName)
            {
                Unlock();
                return;
            }
            else
            {
                Debug.Log($"name: {inventory.GetCurrentlyHeldSymbol().name}");
                Debug.Log($"Currently held item does not match required item: {requiredItemName}");
            }
            // foreach (var item in inventory.GetAllItems())
            // {
            //     if (item.itemName == requiredItemName)
            //     {
            //         Unlock();
            //         return;
            //     }
            // }
        }

        Debug.Log($"Missing required item: {requiredItemName}");
    }

    private void Unlock()
    {
        isUnlocked = true;
        Debug.Log("Item unlocked!");
        gameObject.SetActive(false);
    }
}
