using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public GameObject itemObject; // Reference to the actual collected object
    public Vector3 originalScale;
    public Vector3 originalPosition;
    public Quaternion originalRotation;
    public Transform originalParent;
}

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxInventorySize = 20;
    
    [Header("Item Display")]
    public Transform itemHoldPosition; // Position in front of camera where items are held
    public float itemDisplayDistance = 1.5f;
    public float itemDisplayScale = 0.5f;
    public Vector3 holdOffset = new Vector3(0.3f, -0.3f, 0f);
    
    [Header("References")]
    public Camera playerCamera;
    
    // Private variables
    public List<InventoryItem> inventory = new List<InventoryItem>();
    private int currentItemIndex = -1;
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        if (itemHoldPosition == null)
        {
            // Create a default hold position
            GameObject holdPos = new GameObject("ItemHoldPosition");
            holdPos.transform.SetParent(playerCamera.transform);
            holdPos.transform.localPosition = holdOffset + Vector3.forward * itemDisplayDistance;
            itemHoldPosition = holdPos.transform;
        }
    }
    
    void Update()
    {
        HandleInventoryScrolling();
    }
    
    void HandleInventoryScrolling()
    {
        if (inventory.Count == 0) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Debug.Log("Scroll value: " + scroll); // Add this line

        
        if (Mathf.Abs(scroll) > 0.1f) // Add deadzone to prevent accidental scrolling
        {
            if (scroll > 0f) // Scroll up
            {
                ScrollToNextItem();
            }
            else if (scroll < 0f) // Scroll down
            {
                ScrollToPreviousItem();
            }
        }
    }
    
    public bool AddItem(CollectibleItem collectibleComponent)
    {
        if (inventory.Count >= maxInventorySize)
        {
            Debug.Log("Inventory is full!");
            return false;
        }
        
        GameObject itemObject = collectibleComponent.gameObject;
        
        // Store original transform data
        InventoryItem item = new InventoryItem
        {
            itemName = collectibleComponent.itemName,
            itemDescription = collectibleComponent.itemDescription,
            itemIcon = collectibleComponent.itemIcon,
            itemObject = itemObject,
            originalScale = itemObject.transform.localScale,
            originalPosition = itemObject.transform.position,
            originalRotation = itemObject.transform.rotation,
            originalParent = itemObject.transform.parent
        };
        
        inventory.Add(item);
        
        // Prepare the item for inventory (disable physics, etc.)
        PrepareItemForInventory(itemObject);
        
        // Hide the item initially
        itemObject.SetActive(false);
        
        // If this is the first item, display it
        if (inventory.Count == 1)
        {
            currentItemIndex = 0;
            DisplayCurrentItem();
        }
        
        Debug.Log($"Added {item.itemName} to inventory. Total items: {inventory.Count}");
        return true;
    }
    
    void PrepareItemForInventory(GameObject itemObject)
    {
        // Disable physics
        Rigidbody rb = itemObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Disable colliders (except trigger colliders for interaction)
        Collider[] colliders = itemObject.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger)
                col.enabled = false;
        }
        
        // Remove the CollectibleItem component since it's now collected
        CollectibleItem collectible = itemObject.GetComponent<CollectibleItem>();
        if (collectible != null)
            Destroy(collectible);
    }
    
    public void ScrollToNextItem()
    {
        if (inventory.Count <= 1) return; // No point scrolling with 0 or 1 items
        
        // Hide current item
        HideCurrentItem();
        
        currentItemIndex = (currentItemIndex + 1) % inventory.Count;
        DisplayCurrentItem();
        
        Debug.Log($"Scrolled to: {inventory[currentItemIndex].itemName} ({currentItemIndex + 1}/{inventory.Count})");
    }
    
    public void ScrollToPreviousItem()
    {
        if (inventory.Count <= 1) return; // No point scrolling with 0 or 1 items
        
        // Hide current item
        HideCurrentItem();
        
        currentItemIndex--;
        if (currentItemIndex < 0)
            currentItemIndex = inventory.Count - 1;
        
        DisplayCurrentItem();
        
        Debug.Log($"Scrolled to: {inventory[currentItemIndex].itemName} ({currentItemIndex + 1}/{inventory.Count})");
    }
    
    void HideCurrentItem()
    {
        if (currentItemIndex >= 0 && currentItemIndex < inventory.Count)
        {
            InventoryItem currentItem = inventory[currentItemIndex];
            if (currentItem.itemObject != null)
            {
                currentItem.itemObject.SetActive(false);
            }
        }
    }
    
    void DisplayCurrentItem()
    {
        if (currentItemIndex >= 0 && currentItemIndex < inventory.Count)
        {
            InventoryItem currentItem = inventory[currentItemIndex];
            
            if (currentItem.itemObject != null)
            {
                // Activate the item
                currentItem.itemObject.SetActive(true);
                
                // Position it at the hold position
                currentItem.itemObject.transform.SetParent(itemHoldPosition);
                currentItem.itemObject.transform.localPosition = Vector3.zero;
                currentItem.itemObject.transform.localRotation = Quaternion.identity;
                currentItem.itemObject.transform.localScale = currentItem.originalScale * itemDisplayScale;
                
                // Add rotation component if it doesn't exist
                ItemDisplayRotator rotator = currentItem.itemObject.GetComponent<ItemDisplayRotator>();
                if (rotator == null)
                {
                    rotator = currentItem.itemObject.AddComponent<ItemDisplayRotator>();
                }
                
                Debug.Log($"Now displaying: {currentItem.itemName}");
            }
        }
    }
    
    public void DropCurrentItem()
    {
        if (currentItemIndex >= 0 && currentItemIndex < inventory.Count)
        {
            InventoryItem itemToDrop = inventory[currentItemIndex];
            
            // Restore original state
            RestoreItemToWorld(itemToDrop);
            
            // Remove from inventory
            inventory.RemoveAt(currentItemIndex);
            
            // Adjust current index
            if (currentItemIndex >= inventory.Count)
            {
                currentItemIndex = inventory.Count - 1;
            }
            
            // Display new current item or hide if no items left
            if (inventory.Count == 0)
            {
                currentItemIndex = -1;
            }
            else
            {
                DisplayCurrentItem();
            }
        }
    }
    
    void RestoreItemToWorld(InventoryItem item)
    {
        if (item.itemObject != null)
        {
            // Restore transform
            item.itemObject.transform.SetParent(item.originalParent);
            item.itemObject.transform.position = item.originalPosition + Vector3.up * 0.5f; // Drop slightly above original position
            item.itemObject.transform.rotation = item.originalRotation;
            item.itemObject.transform.localScale = item.originalScale;
            
            // Re-enable physics
            Rigidbody rb = item.itemObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            
            // Re-enable colliders
            Collider[] colliders = item.itemObject.GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }
            
            // Remove display rotator
            ItemDisplayRotator rotator = item.itemObject.GetComponent<ItemDisplayRotator>();
            if (rotator != null)
                Destroy(rotator);
        }
    }
    
    public InventoryItem GetCurrentItem()
    {
        if (currentItemIndex >= 0 && currentItemIndex < inventory.Count)
            return inventory[currentItemIndex];
        return null;
    }
    
    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(inventory);
    }
    
    public int GetInventoryCount()
    {
        return inventory.Count;
    }
}