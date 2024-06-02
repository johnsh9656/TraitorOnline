using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightMenu : MonoBehaviour
{
    //[SerializeField] private Timer timer;

    [Header("Gradients")]
    [Header("Day")]
    [SerializeField] private Gradient fogGradient;
    [SerializeField] private Gradient ambientGradient;
    [SerializeField] private Gradient directionLightGradient;
    [SerializeField] private Gradient daySkyboxTintGradient;

    [Header("Environmental Assets")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Material daySkyboxMaterial;
    [SerializeField] private Material nightSkyboxMaterial;
    [SerializeField] private Light fireLight;
    [SerializeField] private ParticleSystem fireFX;

    [Header("Variables")]
    [SerializeField] private float dayLength = 60f;
    [SerializeField] private float currentTime = 20f;

    private float interpolationPoint = 0;
    private float lerpValue = 0;
    private float sunPosition = 80f;
    private bool isNight = false;

    private void Update()
    {
        currentTime += Time.deltaTime;

        interpolationPoint = currentTime / dayLength;

        if (currentTime >= dayLength) { currentTime = 0; }

        UpdateDayNightCycle();
        HandleSunPosition();
        RotateSkybox();
    }

    // handles skybox, lighting settings each frame
    private void UpdateDayNightCycle()
    {
        lerpValue = Mathf.Lerp(0, 1, interpolationPoint);

        RenderSettings.fogColor = fogGradient.Evaluate(lerpValue);
        RenderSettings.ambientLight = ambientGradient.Evaluate(lerpValue);
        directionalLight.color = directionLightGradient.Evaluate(lerpValue);
        skyboxMaterial.SetColor("_Tint", daySkyboxTintGradient.Evaluate(lerpValue));
    }

    // handles position of directional light each frame
    private void HandleSunPosition()
    {
        if (!isNight)
        {
            float sunLerpValue = currentTime / (dayLength * 0.7f);
            sunPosition = Mathf.Lerp(180, 0, sunLerpValue);
            directionalLight.transform.rotation = Quaternion.Euler(sunPosition, 30f, 0f);

            if (interpolationPoint >= 0.7f)
            {
                isNight = true;
                skyboxMaterial = nightSkyboxMaterial;
                RenderSettings.skybox = nightSkyboxMaterial;
                directionalLight.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            }
        }
        else 
        {
            if (interpolationPoint >= 0.96f)
            {
                skyboxMaterial = daySkyboxMaterial;
                RenderSettings.skybox = daySkyboxMaterial;
            }
            else if (interpolationPoint < 0.6f)
            {
                isNight = false;
            }
        }
    }

    // rotates skybox continuously
    private void RotateSkybox()
    {
        float currentRotation = skyboxMaterial.GetFloat("_Rotation");
        float newRotation = currentRotation + 1.2f * Time.deltaTime;
        newRotation = Mathf.Repeat(newRotation, 360f);
        skyboxMaterial.SetFloat("_Rotation", newRotation);
    }
}
