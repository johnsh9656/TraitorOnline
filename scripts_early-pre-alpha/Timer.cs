using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;
    private float timeRemaining = 0f;
    private GameplayManager manager;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (timeRemaining <= 0) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0 && NetworkManager.Singleton.IsHost)
        {
            manager.TimerEnded();
            ResetTimerClientRpc();
        }

        int seconds = ((int)timeRemaining % 60);
        int minutes = ((int)timeRemaining / 60);
        timeText.text = "Next phase: " + (string.Format("{0:00}:{1:00}", minutes, seconds));
    }

    public void SetTimeRemaining(float time)
    {
        timeRemaining = time;
        int seconds = ((int)timeRemaining % 60);
        int minutes = ((int)timeRemaining / 60);
        timeText.text = "Next phase: " + (string.Format("{0:00}:{1:00}", minutes, seconds));
    }

    public void SetGameplayManager(GameplayManager gm)
    {
        manager = gm;
    }

    public void ResetTimer()
    {
        timeRemaining = 0;
        timeText.text = "Next phase: 0:00";
    }

    [ClientRpc]
    public void ResetTimerClientRpc()
    {
        timeRemaining = 0;
        timeText.text = "Next phase: 0:00";
    }
}
