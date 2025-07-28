// File: Assets/Editor/FindAllVMColliderInstances.cs

using UnityEngine;
using UnityEditor;
using ATOS.Bridge;   // ← import the namespace where VMCollider is defined

public static class FindAllVMColliderInstances
{
    // This menu item will show up under the “Tools” menu in the Unity Editor.
    [MenuItem("Tools/Find All VMCollider Instances")]
    private static void FindAllInstances()
    {
        // Because VMCollider lives in ATOS.Bridge, and we did 'using ATOS.Bridge',
        // UnityEditor can now resolve VMCollider here.
        VMCollider[] allCopies = Object.FindObjectsOfType<VMCollider>();

        if (allCopies.Length == 0)
        {
            Debug.LogWarning("No VMCollider instances found in the open scenes.");
            return;
        }

        Debug.Log($"Found {allCopies.Length} VMCollider instance(s):");
        foreach (var instance in allCopies)
        {
            // Passing instance.gameObject as the second argument ensures
            // clicking the log entry will highlight that GameObject in the Hierarchy.
            Debug.Log($" • VMCollider on GameObject '{instance.gameObject.name}'", instance.gameObject);
        }
    }
}
