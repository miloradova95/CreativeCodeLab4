using UnityEngine;

public class CollectibleItem : Item
{
    [Header("Inventory Display")]
    public Sprite inventorySprite;
    public Color inventoryColor = Color.white;
    
    protected override void Start()
    {
        base.Start();
        
        // Set the item icon and color from our local fields
        itemIcon = inventorySprite;
        itemColor = inventoryColor;
    }
    
    public override void Interact()
    {
        InventorySystem inventory = FindObjectOfType<InventorySystem>();
        if (inventory != null)
        {
            inventory.AddItem(this);
        }
    }
    
    // Method to set sprite and color (called by SymbolHandler)
    public void SetInventoryDisplay(Sprite sprite, Color color, string displayName)
    {
        inventorySprite = sprite;
        inventoryColor = color;
        itemIcon = sprite;
        itemColor = color;
        itemName = displayName;
    }
}