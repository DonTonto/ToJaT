using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class UnparentAllChildrenRecursive
{
    [MenuItem("Tools/Unparent All Children Recursively")]
    private static void UnparentAll()
    {
        // 1) Gather every descendant of each selected GameObject
        var allDescendants = new List<Transform>();
        foreach (var go in Selection.gameObjects)
            CollectDescendants(go.transform, allDescendants);

        // 2) Sort by depth (deepest first) so children get unparented before their parents
        allDescendants.Sort((a, b) => GetDepth(b).CompareTo(GetDepth(a)));

        // 3) Unparent each transform (with undo support)
        for (int i = 0; i < allDescendants.Count; i++)
        {
            var t = allDescendants[i];
            Undo.SetTransformParent(t, null, "Unparent Descendants");
        }

        Debug.Log($"Unparented {allDescendants.Count} transforms.");
    }

    // Recursively add every child to the list
    private static void CollectDescendants(Transform parent, List<Transform> list)
    {
        foreach (Transform child in parent)
        {
            list.Add(child);
            CollectDescendants(child, list);
        }
    }

    // Helper to compute hierarchy depth
    private static int GetDepth(Transform t)
    {
        int depth = 0;
        while (t.parent != null)
        {
            depth++;
            t = t.parent;
        }
        return depth;
    }
}