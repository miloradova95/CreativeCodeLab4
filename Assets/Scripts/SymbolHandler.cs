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

        if (symbolObject != null)
            Destroy(symbolObject);

        symbolObject = new GameObject("Symbol");
        symbolObject.transform.SetParent(symbolDisplayTarget, false); // Keep it local to target

        MeshFilter meshFilter = symbolObject.AddComponent<MeshFilter>();
        meshFilter.mesh = symbol.symbolMesh;

        MeshRenderer meshRenderer = symbolObject.AddComponent<MeshRenderer>();
        meshRenderer.material = symbol.symbolMaterial;
    }
}
