using UnityEngine;

public class SymbolHandler : MonoBehaviour
{
    [Header("Symbol Settings")]
    public SymbolLibrary symbolLibrary;
    public Transform symbolDisplayTarget;

    public SymbolData currentSymbol { get; private set; }

    private GameObject symbolObject;

    void Start()
    {
    }

    public void AssignRandomSymbol()
    {
        if (symbolLibrary == null || symbolLibrary.availableSymbols.Length == 0)
        {
            Debug.LogWarning("No symbols in symbol library.");
            return;
        }

        if (symbolDisplayTarget == null)
        {
            Debug.LogWarning("Symbol Display Target not assigned.");
            return;
        }

        currentSymbol = symbolLibrary.availableSymbols[Random.Range(0, symbolLibrary.availableSymbols.Length)];
        ApplySymbol(currentSymbol);
    }

    public void ApplySymbol(SymbolData symbol)
    {
        if (symbolDisplayTarget == null) return;

        currentSymbol = symbol;

        // Apply 3D symbol
        if (symbolObject != null)
            Destroy(symbolObject);

        symbolObject = new GameObject("Symbol");
        symbolObject.transform.SetParent(symbolDisplayTarget, false);

        MeshFilter meshFilter = symbolObject.AddComponent<MeshFilter>();
        meshFilter.mesh = symbol.symbolMesh;

        MeshRenderer meshRenderer = symbolObject.AddComponent<MeshRenderer>();
        meshRenderer.material = symbol.symbolMaterial;

        // Apply inventory data to CollectibleItem if present
        CollectibleItem collectible = GetComponent<CollectibleItem>();
        if (collectible != null)
        {
            Debug.Log($"Applying symbol {symbol.name} to CollectibleItem. Sprite: {(symbol.inventoryIcon != null ? symbol.inventoryIcon.name : "NULL")}");
            collectible.SetInventoryDisplay(symbol.inventoryIcon, symbol.inventoryColor, symbol.name);
        }
        
        // Also apply to base Item component for other uses
        Item item = GetComponent<Item>();
        if (item != null)
        {
            Debug.Log($"Applying symbol {symbol.name} to Item. Sprite: {(symbol.inventoryIcon != null ? symbol.inventoryIcon.name : "NULL")}");
            item.itemIcon = symbol.inventoryIcon;
            item.itemColor = symbol.inventoryColor;
            item.itemName = symbol.name;
        }
    }
}