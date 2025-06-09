using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

public class NavMeshBaker : EditorWindow
{
    [MenuItem("Tools/Bake NavMesh")]
    static void BakeNavMesh()
    {
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("NavMesh baked!");
    }
}
