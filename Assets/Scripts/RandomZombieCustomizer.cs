using UnityEngine;

public class RandomZombieCustomizer : MonoBehaviour
{
    [Header("Zombie Materials")]
    public Material shirtMaterial;
    public Material trousersMaterial;
    public Material skinMaterial;

    private Material shirtMaterialInstance;
    private Material trousersMaterialInstance;
    private Material skinMaterialInstance;

    [Header("Cover Meshes")]
    public GameObject[] armsCover;
    public GameObject[] headCover;
    public GameObject[] legsCover;
    public GameObject[] torsoCover;

    void Start()
    {
        // Clone materials so each zombie gets a unique set
        shirtMaterialInstance = new Material(shirtMaterial);
        trousersMaterialInstance = new Material(trousersMaterial);
        skinMaterialInstance = new Material(skinMaterial);

        ApplyMaterialInstances();

        RandomizeZombie();
    }

    void ApplyMaterialInstances()
    {
        var renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var rend in renderers)
        {
            var materials = rend.materials;

            if (materials.Length >= 5)
            {
                materials[1] = skinMaterialInstance;
                materials[2] = trousersMaterialInstance;
                materials[4] = shirtMaterialInstance;
            }

            rend.materials = materials;
        }

    }

    public void RandomizeZombie()
    {
        RandomizeShirtMaterial();
        RandomizeTrousersMaterial();
        RandomizeSkinMaterial();
        ToggleRandomCovers(armsCover, skinMaterialInstance);
        ToggleRandomCovers(headCover, skinMaterialInstance);
        ToggleRandomCovers(legsCover, trousersMaterialInstance);
        ToggleRandomCovers(torsoCover, shirtMaterialInstance);
    }

    void RandomizeShirtMaterial()
    {
        if (shirtMaterialInstance != null)
        {
            shirtMaterialInstance.color = Random.ColorHSV();
            shirtMaterialInstance.SetFloat("_NoiseIntensity", Random.Range(0.5f, 0.8f));
        }
    }

    void RandomizeTrousersMaterial()
    {
        if (trousersMaterialInstance != null)
        {
            trousersMaterialInstance.SetInt("_alternativeColor1", Random.value > 0.5f ? 1 : 0);
            trousersMaterialInstance.SetInt("_alternativeColor2", Random.value > 0.5f ? 1 : 0);
        }
    }

    void RandomizeSkinMaterial()
    {
        if (skinMaterialInstance != null)
        {
            skinMaterialInstance.SetInt("_Skin2", Random.value > 0.5f ? 1 : 0);
            skinMaterialInstance.SetInt("_Skin3", Random.value > 0.5f ? 1 : 0);
        }
    }

    void ToggleRandomCovers(GameObject[] covers)
    {
        foreach (var cover in covers)
        {
            if (cover != null)
            {
                cover.SetActive(Random.value > 0.5f);
            }
        }
    }

    void ToggleRandomCovers(GameObject[] covers, Material coverMaterial)
    {
        foreach (var cover in covers)
        {
            if (cover != null)
            {
                cover.SetActive(Random.value > 0.5f);

                if (cover.activeSelf)
                {
                    ApplyMaterialToObject(cover, coverMaterial);
                }
            }
        }
    }

    void ApplyMaterialToObject(GameObject obj, Material material)
    {
        if (obj == null) return;

        var rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            var materials = rend.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }
            rend.materials = materials;
        }
    }

}
