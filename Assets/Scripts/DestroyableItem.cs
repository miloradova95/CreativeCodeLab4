using UnityEngine;

public class DestroyableItem : Item
{
    public override void Interact()
    {
        gameObject.SetActive(false);
    }
}
