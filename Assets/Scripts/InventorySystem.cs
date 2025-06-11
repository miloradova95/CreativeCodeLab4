using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    
    [Header("UI References")]
    public GameObject inventoryPanel;
    public InventorySlot[] inventorySlots = new InventorySlot[5]; // Array for 5 slots

    private List<Item> items = new List<Item>();
    private int maxSlots = 5;

    void Start()
    {
        // Initialize all slots as empty
        UpdateInventoryUI();
    }

    public bool AddItem(Item item)
    {
        if (item != null && items.Count < maxSlots)
        {
            items.Add(item);
            item.gameObject.SetActive(false); // Hide the item in the scene
            UpdateInventoryUI();
            return true;
        }
        return false;
    }

    public void DropCurrentItem()
    {
        if (items.Count > 0)
        {
            Item currentItem = items[items.Count - 1]; // Last item is current
            items.RemoveAt(items.Count - 1);
            
            // Position the dropped item in front of player
            if (playerCamera != null)
            {
                Vector3 dropPosition = playerCamera.transform.position + playerCamera.transform.forward * 2f;
                dropPosition.y = playerCamera.transform.position.y - 0.5f; // Slightly below camera level
                currentItem.transform.position = dropPosition;
            }
            
            currentItem.gameObject.SetActive(true); // Show the item in the scene
            UpdateInventoryUI();
        }
    }

    private void UpdateInventoryUI()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null)
            {
                if (i < items.Count)
                {
                    inventorySlots[i].SetItem(items[i]);
                }
                else
                {
                    inventorySlots[i].SetEmpty();
                }
            }
        }
    }

    public List<Item> GetAllItems()
    {
        return items;
    }
    
    public bool IsFull()
    {
        return items.Count >= maxSlots;
    }
}