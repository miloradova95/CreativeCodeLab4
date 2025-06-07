using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 5f;
    public LayerMask interactionLayers = -1; // All layers by default
    public KeyCode interactionKey = KeyCode.E;

    [Header("References")]
    public Camera playerCamera;

    [Header("UI Feedback")]
    public GameObject interactionPrompt; // Optional UI element showing "Press E to collect"

    private Item currentLookedAtItem;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    void Update()
    {
        CheckForInteractables();
        HandleInteractionInput();
    }

    void CheckForInteractables()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        Item newLookedAtItem = null;

        if (Physics.Raycast(ray, out hit, interactionRange, interactionLayers))
        {
            Item item = hit.collider.GetComponent<Item>();
            if (item != null)
            {
                newLookedAtItem = item;
            }
        }

        // Handle looking at new item
        if (newLookedAtItem != currentLookedAtItem)
        {
            // Stop looking at previous item
            if (currentLookedAtItem != null)
            {
                currentLookedAtItem.OnLookedAway();
            }

            // Start looking at new item
            currentLookedAtItem = newLookedAtItem;
            if (currentLookedAtItem != null)
            {
                currentLookedAtItem.OnLookedAt();
            }

            // Update UI
            UpdateInteractionUI();
        }
    }

    void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactionKey))
        {
            if (currentLookedAtItem != null)
            {
                if (currentLookedAtItem.gameObject.CompareTag("Collectible"))
                {
                    CollectibleItem collectible = currentLookedAtItem.GetComponent<CollectibleItem>();
                    collectible.Interact();
                    currentLookedAtItem = null;
                    UpdateInteractionUI();
                }

                if (currentLookedAtItem.gameObject.CompareTag("Door"))
                {
                    UnlockableItem unlockable = currentLookedAtItem.GetComponent<UnlockableItem>();
                    if (unlockable != null)
                    {
                        unlockable.Interact();
                    }
                }
            }
        }
    }

    void UpdateInteractionUI()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(currentLookedAtItem != null);
        }
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Vector3 rayStart = playerCamera.transform.position;
            Vector3 rayDirection = playerCamera.transform.forward;
            Gizmos.DrawRay(rayStart, rayDirection * interactionRange);
        }
    }
}