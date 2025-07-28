// Assets/Editor/RemoveMissingScripts.cs
using UnityEngine;
using UnityEditor;

public static class RemoveMissingScripts
{
    // Main menu item under Tools
    [MenuItem("Tools/Remove Missing Scripts", priority = 200)]
    public static void RemoveFromSelection()
    {
        var selection = Selection.gameObjects;
        if (selection.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }

        int totalRemoved = 0;
        foreach (var go in selection)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
                Debug.Log($"[{go.name}] stripped {removed} missing script(s).");
            totalRemoved += removed;
        }

        Debug.Log($"Removed {totalRemoved} missing scripts from {selection.Length} GameObject(s).");
    }

    // Add right-click context menu in the Hierarchy
    [MenuItem("GameObject/Remove Missing Scripts", false, 0)]
    public static void RemoveFromContext(MenuCommand cmd)
    {
        // cmd.context is the GameObject you right-clicked on
        var go = cmd.context as GameObject;
        if (go == null)
            return;

        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        if (removed > 0)
            Debug.Log($"[{go.name}] stripped {removed} missing script(s).");
    }
}
