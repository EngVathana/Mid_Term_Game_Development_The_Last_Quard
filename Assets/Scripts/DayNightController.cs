using System.Collections;
using UnityEngine;

/// <summary>Owns the environment lighting. GameManager only asks for day or night.</summary>
public sealed class DayNightController : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField, Min(0.1f)] private float transitionDuration = 2f;

    [Header("Night")]
    [SerializeField] private Vector3 nightSunRotation = new Vector3(330f, 170f, 0f);
    [SerializeField] private Color nightSunColor = new Color(0.35f, 0.45f, 0.7f);
    [SerializeField, Range(0f, 2f)] private float nightSunIntensity = 0.15f;
    [SerializeField] private Color nightAmbientColor = new Color(0.035f, 0.045f, 0.09f);

    private Quaternion dayRotation;
    private Color daySunColor;
    private float daySunIntensity;
    private Color dayAmbientColor;
    private Coroutine transition;

    private void Awake()
    {
        if (sun == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Light candidate in lights)
            {
                if (candidate.type == LightType.Directional)
                {
                    sun = candidate;
                    break;
                }
            }
        }

        if (sun != null)
        {
            dayRotation = sun.transform.rotation;
            daySunColor = sun.color;
            daySunIntensity = sun.intensity;
        }

        dayAmbientColor = RenderSettings.ambientLight;
    }

    public void SetDay(bool instant = false) => BeginTransition(false, instant);
    public void SetNight(bool instant = false) => BeginTransition(true, instant);

    private void BeginTransition(bool toNight, bool instant)
    {
        if (transition != null)
        {
            StopCoroutine(transition);
        }

        if (instant)
        {
            Apply(toNight ? 1f : 0f);
            transition = null;
            return;
        }

        transition = StartCoroutine(Transition(toNight));
    }

    private IEnumerator Transition(bool toNight)
    {
        float start = GetNightBlend();
        float end = toNight ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float amount = Mathf.SmoothStep(start, end, Mathf.Clamp01(elapsed / transitionDuration));
            Apply(amount);
            yield return null;
        }

        Apply(end);
        transition = null;
    }

    private float GetNightBlend()
    {
        if (sun == null || Mathf.Approximately(daySunIntensity, nightSunIntensity))
        {
            return 0f;
        }

        return Mathf.InverseLerp(daySunIntensity, nightSunIntensity, sun.intensity);
    }

    private void Apply(float nightBlend)
    {
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Slerp(dayRotation, Quaternion.Euler(nightSunRotation), nightBlend);
            sun.color = Color.Lerp(daySunColor, nightSunColor, nightBlend);
            sun.intensity = Mathf.Lerp(daySunIntensity, nightSunIntensity, nightBlend);
        }

        RenderSettings.ambientLight = Color.Lerp(dayAmbientColor, nightAmbientColor, nightBlend);
    }
}
