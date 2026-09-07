using OccaSoftware.SuperSimpleSkybox.Runtime;
using System.Collections;
using UnityEngine;

public class LightingController : MonoBehaviour
{
    public Sun Sun;

    public Color FogColorDay;
    public Color FogColorNight;

    public float FogFadeTime = 5f;

    private IEnumerator setColorsCoroutine;
    private Color currentFogColor, targetFogColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentFogColor = FogColorDay;
        targetFogColor = FogColorNight;

        Sun.OnRise += SetDayColors;
        Sun.OnSet += SetNightColors;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
       // Debug.Log($"sun: {Sun.GetLightAngle()}"); // 1.0 at noon, 0.0 at rise/set, -1.0 at midnight
    }

    private void SetDayColors()
    {
        targetFogColor = FogColorDay;
        setColorsCoroutine = SetFogColorsCO();
        StartCoroutine(setColorsCoroutine);
    }
    private void SetNightColors()
    {
        targetFogColor = FogColorNight;
        setColorsCoroutine = SetFogColorsCO();
        StartCoroutine(setColorsCoroutine);
    }

    private IEnumerator SetFogColorsCO()
    {
        for (float s = 0f; s < FogFadeTime; s += Time.deltaTime)
        {
            float t = s / FogFadeTime;
            RenderSettings.fogColor = Color.Lerp(currentFogColor, targetFogColor, t);
            yield return null;
        }
        currentFogColor = targetFogColor;
    }
}
