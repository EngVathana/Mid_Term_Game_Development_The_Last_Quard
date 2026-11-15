using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AK47PlayerSetup
{
    private const string Ak47PrefabPath = "Assets/PolyOne/Free Gun/Prefabs/SM_Ak47.prefab";

    [MenuItem("Tools/Setup Player AK47")]
    public static void Setup()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("AK47 setup could not find the Player object.");
            return;
        }

        Gun oldGun = UnityEngine.Object.FindObjectsByType<Gun>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(component => component.gameObject.name.Equals("Gun", StringComparison.OrdinalIgnoreCase));

        GameObject ak47 = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Select(item => item.gameObject)
            .Where(item =>
                item.scene.IsValid() &&
                (item.name.Equals("AK47", StringComparison.OrdinalIgnoreCase) ||
                 item.name.Equals("Ak47", StringComparison.OrdinalIgnoreCase) ||
                 item.name.Equals("SM_Ak47", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(GetHierarchyDepth)
            .FirstOrDefault();

        if (ak47 == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ak47PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"AK47 setup could not load {Ak47PrefabPath}.");
                return;
            }

            ak47 = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(ak47, "Create Player AK47");

            if (oldGun != null)
            {
                ak47.transform.SetPositionAndRotation(oldGun.transform.position, oldGun.transform.rotation);
            }
        }

        Undo.SetTransformParent(ak47.transform, player.transform, "Parent AK47 to Player");
        ak47.name = "AK47";

        Gun[] existingAk47Guns = ak47.GetComponentsInChildren<Gun>(true);
        Gun settingsSource = oldGun != null ? oldGun : existingAk47Guns.FirstOrDefault();
        Gun ak47Gun = ak47.GetComponent<Gun>();
        if (ak47Gun == null)
        {
            ak47Gun = Undo.AddComponent<Gun>(ak47);
        }

        foreach (Gun duplicate in existingAk47Guns)
        {
            if (duplicate == ak47Gun)
            {
                continue;
            }

            Undo.RecordObject(duplicate, "Disable duplicate AK47 shooting");
            duplicate.enabled = false;
            EditorUtility.SetDirty(duplicate);
        }

        Camera playerCamera = settingsSource != null && settingsSource.playerCamera != null
            ? settingsSource.playerCamera
            : player.GetComponentInChildren<Camera>(true);

        Undo.RecordObject(ak47Gun, "Configure AK47 shooting");
        ak47Gun.playerCamera = playerCamera;
        ak47Gun.shootingRange = settingsSource != null ? settingsSource.shootingRange : 1000f;
        ak47Gun.damage = settingsSource != null ? settingsSource.damage : 50;
        EditorUtility.SetDirty(ak47Gun);

        if (oldGun != null && oldGun.gameObject != ak47)
        {
            Undo.DestroyObjectImmediate(oldGun.gameObject);
        }

        Selection.activeGameObject = ak47;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Player AK47 setup complete: attached to Player, shooting enabled, and old Gun removed.");
    }

    private static int GetHierarchyDepth(GameObject item)
    {
        int depth = 0;
        Transform parent = item.transform.parent;
        while (parent != null)
        {
            depth++;
            parent = parent.parent;
        }

        return depth;
    }
}
