using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Damageable))]
public sealed class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField, Min(1f)] private float respawnDelay = 10f;
    [SerializeField] private Vector3 spawnPosition = new Vector3(430.1f, 1.57f, -973.2f);
    [SerializeField] private Quaternion spawnRotation = Quaternion.identity;

    private Damageable health;
    private PlayerMovement movement;
    private CharacterController characterController;
    private MouseMovement mouseMovement;
    private Gun[] guns;
    private Renderer[] playerRenderers;
    private Collider[] playerColliders;
    private bool[] colliderStates;
    private TMP_Text countdownText;
    private bool isRespawning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnPlayer()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null && player.GetComponent<PlayerRespawn>() == null)
        {
            player.gameObject.AddComponent<PlayerRespawn>();
        }
    }

    private void Awake()
    {
        health = GetComponent<Damageable>();
        movement = GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
        mouseMovement = GetComponentInChildren<MouseMovement>(true);
        guns = GetComponentsInChildren<Gun>(true);
        playerRenderers = GetComponentsInChildren<Renderer>(true);
        playerColliders = GetComponentsInChildren<Collider>(true);
        colliderStates = playerColliders.Select(item => item.enabled).ToArray();

        Transform marker = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name.Equals(
                "Player Spawn Point",
                System.StringComparison.OrdinalIgnoreCase));

        if (marker != null)
        {
            spawnPosition = marker.position;
            spawnRotation = marker.rotation;
        }

        CreateCountdownUI();
    }

    private void Update()
    {
        if (!isRespawning && health != null && health.IsDead)
        {
            StartCoroutine(RespawnCountdown());
        }
    }

    private IEnumerator RespawnCountdown()
    {
        isRespawning = true;
        SetPlayerActive(false);
        countdownText.gameObject.SetActive(true);

        float remaining = respawnDelay;
        while (remaining > 0f)
        {
            countdownText.text = $"Spawn in {Mathf.CeilToInt(remaining)}s";
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        RespawnPlayer();
        countdownText.gameObject.SetActive(false);
        isRespawning = false;
    }

    private void RespawnPlayer()
    {
        Vector3 finalPosition = GetGroundedSpawnPosition();
        transform.SetPositionAndRotation(finalPosition, spawnRotation);
        health.RestoreFullHealth();

        if (movement != null)
        {
            movement.ResetMovement();
        }

        SetPlayerActive(true);
    }

    private Vector3 GetGroundedSpawnPosition()
    {
        if (characterController == null)
        {
            return spawnPosition;
        }

        float scaleY = Mathf.Abs(transform.lossyScale.y);
        float feetOffset = (characterController.height * 0.5f - characterController.center.y) * scaleY;

        // The terrain objects are on Default while PlayerMovement's groundMask
        // only contains Ground. Sample terrain directly so respawning never
        // depends on that mismatched layer mask.
        Terrain[] terrains = FindObjectsByType<Terrain>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        foreach (Terrain terrain in terrains)
        {
            Vector3 terrainOrigin = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideX = spawnPosition.x >= terrainOrigin.x
                && spawnPosition.x <= terrainOrigin.x + terrainSize.x;
            bool insideZ = spawnPosition.z >= terrainOrigin.z
                && spawnPosition.z <= terrainOrigin.z + terrainSize.z;

            if (insideX && insideZ)
            {
                float groundY = terrain.SampleHeight(spawnPosition) + terrainOrigin.y;
                return new Vector3(
                    spawnPosition.x,
                    groundY + feetOffset + 0.1f,
                    spawnPosition.z);
            }
        }

        Vector3 rayOrigin = spawnPosition + Vector3.up * 200f;
        if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                500f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return new Vector3(
                spawnPosition.x,
                hit.point.y + feetOffset + 0.1f,
                spawnPosition.z);
        }

        // Last-resort fallback treats the saved marker position as foot level
        // instead of placing the center of the large controller underground.
        return spawnPosition + Vector3.up * (feetOffset + 0.1f);
    }

    private void SetPlayerActive(bool active)
    {
        if (movement != null)
        {
            movement.enabled = active;
        }

        if (mouseMovement != null)
        {
            mouseMovement.enabled = active;
        }

        foreach (Gun gun in guns)
        {
            gun.enabled = active;
        }

        foreach (Renderer playerRenderer in playerRenderers)
        {
            playerRenderer.enabled = active;
        }

        for (int index = 0; index < playerColliders.Length; index++)
        {
            playerColliders[index].enabled = active && colliderStates[index];
        }
    }

    private void CreateCountdownUI()
    {
        GameObject canvasObject = new GameObject("Respawn Countdown Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("Respawn Countdown Text");
        textObject.transform.SetParent(canvasObject.transform, false);
        countdownText = textObject.AddComponent<TextMeshProUGUI>();
        countdownText.font = TMP_Settings.defaultFontAsset;
        countdownText.fontSize = 52f;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.color = Color.white;
        countdownText.text = string.Empty;

        RectTransform rect = countdownText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(900f, 140f);
        countdownText.gameObject.SetActive(false);
    }
}
