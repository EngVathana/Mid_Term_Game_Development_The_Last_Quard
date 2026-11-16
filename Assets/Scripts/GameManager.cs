using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    Home,
    Preparation,
    WaveActive,
    Victory,
    Defeat
}

[Serializable]
public sealed class WaveEnemyEntry
{
    public GameObject enemyPrefab;
    [Min(0)] public int count = 1;
}

[Serializable]
public sealed class EnemyWave
{
    public string waveName = "Wave";
    public List<WaveEnemyEntry> enemies = new List<WaveEnemyEntry>();
}

/// <summary>Coordinates the match while individual components keep their own jobs.</summary>
public sealed class GameManager : MonoBehaviour
{
    [Header("Waves - change enemy type and count here")]
    [SerializeField] private List<EnemyWave> waves = new List<EnemyWave>();
    [SerializeField, Min(0.05f)] private float spawnInterval = 0.6f;
    [SerializeField, Min(0f)] private float spawnHeightOffset = 0.25f;
    [SerializeField, Min(0.1f)] private float holdZDuration = 1.5f;
    [SerializeField, Min(0f)] private float timeAfterWave = 1.5f;

    [Header("Scene References (automatic if empty)")]
    [SerializeField] private DayNightController dayNight;
    [SerializeField] private Damageable castleHealth;
    private static readonly Vector3[] EnemySpawnPositions =
    {
        new Vector3(235.4599f, 0.5f, 736.773f),
        new Vector3(286.88f, 0.57f, 737.68f),
        new Vector3(333.93f, 0.57f, 737.68f),
        new Vector3(381.96f, 0.57f, 737.68f),
        new Vector3(434.05f, 0.57f, 737.68f),
        new Vector3(483.39f, 0.57f, 737.68f),
        new Vector3(531.5f, 0.57f, 737.68f),
        new Vector3(584.39f, 0.57f, 737.68f),
        new Vector3(633.54f, 0.57f, 737.68f),
        new Vector3(684.25f, 0.57f, 737.68f),
        new Vector3(733.8f, 0.57f, 737.68f)
    };

    public GameState State { get; private set; } = GameState.Home;
    public int CurrentWaveNumber => currentWaveIndex + 1;

    private static bool startImmediatelyAfterReload;

    private int currentWaveIndex;
    private int enemiesAlive;
    private bool isSpawning;
    private bool waveEnding;
    private float holdProgress;
    private PlayerMovement playerMovement;
    private MouseMovement mouseMovement;
    private Gun[] guns = Array.Empty<Gun>();

    private Canvas gameCanvas;
    private GameObject homePanel;
    private GameObject hudPanel;
    private GameObject victoryPanel;
    private GameObject defeatPanel;
    private TMP_Text stateText;
    private TMP_Text enemyCountText;
    private TMP_Text holdText;
    private RectTransform holdFill;

    private readonly Color panelColor = new Color(0.015f, 0.025f, 0.04f, 0.94f);
    private readonly Color gold = new Color(0.95f, 0.72f, 0.2f);

    private void Awake()
    {
        ResolveSceneReferences();
        CreateUI();

        if (castleHealth != null)
        {
            castleHealth.Died += HandleCastleDestroyed;
        }
    }

    private void Start()
    {
        dayNight?.SetDay(true);
        if (startImmediatelyAfterReload)
        {
            startImmediatelyAfterReload = false;
            StartGame();
        }
        else
        {
            ShowHome();
        }
    }

    private void OnDestroy()
    {
        if (castleHealth != null)
        {
            castleHealth.Died -= HandleCastleDestroyed;
        }
    }

    private void Update()
    {
        if (State != GameState.Preparation)
        {
            return;
        }

        if (Input.GetKey(KeyCode.Z))
        {
            holdProgress = Mathf.Min(holdZDuration, holdProgress + Time.unscaledDeltaTime);
            if (holdProgress >= holdZDuration)
            {
                holdProgress = 0f;
                BeginCurrentWave();
            }
        }
        else
        {
            holdProgress = Mathf.MoveTowards(holdProgress, 0f, Time.unscaledDeltaTime * 2f);
        }

        UpdateHoldDisplay();
    }

    public void StartGame()
    {
        currentWaveIndex = 0;
        enemiesAlive = 0;
        holdProgress = 0f;
        waveEnding = false;
        homePanel.SetActive(false);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        hudPanel.SetActive(true);
        SetPlayerInput(true);
        EnterPreparation();
    }

    private void ShowHome()
    {
        State = GameState.Home;
        SetPlayerInput(false);
        homePanel.SetActive(true);
        hudPanel.SetActive(false);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void EnterPreparation()
    {
        if (currentWaveIndex >= waves.Count)
        {
            ShowVictory();
            return;
        }

        State = GameState.Preparation;
        dayNight?.SetDay();
        holdProgress = 0f;
        stateText.text = $"DAY  -  PREPARE FOR WAVE {CurrentWaveNumber} / {waves.Count}";
        enemyCountText.text = "CASTLE DEFENSE READY";
        holdText.gameObject.SetActive(true);
        holdText.text = $"HOLD Z TO START WAVE {CurrentWaveNumber}";
        UpdateHoldDisplay();
    }

    private void BeginCurrentWave()
    {
        if (State != GameState.Preparation || currentWaveIndex >= waves.Count)
        {
            return;
        }

        StartCurrentWave();
    }

    private void StartCurrentWave()
    {
        State = GameState.WaveActive;
        dayNight?.SetNight();
        holdText.gameObject.SetActive(false);
        holdFill.parent.gameObject.SetActive(false);
        stateText.text = $"NIGHT  -  WAVE {CurrentWaveNumber} / {waves.Count}";
        StartCoroutine(SpawnWave(waves[currentWaveIndex]));
    }

    private IEnumerator SpawnWave(EnemyWave wave)
    {
        isSpawning = true;
        enemiesAlive = 0;
        UpdateEnemyCount();

        List<GameObject> queue = new List<GameObject>();
        foreach (WaveEnemyEntry entry in wave.enemies)
        {
            if (entry != null && entry.enemyPrefab != null)
            {
                for (int index = 0; index < Mathf.Max(0, entry.count); index++)
                {
                    queue.Add(entry.enemyPrefab);
                }
            }
        }

        // Mix enemy types and spawn locations while retaining Inspector-defined totals.
        for (int index = queue.Count - 1; index > 0; index--)
        {
            int swap = UnityEngine.Random.Range(0, index + 1);
            (queue[index], queue[swap]) = (queue[swap], queue[index]);
        }

        for (int index = 0; index < queue.Count; index++)
        {
            if (State != GameState.WaveActive)
            {
                yield break;
            }

            SpawnEnemy(queue[index], index);
            UpdateEnemyCount();
            if (spawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        isSpawning = false;
        CheckWaveComplete();
    }

    private void SpawnEnemy(GameObject prefab, int spawnIndex)
    {
        if (EnemySpawnPositions.Length == 0)
        {
            Debug.LogError("No enemy spawn positions are configured in GameManager.", this);
            return;
        }

        Vector3 spawnPosition = EnemySpawnPositions[spawnIndex % EnemySpawnPositions.Length];
        Vector3 position = spawnPosition + Vector3.up * spawnHeightOffset;
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        Enemy enemy = instance.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError($"{prefab.name} has no Enemy component and cannot be counted.", prefab);
            Destroy(instance);
            return;
        }

        enemiesAlive++;
        enemy.Died += HandleEnemyDied;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        enemy.Died -= HandleEnemyDied;
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        UpdateEnemyCount();
        CheckWaveComplete();
    }

    private void CheckWaveComplete()
    {
        if (State == GameState.WaveActive && !isSpawning && enemiesAlive == 0 && !waveEnding)
        {
            StartCoroutine(FinishWave());
        }
    }

    private IEnumerator FinishWave()
    {
        waveEnding = true;
        enemyCountText.text = $"WAVE {CurrentWaveNumber} CLEARED";
        yield return new WaitForSeconds(timeAfterWave);

        currentWaveIndex++;
        waveEnding = false;
        if (currentWaveIndex >= waves.Count)
        {
            ShowVictory();
        }
        else
        {
            // Only Wave 1 needs the player's Z confirmation. Once the attack has
            // begun, every remaining wave follows automatically without daytime.
            StartCurrentWave();
        }
    }

    private void HandleCastleDestroyed()
    {
        if (State != GameState.Victory && State != GameState.Defeat && State != GameState.Home)
        {
            ShowDefeat();
        }
    }

    private void ShowVictory()
    {
        State = GameState.Victory;
        dayNight?.SetDay();
        hudPanel.SetActive(false);
        victoryPanel.SetActive(true);
        SetEndStateControls();
    }

    private void ShowDefeat()
    {
        State = GameState.Defeat;
        hudPanel.SetActive(false);
        defeatPanel.SetActive(true);
        SetEndStateControls();
    }

    private void SetEndStateControls()
    {
        SetPlayerInput(false);
        foreach (MonsterAI monster in FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            monster.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RetryGame()
    {
        startImmediatelyAfterReload = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToHome()
    {
        startImmediatelyAfterReload = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ResolveSceneReferences()
    {
        if (dayNight == null)
        {
            dayNight = FindFirstObjectByType<DayNightController>();
        }

        if (castleHealth == null)
        {
            CastleTarget target = FindFirstObjectByType<CastleTarget>();
            castleHealth = target != null ? target.GetComponent<Damageable>() : null;
        }

        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            mouseMovement = playerMovement.GetComponentInChildren<MouseMovement>(true);
            guns = playerMovement.GetComponentsInChildren<Gun>(true);
        }

    }

    private void SetPlayerInput(bool active)
    {
        if (playerMovement != null) playerMovement.enabled = active;
        if (mouseMovement != null) mouseMovement.enabled = active;
        foreach (Gun gun in guns) gun.enabled = active;

        if (active)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void UpdateEnemyCount()
    {
        enemyCountText.text = $"ENEMIES LEFT: {enemiesAlive}";
    }

    private void UpdateHoldDisplay()
    {
        float ratio = Mathf.Clamp01(holdProgress / holdZDuration);
        holdFill.anchorMax = new Vector2(ratio, 1f);
        holdFill.parent.gameObject.SetActive(State == GameState.Preparation);
    }

    private void CreateUI()
    {
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject canvasObject = new GameObject("Game Manager UI");
        gameCanvas = canvasObject.AddComponent<Canvas>();
        gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gameCanvas.sortingOrder = 2000;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        homePanel = CreateFullPanel("Home Page", panelColor);
        CreateText(homePanel.transform, "THE LAST GUARD", 88f, gold, new Vector2(0f, 170f), new Vector2(1200f, 130f));
        CreateText(homePanel.transform, "DEFEND THE CASTLE THROUGH THREE NIGHTS", 30f, Color.white, new Vector2(0f, 60f), new Vector2(1100f, 70f));
        CreateButton(homePanel.transform, "START GAME", new Vector2(0f, -90f), StartGame);

        hudPanel = new GameObject("Wave HUD", typeof(RectTransform));
        hudPanel.transform.SetParent(gameCanvas.transform, false);
        Stretch(hudPanel.GetComponent<RectTransform>());
        stateText = CreateText(hudPanel.transform, string.Empty, 34f, Color.white, new Vector2(0f, -45f), new Vector2(1200f, 55f), new Vector2(0.5f, 1f));
        enemyCountText = CreateText(hudPanel.transform, string.Empty, 27f, gold, new Vector2(0f, -92f), new Vector2(900f, 50f), new Vector2(0.5f, 1f));
        holdText = CreateText(hudPanel.transform, string.Empty, 32f, Color.white, new Vector2(0f, 145f), new Vector2(1000f, 60f), new Vector2(0.5f, 0f));
        CreateHoldBar(hudPanel.transform);

        victoryPanel = CreateEndPanel("Victory", "VICTORY", "THE CASTLE SURVIVED ALL 3 WAVES", new Color(0.25f, 1f, 0.45f));
        defeatPanel = CreateEndPanel("Defeat", "DEFEAT", "THE CASTLE HAS FALLEN", new Color(1f, 0.2f, 0.18f));
    }

    private GameObject CreateEndPanel(string name, string title, string subtitle, Color titleColor)
    {
        GameObject panel = CreateFullPanel(name + " Panel", panelColor);
        CreateText(panel.transform, title, 96f, titleColor, new Vector2(0f, 170f), new Vector2(1100f, 140f));
        CreateText(panel.transform, subtitle, 32f, Color.white, new Vector2(0f, 55f), new Vector2(1100f, 70f));
        CreateButton(panel.transform, "PLAY AGAIN", new Vector2(0f, -70f), RetryGame);
        CreateButton(panel.transform, "MAIN MENU", new Vector2(0f, -175f), ReturnToHome);
        panel.SetActive(false);
        return panel;
    }

    private GameObject CreateFullPanel(string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(gameCanvas.transform, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private TMP_Text CreateText(Transform parent, string text, float size, Color color, Vector2 position, Vector2 dimensions, Vector2? anchor = null)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = rect.anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        return label;
    }

    private void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(380f, 78f);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.32f, 0.22f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        CreateText(buttonObject.transform, label, 30f, Color.white, Vector2.zero, rect.sizeDelta);
    }

    private void CreateHoldBar(Transform parent)
    {
        GameObject track = new GameObject("Hold Z Progress", typeof(RectTransform), typeof(Image));
        track.transform.SetParent(parent, false);
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = trackRect.anchorMax = trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.anchoredPosition = new Vector2(0f, 105f);
        trackRect.sizeDelta = new Vector2(520f, 22f);
        track.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(track.transform, false);
        holdFill = fill.GetComponent<RectTransform>();
        holdFill.anchorMin = Vector2.zero;
        holdFill.anchorMax = new Vector2(0f, 1f);
        holdFill.offsetMin = new Vector2(3f, 3f);
        holdFill.offsetMax = new Vector2(-3f, -3f);
        fill.GetComponent<Image>().color = gold;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
