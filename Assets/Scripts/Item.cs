using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName = "Item";
    public string itemDescription = "A generic item";
    public Sprite itemIcon;

    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public LayerMask playerLayer = 1;

    [Header("Visual Feedback")]
    public GameObject highlightEffect;
    public Color highlightColor = Color.yellow;

    protected Renderer itemRenderer;
    protected Color originalColor;
    protected bool isHighlighted = false;
    protected Material originalMaterial;
    protected Material highlightMaterial;

    protected virtual void Start()
    {
        itemRenderer = GetComponent<Renderer>();
        if (itemRenderer != null)
        {
            originalMaterial = itemRenderer.material;
            originalColor = originalMaterial.color;

            highlightMaterial = new Material(originalMaterial);
            highlightMaterial.color = highlightColor;
            highlightMaterial.SetFloat("_Emission", 0.3f);
        }

        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }

    public void OnLookedAt()
    {
        if (!isHighlighted)
        {
            isHighlighted = true;

            if (itemRenderer != null)
                itemRenderer.material = highlightMaterial;

            if (highlightEffect != null)
                highlightEffect.SetActive(true);
        }
    }

    public void OnLookedAway()
    {
        if (isHighlighted)
        {
            isHighlighted = false;

            if (itemRenderer != null)
                itemRenderer.material = originalMaterial;

            if (highlightEffect != null)
                highlightEffect.SetActive(false);
        }
    }

    public abstract void Interact();

    protected void OnDestroy()
    {
        if (highlightMaterial != null && highlightMaterial != originalMaterial)
        {
            DestroyImmediate(highlightMaterial);
        }
    }
}
