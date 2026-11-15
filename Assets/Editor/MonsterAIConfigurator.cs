using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MonsterAIConfigurator
{
    private const string ControllerFolder = "Assets/Monster/Controllers";

    private readonly struct MonsterDefinition
    {
        public readonly string Name;
        public readonly string ModelPath;
        public readonly string PrefabPath;

        public MonsterDefinition(string name, string modelPath, string prefabPath)
        {
            Name = name;
            ModelPath = modelPath;
            PrefabPath = prefabPath;
        }
    }

    private static readonly MonsterDefinition[] Monsters =
    {
        new MonsterDefinition("Skeleton", "Assets/Monster/Skeleton.fbx", "Assets/Prefabs/Monster/Skeleton.prefab"),
        new MonsterDefinition("Golem", "Assets/Monster/Golem.fbx", "Assets/Prefabs/Monster/Golem.prefab"),
        new MonsterDefinition("DevilTall1", "Assets/Monster/DevilTall1.fbx", "Assets/Prefabs/Monster/DevilTall1.prefab")
    };

    [MenuItem("Tools/Setup Monster AI")]
    public static void Setup()
    {
        EnsureControllerFolder();

        foreach (MonsterDefinition monster in Monsters)
        {
            ConfigureAnimationLooping(monster.ModelPath);
            AnimatorController controller = CreateController(monster);
            ConfigurePrefab(monster, controller);
        }

        ConfigureSceneTargets();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Monster AI setup complete: monsters target the castle, switch to the player in range, and use walk/attack animations.");
    }

    [MenuItem("Tools/Fix Monster Attack Range")]
    public static void FixMonsterAttackRange()
    {
        MonsterAI[] monsters = UnityEngine.Object.FindObjectsByType<MonsterAI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (MonsterAI monster in monsters)
        {
            SerializedObject serializedMonster = new SerializedObject(monster);
            serializedMonster.FindProperty("attackRange").floatValue = 1.5f;
            serializedMonster.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(monster);
            EditorUtility.SetDirty(monster);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"Monster attack physics fixed: {monsters.Length} scene monsters now use a 1.5 surface range.");
    }

    private static void EnsureControllerFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Monster"))
        {
            AssetDatabase.CreateFolder("Assets", "Monster");
        }

        if (!AssetDatabase.IsValidFolder(ControllerFolder))
        {
            AssetDatabase.CreateFolder("Assets/Monster", "Controllers");
        }
    }

    private static void ConfigureAnimationLooping(string modelPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
        {
            return;
        }

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        bool changed = importer.clipAnimations.Length == 0;

        foreach (ModelImporterClipAnimation clip in clips)
        {
            bool shouldLoop = ContainsAny(clip.name, "idle", "walk", "run");
            if (clip.loopTime != shouldLoop)
            {
                clip.loopTime = shouldLoop;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    private static AnimatorController CreateController(MonsterDefinition monster)
    {
        string controllerPath = $"{ControllerFolder}/{monster.Name}AI.controller";
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(monster.ModelPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        AnimationClip idle = FindClip(clips, "idle")
            ?? FindClip(clips, "tpose")
            ?? clips.FirstOrDefault();
        AnimationClip walk = FindClip(clips, "walk", "run");
        AnimationClip attack = FindClip(clips, "attack", "hit", "punch", "slash", "bite");

        Debug.Log($"{monster.Name} clips: {string.Join(", ", clips.Select(clip => clip.name))}");

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle");
        AnimatorState walkState = machine.AddState("Walk");
        AnimatorState attackState = machine.AddState("Attack");

        idleState.motion = idle;
        walkState.motion = walk ?? idle;
        attackState.motion = attack ?? idle;
        machine.defaultState = idleState;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.12f;
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.12f;
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

        AnimatorStateTransition anyToAttack = machine.AddAnyStateTransition(attackState);
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.08f;
        anyToAttack.canTransitionToSelf = false;
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;
        attackToIdle.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip FindClip(AnimationClip[] clips, params string[] terms)
    {
        return clips.FirstOrDefault(clip => ContainsAny(clip.name, terms));
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static void ConfigurePrefab(MonsterDefinition monster, RuntimeAnimatorController controller)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(monster.PrefabPath);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
            }

            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.enabled = true;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                EditorUtility.SetDirty(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }

            MonsterAI ai = root.GetComponent<MonsterAI>();
            if (ai == null)
            {
                ai = root.AddComponent<MonsterAI>();
            }

            SerializedObject aiObject = new SerializedObject(ai);
            aiObject.FindProperty("animatorController").objectReferenceValue = controller;
            aiObject.FindProperty("attackRange").floatValue = 1.5f;
            aiObject.ApplyModifiedPropertiesWithoutUndo();

            Damageable health = root.GetComponent<Damageable>();
            if (health == null)
            {
                health = root.AddComponent<Damageable>();
            }

            SerializedObject healthObject = new SerializedObject(health);
            healthObject.FindProperty("maxHealth").floatValue = monster.Name == "Golem" ? 250f : 120f;
            healthObject.FindProperty("destroyOnDeath").boolValue = true;
            healthObject.ApplyModifiedPropertiesWithoutUndo();

            Rigidbody body = root.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }

            PrefabUtility.SaveAsPrefabAsset(root, monster.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureSceneTargets()
    {
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            Damageable playerHealth = player.GetComponent<Damageable>();
            if (playerHealth == null)
            {
                playerHealth = player.gameObject.AddComponent<Damageable>();
            }

            SetHealth(playerHealth, 100f, false);
            EditorUtility.SetDirty(player.gameObject);
        }

        Transform castle = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name.Equals("building_castle_blue", StringComparison.OrdinalIgnoreCase));

        if (castle != null)
        {
            CastleTarget marker = castle.GetComponent<CastleTarget>();
            if (marker == null)
            {
                marker = castle.gameObject.AddComponent<CastleTarget>();
            }

            Damageable castleHealth = castle.GetComponent<Damageable>();
            if (castleHealth == null)
            {
                castleHealth = castle.gameObject.AddComponent<Damageable>();
            }

            SetHealth(castleHealth, 1000f, false);
            EditorUtility.SetDirty(marker);
            EditorUtility.SetDirty(castle.gameObject);
        }
        else
        {
            Debug.LogWarning("Monster AI setup could not find a scene object named building_castle_blue.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void SetHealth(Damageable health, float maxHealth, bool destroyOnDeath)
    {
        SerializedObject serializedHealth = new SerializedObject(health);
        serializedHealth.FindProperty("maxHealth").floatValue = maxHealth;
        serializedHealth.FindProperty("destroyOnDeath").boolValue = destroyOnDeath;
        serializedHealth.ApplyModifiedPropertiesWithoutUndo();
    }
}
