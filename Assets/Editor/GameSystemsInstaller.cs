#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameSystemsInstaller
{
    [MenuItem("Tools/Install Game Systems")]
    public static void Install()
    {
        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        GameObject systems;
        if (manager == null)
        {
            systems = new GameObject("Game Systems");
            Undo.RegisterCreatedObjectUndo(systems, "Install Game Systems");
            manager = systems.AddComponent<GameManager>();
        }
        else
        {
            systems = manager.gameObject;
        }

        DayNightController dayNight = systems.GetComponent<DayNightController>();
        if (dayNight == null)
        {
            dayNight = Undo.AddComponent<DayNightController>(systems);
        }

        SerializedObject managerObject = new SerializedObject(manager);
        managerObject.FindProperty("dayNight").objectReferenceValue = dayNight;

        CastleTarget castle = Object.FindFirstObjectByType<CastleTarget>();
        if (castle != null)
        {
            managerObject.FindProperty("castleHealth").objectReferenceValue = castle.GetComponent<Damageable>();
        }

        GameObject skeleton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Skeleton.prefab");
        GameObject devil = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/DevilTall1 1.prefab");
        GameObject golem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Golem.prefab");

        SerializedProperty waves = managerObject.FindProperty("waves");
        if (waves.arraySize == 0)
        {
            waves.arraySize = 3;
            ConfigureWave(waves.GetArrayElementAtIndex(0), "Wave 1", skeleton, 5, devil, 2, golem, 1);
            ConfigureWave(waves.GetArrayElementAtIndex(1), "Wave 2", skeleton, 8, devil, 3, golem, 2);
            ConfigureWave(waves.GetArrayElementAtIndex(2), "Wave 3", skeleton, 12, devil, 5, golem, 3);
        }

        managerObject.ApplyModifiedProperties();

        // The scene contained several monsters that were used while building the
        // AI. Waves now own enemy creation, so keep those examples in the scene
        // but deactivate their complete prefab roots.
        int disabledEnemyCount = 0;
        HashSet<GameObject> disabledRoots = new HashSet<GameObject>();
        foreach (MonsterAI monster in Object.FindObjectsByType<MonsterAI>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(monster.gameObject);
            if (root == null)
            {
                root = monster.transform.root.gameObject;
            }

            if (disabledRoots.Add(root))
            {
                Undo.RecordObject(root, "Disable Pre-Placed Enemy");
                root.SetActive(false);
                EditorUtility.SetDirty(root);
                disabledEnemyCount++;
            }
        }

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(dayNight);
        EditorSceneManager.MarkSceneDirty(systems.scene);
        Selection.activeGameObject = systems;

        int spawnCount = 0;
        foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (item.name.StartsWith("enemy spawn ", System.StringComparison.OrdinalIgnoreCase))
            {
                spawnCount++;
            }
        }

        Debug.Log($"Game systems installed. Found {spawnCount} enemy spawn points and disabled {disabledEnemyCount} pre-placed enemy roots. Edit Waves on the GameManager component.", manager);
    }

    private static void ConfigureWave(SerializedProperty wave, string name,
        GameObject first, int firstCount, GameObject second, int secondCount, GameObject third, int thirdCount)
    {
        wave.FindPropertyRelative("waveName").stringValue = name;
        SerializedProperty enemies = wave.FindPropertyRelative("enemies");
        enemies.arraySize = 3;
        ConfigureEnemy(enemies.GetArrayElementAtIndex(0), first, firstCount);
        ConfigureEnemy(enemies.GetArrayElementAtIndex(1), second, secondCount);
        ConfigureEnemy(enemies.GetArrayElementAtIndex(2), third, thirdCount);
    }

    private static void ConfigureEnemy(SerializedProperty entry, GameObject prefab, int count)
    {
        entry.FindPropertyRelative("enemyPrefab").objectReferenceValue = prefab;
        entry.FindPropertyRelative("count").intValue = count;
    }
}
#endif
