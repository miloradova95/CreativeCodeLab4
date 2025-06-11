using UnityEngine;

[CreateAssetMenu(menuName = "Symbol/SymbolData")]
public class SymbolData : ScriptableObject
{
    [Header("3D Display")]
    public Mesh symbolMesh;
    public Material symbolMaterial;
    
     [Header("Inventory Display")]
     public Sprite inventoryIcon; // Icon to show in inventory
     public Color inventoryColor = Color.white; // Fallback color
 }