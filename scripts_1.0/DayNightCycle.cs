using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    //[SerializeField] private Timer timer;

    [Header("Gradients")]
    [Header("Day")]
    [SerializeField] private Gradient dayFogGradient;
    [SerializeField] private Gradient dayAmbientGradient;
    [SerializeField] private Gradient dayDirectionLightGradient;
    [SerializeField] private Gradient daySkyboxTintGradient;
    private Gradient[] dayGradients;
    [Header("Night")]
    [SerializeField] private Gradient nightFogGradient;
    [SerializeField] private Gradient nightAmbientGradient;
    [SerializeField] private Gradient nightDirectionLightGradient;
    [SerializeField] private Gradient nightSkyboxTintGradient;
    private Gradient[] nightGradients;
    [Header("Voting")]
    [SerializeField] private Gradient votingFogGradient;
    [SerializeField] private Gradient votingAmbientGradient;
    [SerializeField] private Gradient votingDirectionLightGradient;
    [SerializeField] private Gradient votingSkyboxTintGradient;
    private Gradient[] votingGradients;

    private Gradient[][] gradients;

    [Header("Environmental Assets")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Material daySkyboxMaterial;
    [SerializeField] private Material nightSkyboxMaterial;
    [SerializeField] private Light fireLight;
    [SerializeField] private ParticleSystem fireFX;
    private Material skyboxMaterial;

    [SerializeField] AudioSource bonfireSource;

    private float currentTime = 0;
    private float phaseLength = 0;
    private bool counting = false;
    private int phaseIndex = 0;
    private float lerpStart = 0;
    private float sunLerpStart = 120f;
    private float interpolationPoint = 0;
    private float lerpValue = 0;
    private float sunPosition = 80f;

    private void Start()
    {
        dayGradients = new Gradient[] { dayFogGradient, dayAmbientGradient, dayDirectionLightGradient, daySkyboxTintGradient };
        nightGradients = new Gradient[] { nightFogGradient, nightAmbientGradient, nightDirectionLightGradient, nightSkyboxTintGradient };
        votingGradients = new Gradient[] { votingFogGradient, votingAmbientGradient, votingDirectionLightGradient, votingSkyboxTintGradient };

        gradients = new Gradient[][] { dayGradients, nightGradients, votingGradients };

        skyboxMaterial = daySkyboxMaterial;

        SetState(2, 2f, false);
    }

    private void Update()
    {
        RotateSkybox();

        if (!counting) return;

        if (currentTime >= phaseLength)
        {
            counting = false;
            return;
        }

        currentTime += Time.deltaTime;

        interpolationPoint = currentTime / phaseLength;
        UpdateDayNightCycle();
        HandleSunPosition();
    }

    // handles skybox, lighting settings each frame
    private void UpdateDayNightCycle()
    {
        lerpValue = Mathf.Lerp(lerpStart, 1, interpolationPoint);

        RenderSettings.fogColor = gradients[phaseIndex][0].Evaluate(lerpValue);
        RenderSettings.ambientLight = gradients[phaseIndex][1].Evaluate(lerpValue);
        directionalLight.color = gradients[phaseIndex][2].Evaluate(lerpValue);
        skyboxMaterial.SetColor("_Tint", gradients[phaseIndex][3].Evaluate(lerpValue));
    }

    // handles position of directional light each frame
    private void HandleSunPosition()
    {
        if (phaseIndex == 0) // day
        {
            sunPosition = Mathf.Lerp(sunLerpStart, 0, interpolationPoint);
            directionalLight.transform.rotation = Quaternion.Euler(sunPosition, 80f, 0f);
        } else if (phaseIndex == 1) // night
        {

        } else // morning
        {
            sunPosition = Mathf.Lerp(sunLerpStart, 120, interpolationPoint);
            directionalLight.transform.rotation = Quaternion.Euler(sunPosition, 80f, 0f);
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

    private void OnApplicationQuit()
    {
        skyboxMaterial.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f));
    }

    // called when switching phases
    public void SetState(int phase, float phaseTime, bool continous)
    {
        phaseIndex = phase;
        phaseLength = phaseTime;
        counting = true;
        currentTime = 0;

        if (continous)
        {
            lerpStart = lerpValue;
            sunLerpStart = sunPosition;
        } else
        {
            lerpStart = 0;

            if (phase == 2) // morning (voting)
            {
                skyboxMaterial = daySkyboxMaterial;
                bonfireSource.Stop();
                fireFX.Stop();
                fireLight.gameObject.SetActive(false);
                sunLerpStart = 180f;
            }
            else if (phase == 1) // night
            {
                skyboxMaterial = nightSkyboxMaterial;
                bonfireSource.Play();
                fireFX.Play();
                fireLight.gameObject.SetActive(true);
                sunLerpStart = 0f;
            } else // day
            {
                sunLerpStart = 120;
            }
            RenderSettings.skybox = skyboxMaterial;
        }
    }
}
