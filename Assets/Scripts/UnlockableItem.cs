using System.Collections;
using UnityEngine;

public class UnlockableItem : Item
{
    [Header("Unlock Settings")]
    public string requiredItemName;
    public bool isUnlocked = false;

    private CallEvent callEvent;

    private void Start()
    {
        callEvent = GetComponent<CallEvent>();
        if (callEvent == null)
        {
            callEvent = FindObjectOfType<CallEvent>();
        }

        if (callEvent == null)
        {
            Debug.LogWarning("CallEvent component not found!");
        }
    }

public override void Interact()
{
    if (isUnlocked)
    {
        Debug.Log("Already unlocked.");
        return;
    }

    InventorySystem inventory = FindObjectOfType<InventorySystem>();
    if (inventory != null)
    {
        if (inventory.GetCurrentlyHeldSymbol().name == requiredItemName)
        {
            // Delay unlock and sound
            if (callEvent != null)
                StartCoroutine(DelayedUnlock());
            return;
        }
        else
        {
            Debug.Log($"name: {inventory.GetCurrentlyHeldSymbol().name}");
            Debug.Log($"Currently held item does not match required item: {requiredItemName}");

            if (callEvent != null)
                callEvent.Callevent("CantUnlockDoor");
        }
    }
    else
    {
        Debug.Log($"Missing required item: {requiredItemName}");
        if (callEvent != null)
            callEvent.Callevent("CantUnlockDoor");
    }
}

private IEnumerator DelayedUnlock()
{
    callEvent.Callevent("UnlockDoor"); // Play unlock sound
    yield return new WaitForSeconds(1.5f);
    Unlock();
}

    private void Unlock()
    {
        isUnlocked = true;
        Debug.Log("Item unlocked!");
        gameObject.SetActive(false);
    }
}
