using UnityEngine;
using System.Collections.Generic;

public class SymbolPairManager : MonoBehaviour
{
    public SymbolLibrary symbolLibrary;
    public List<UnlockableItem> doors;
    public GameObject keyPrefab;

    [Header("Key Spawn Points")]
    public Transform[] keySpawnPoints;

    private List<SymbolPair> symbolPairs = new List<SymbolPair>();

    void Start()
    {
        GenerateSymbolPairs();
    }

    void GenerateSymbolPairs()
    {
        if (symbolLibrary == null || symbolLibrary.availableSymbols.Length == 0 ||
            doors.Count == 0 || keySpawnPoints.Length == 0 || keyPrefab == null)
        {
            Debug.LogWarning("SymbolPairManager is missing data.");
            return;
        }

        int pairCount = Mathf.Min(doors.Count, keySpawnPoints.Length, symbolLibrary.availableSymbols.Length);
        List<SymbolData> shuffledSymbols = new List<SymbolData>(symbolLibrary.availableSymbols);
        ShuffleList(shuffledSymbols);
        ShuffleList(doors); // Optional: randomize door assignments
        ShuffleList(keySpawnPoints); // Randomize spawn locations

        for (int i = 0; i < pairCount; i++)
        {
            // Instantiate the key at a spawn point
            GameObject keyObj = Instantiate(keyPrefab, keySpawnPoints[i].position, keySpawnPoints[i].rotation);
            CollectibleItem key = keyObj.GetComponent<CollectibleItem>();
            if (key == null)
            {
                Debug.LogError("Key prefab is missing CollectibleItem component.");
                continue;
            }

            SymbolData symbol = shuffledSymbols[i];
            UnlockableItem door = doors[i];

            // Apply symbol to both
            key.GetComponent<SymbolHandler>().ApplySymbol(symbol);
            key.itemName = symbol.name;

            door.GetComponent<SymbolHandler>().ApplySymbol(symbol);
            door.requiredItemName = symbol.name;

            Debug.Log($"Assigned symbol '{symbol.name}' to Key: {key.name} and Door: {door.name}");

            symbolPairs.Add(new SymbolPair { key = key, door = door, symbol = symbol });
        }
    }

    void ShuffleList<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
