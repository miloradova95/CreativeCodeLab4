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
    public Color highlightedSlotColor = Color.yellow; // New highlight color
    
    [Header("Highlight Settings")]
    public float highlightPulseSpeed = 2f; // Speed of pulsing effect
    public float highlightPulseMin = 0.7f; // Minimum alpha for pulse
    public float highlightPulseMax = 1f; // Maximum alpha for pulse
    
    private Item currentItem;
    private bool isHighlighted = false;
    private Color originalSlotColor;
    
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
    
    void Update()
    {
        // Handle highlight pulsing effect
        if (isHighlighted && backgroundImage != null)
        {
            float pulse = Mathf.Lerp(highlightPulseMin, highlightPulseMax, 
                (Mathf.Sin(Time.time * highlightPulseSpeed) + 1f) / 2f);
            
            Color currentColor = backgroundImage.color;
            currentColor.a = pulse;
            backgroundImage.color = currentColor;
        }
    }
    
    public void SetItem(Item item)
    {
        currentItem = item;
        
        if (item != null)
        {
            Debug.Log($"Setting item in slot: {item.itemName}");
            Debug.Log($"Item icon: {(item.itemIcon != null ? item.itemIcon.name : "NULL")}");
            Debug.Log($"Item color: {item.itemColor}");
            
            // Store original color
            originalSlotColor = filledSlotColor;
            
            // Set background to filled color (will be overridden if highlighted)
            if (backgroundImage != null && !isHighlighted)
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
        isHighlighted = false;
        originalSlotColor = emptySlotColor;
        
        if (backgroundImage != null)
            backgroundImage.color = emptySlotColor;
        
        if (itemImage != null)
        {
            itemImage.enabled = false;
            itemImage.sprite = null;
        }
    }
    
    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;
        
        if (backgroundImage != null)
        {
            if (highlighted)
            {
                backgroundImage.color = highlightedSlotColor;
            }
            else
            {
                // Return to original color
                if (currentItem != null)
                {
                    backgroundImage.color = filledSlotColor;
                }
                else
                {
                    backgroundImage.color = emptySlotColor;
                }
            }
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
    
    public bool IsHighlighted()
    {
        return isHighlighted;
    }
}