using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class InventorySlot : MonoBehaviour
{
    [Header("UI Components")]
    public Image backgroundImage; // The slot background
    public Image itemImage; // The item icon display
    
    [Header("Colors")]
    public Color emptySlotColor = Color.gray;
    public Color filledSlotColor = Color.white;
    
    private Item currentItem;
    
    void Start()
    {
        // Make sure item image starts invisible
        if (itemImage != null)
        {
            itemImage.enabled = false;
        }
        
        // Set empty state
        SetEmpty();
    }
    
    public void SetItem(Item item)
    {
        currentItem = item;
        
        if (item != null)
        {
            Debug.Log($"Setting item in slot: {item.itemName}");
            Debug.Log($"Item icon: {(item.itemIcon != null ? item.itemIcon.name : "NULL")}");
            Debug.Log($"Item color: {item.itemColor}");
            
            // Set background to filled color
            if (backgroundImage != null)
                backgroundImage.color = filledSlotColor;
            
            // Show item icon or color
            if (itemImage != null)
            {
                itemImage.enabled = true;
                
                if (item.itemIcon != null)
                {
                    // Use the item's icon
                    itemImage.sprite = item.itemIcon;
                    itemImage.color = Color.white; // This should make the sprite fully visible
                    Debug.Log($"Applied sprite: {item.itemIcon.name}");
                    Debug.Log($"ItemImage sprite is now: {(itemImage.sprite != null ? itemImage.sprite.name : "NULL")}");
                    Debug.Log($"ItemImage color is now: {itemImage.color}");
                    Debug.Log($"ItemImage enabled: {itemImage.enabled}");
                }
                else
                {
                    // Use solid color as fallback - need a default sprite for color to show
                    if (itemImage.sprite == null)
                    {
                        // If no default sprite is assigned, we can't show a color
                        Debug.LogWarning("No sprite found and no default sprite assigned to show color!");
                    }
                    itemImage.color = item.itemColor;
                    Debug.Log($"No sprite found, using color: {item.itemColor}");
                }
            }
        }
        else
        {
            SetEmpty();
        }
    }
    
    public void SetEmpty()
    {
        currentItem = null;
        
        if (backgroundImage != null)
            backgroundImage.color = emptySlotColor;
        
        if (itemImage != null)
        {
            itemImage.enabled = false;
            itemImage.sprite = null;
        }
    }
    
    public Item GetItem()
    {
        return currentItem;
    }
    
    public bool IsEmpty()
    {
        return currentItem == null;
    }
}