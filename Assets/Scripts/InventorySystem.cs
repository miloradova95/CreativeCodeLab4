using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    
    [Header("UI References")]
    public GameObject inventoryPanel;
    public InventorySlot[] inventorySlots = new InventorySlot[5]; // Array for 5 slots

    [Header("Item Holding")]
    public Transform itemHoldPosition; // Where the held item appears (assign in inspector)
    public float itemHoldDistance = 3f; // Distance from camera
    public Vector3 itemHoldOffset = new Vector3(0.5f, -0.3f, 0f); // Offset for positioning

    [Header("Key Prefab")]

    // Store both the item and its symbol data
    private List<CollectibleItem> items = new List<CollectibleItem>();
    private List<SymbolData> itemSymbols = new List<SymbolData>(); // Store symbol data separately
    
    private int maxSlots = 5;
    private int currentSelectedIndex = -1; // -1 means no selection
    private GameObject heldItemDisplay; // The 3D representation of the held item
    private bool isInitialized = false;

    void Start()
    {
        // Initialize all slots as empty
        UpdateInventoryUI();
    }

    void Update()
    {
        // Ensure hold position is created before handling input
        if (!isInitialized)
        {
            InitializeHoldPosition();
        }
        
        HandleScrollInput();
    }

    void InitializeHoldPosition()
    {
        // Create item hold position if not assigned and camera is available
        if (itemHoldPosition == null && playerCamera != null)
        {
            GameObject holdPos = new GameObject("ItemHoldPosition");
            holdPos.transform.SetParent(playerCamera.transform);
            
            // Use itemHoldDistance to position the hold point
            Vector3 forwardOffset = playerCamera.transform.forward * (itemHoldDistance * 0.01f); // Convert to reasonable scale
            holdPos.transform.localPosition = itemHoldOffset + forwardOffset;
            
            itemHoldPosition = holdPos.transform;
            Debug.Log("Created ItemHoldPosition at: " + itemHoldPosition.position);
        }
        
        isInitialized = (itemHoldPosition != null);
    }

    void HandleScrollInput()
    {
        if (!isInitialized) return; // Don't handle input until initialized
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll > 0f) // Scroll up
        {
            SelectNextItem();
        }
        else if (scroll < 0f) // Scroll down
        {
            SelectPreviousItem();
        }
    }

    void SelectNextItem()
    {
        if (items.Count == 0) return;

        currentSelectedIndex++;
        if (currentSelectedIndex >= items.Count)
        {
            currentSelectedIndex = -1; // Go back to no selection
        }
        
        UpdateSelection();
    }

    void SelectPreviousItem()
    {
        if (items.Count == 0) return;

        currentSelectedIndex--;
        if (currentSelectedIndex < -1)
        {
            currentSelectedIndex = items.Count - 1; // Go to last item
        }
        
        UpdateSelection();
    }

    void UpdateSelection()
    {
        // Update UI highlighting
        UpdateInventoryUI();
        
        // Update held item display
        UpdateHeldItemDisplay();
    }

    void UpdateHeldItemDisplay()
    {
        Debug.Log($"UpdateHeldItemDisplay called. CurrentSelectedIndex: {currentSelectedIndex}, Items count: {items.Count}");
        
        // Destroy current held item display
        if (heldItemDisplay != null)
        {
            Destroy(heldItemDisplay);
            heldItemDisplay = null;
            Debug.Log("Destroyed previous held item display");
        }

        // Create new held item display if something is selected
        if (currentSelectedIndex >= 0 && currentSelectedIndex < items.Count && itemHoldPosition != null)
        {
            SymbolData symbolData = itemSymbols[currentSelectedIndex];
            CollectibleItem selectedItem = items[currentSelectedIndex];
            Debug.Log($"Creating held item display for: {selectedItem.itemName} with symbol: {(symbolData != null ? symbolData.name : "None")}");
            CreateHeldItemDisplay(selectedItem, symbolData);
        }
        else
        {
            Debug.Log("No item selected or invalid index - not creating held display");
        }
    }

    void CreateHeldItemDisplay(CollectibleItem item, SymbolData symbolData)
    {
        if (itemHoldPosition == null)
        {
            Debug.LogWarning("Cannot create held item display - missing itemHoldPosition or keyPrefab");
            return;
        }

        // Instantiate the held item display
        heldItemDisplay = Instantiate(item.gameObject, itemHoldPosition.position, itemHoldPosition.rotation, itemHoldPosition);
        heldItemDisplay.SetActive(true);

        // Scale it down for held display
        heldItemDisplay.transform.localScale = Vector3.one * 0.3f;
        
        // Remove any colliders since this is just for display
        Collider[] colliders = heldItemDisplay.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            Destroy(col);
        }
        
        // Remove the CollectibleItem script since this is just for display
        CollectibleItem collectible = heldItemDisplay.GetComponent<CollectibleItem>();
        if (collectible != null)
        {
            Destroy(collectible);
        }

        // Get the SymbolHandler and apply the symbol
        SymbolHandler symbolHandler = heldItemDisplay.GetComponent<SymbolHandler>();
        if (symbolHandler != null && symbolData != null)
        {
            symbolHandler.ApplySymbol(symbolData);
            Debug.Log($"Applied symbol {symbolData.name} to held item display");
        }
        else
        {
            Debug.LogWarning("SymbolHandler not found or symbolData is null");
        }

        // Add animation to make it look nice
        HeldItemAnimator animator = heldItemDisplay.GetComponent<HeldItemAnimator>();
        if (animator == null)
        {
            animator = heldItemDisplay.AddComponent<HeldItemAnimator>();
        }
        
        Debug.Log($"Created held item display for: {item.itemName}");
    }

    public void AddItem(CollectibleItem item)
    {
        if (item != null && items.Count < maxSlots)
        {
            // Get the symbol data BEFORE deactivating the item
            SymbolData symbolData = null;
            SymbolHandler symbolHandler = item.GetComponent<SymbolHandler>();
            if (symbolHandler != null)
            {
                symbolData = symbolHandler.currentSymbol;
                Debug.Log($"Found SymbolHandler with symbol: {(symbolData != null ? symbolData.name : "NULL")}");
            }
            else
            {
                Debug.LogWarning($"No SymbolHandler found on item: {item.itemName}");
            }

            // Check if this will be the first item BEFORE adding it
            bool isFirstItem = (items.Count == 0 && currentSelectedIndex == -1);

            // Add item and its symbol data to our lists
            items.Add(item);
            itemSymbols.Add(symbolData);
            
            item.gameObject.SetActive(false); // Hide the item in the scene
            
            // If this is the first item and nothing is selected, auto-select it
            if (isFirstItem && isInitialized) // Only auto-select if initialized
            {
                Debug.Log("Auto-selecting first item");
                currentSelectedIndex = 0;
                UpdateSelection();
            }
            else
            {
                UpdateInventoryUI();
            }
            
            Debug.Log($"Added item: {item.itemName} with symbol: {(symbolData != null ? symbolData.name : "None")}");
        }
    }

    public void DropCurrentItem()
    {
        if (items.Count > 0)
        {
            // If we're dropping the currently selected item, adjust selection
            int dropIndex = items.Count - 1; // Last item is dropped
            if (currentSelectedIndex == dropIndex)
            {
                currentSelectedIndex = -1; // Deselect
            }
            else if (currentSelectedIndex > dropIndex)
            {
                currentSelectedIndex--; // Adjust index
            }

            Item currentItem = items[dropIndex];
            items.RemoveAt(dropIndex);
            itemSymbols.RemoveAt(dropIndex); // Also remove the symbol data
            
            // Position the dropped item in front of player
            if (playerCamera != null)
            {
                Vector3 dropPosition = playerCamera.transform.position + playerCamera.transform.forward * 2f;
                dropPosition.y = playerCamera.transform.position.y - 0.5f;
                currentItem.transform.position = dropPosition;
            }
            
            currentItem.gameObject.SetActive(true);
            UpdateSelection(); // This will update both UI and held item display
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
                    // Highlight if this is the selected item
                    inventorySlots[i].SetHighlighted(i == currentSelectedIndex);
                }
                else
                {
                    inventorySlots[i].SetEmpty();
                    inventorySlots[i].SetHighlighted(false);
                }
            }
        }
    }

    public List<CollectibleItem> GetAllItems()
    {
        return items;
    }
    
    public bool IsFull()
    {
        return items.Count >= maxSlots;
    }

    public CollectibleItem GetCurrentlyHeldItem()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < items.Count)
        {
            return items[currentSelectedIndex];
        }
        return null;
    }

    public SymbolData GetCurrentlyHeldSymbol()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < itemSymbols.Count)
        {
            return itemSymbols[currentSelectedIndex];
        }
        return null;
    }

    public int GetCurrentSelectedIndex()
    {
        return currentSelectedIndex;
    }

    public void SelectItemByIndex(int index)
    {
        if (index >= 0 && index < items.Count)
        {
            currentSelectedIndex = index;
        }
        else if (index >= items.Count || index < 0)
        {
            currentSelectedIndex = -1; // Deselect if invalid index
        }
        
        UpdateSelection();
    }
}