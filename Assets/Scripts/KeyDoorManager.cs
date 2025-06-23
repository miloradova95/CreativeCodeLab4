using UnityEngine;
using System.Collections.Generic;

public class KeyDoorManager : MonoBehaviour
{
    [Header("Symbol Library + Key Prefab")]
    public SymbolLibrary symbolLibrary;
    public GameObject keyPrefab;

    [Header("Door List + Key Spawn Points List")]
    public List<KeySpawns> list;

    private List<SymbolPair> symbolPairs = new List<SymbolPair>();

    void Start()
    {
        GenerateSymbolPairs();
    }

    void GenerateSymbolPairs()
    {
        if (symbolLibrary == null || keyPrefab == null || list.Count == 0)
        {
            Debug.LogWarning("Missing required references.");
            return;
        }

        List<SymbolData> shuffledSymbols = new List<SymbolData>(symbolLibrary.availableSymbols);
        ShuffleList(shuffledSymbols);

        int pairCount = Mathf.Min(list.Count, shuffledSymbols.Count);

        for (int i = 0; i < pairCount; i++)
        {
            KeySpawns pair = list[i];
            UnlockableItem door = pair.door;
            Transform[] spawnOptions = pair.keySpawnPoints;

            if (door == null || spawnOptions == null || spawnOptions.Length == 0)
            {
                Debug.LogWarning($"KeySpawns entry {i} is missing data.");
                continue;
            }

            // Pick a random spawn point for this key
            Transform chosenSpawn = spawnOptions[Random.Range(0, spawnOptions.Length)];

            // Instantiate key at chosen location
            GameObject keyObj = Instantiate(keyPrefab, chosenSpawn.position, chosenSpawn.rotation);
            CollectibleItem key = keyObj.GetComponent<CollectibleItem>();

            if (key == null)
            {
                Debug.LogError("Key prefab missing CollectibleItem component.");
                continue;
            }

            // Assign and apply symbol
            SymbolData symbol = shuffledSymbols[i];

            key.GetComponent<SymbolHandler>().ApplySymbol(symbol);
            key.itemName = symbol.name;

            door.GetComponent<SymbolHandler>().ApplySymbol(symbol);
            door.requiredItemName = symbol.name;

            symbolPairs.Add(new SymbolPair { key = key, door = door, symbol = symbol });

            Debug.Log($"Linked Key '{key.name}' with Door '{door.name}' using Symbol '{symbol.name}'.");
        }
    }

    void ShuffleList<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
