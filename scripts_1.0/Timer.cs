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
    bool tickedAlready = false;

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

        // sound effect on last 5 seconds
        if (timeRemaining <= 5 && !tickedAlready)
        {
            tickedAlready = true;
            StartCoroutine(HandleAudio());
        }

        if (timeRemaining <= 0 && NetworkManager.Singleton.IsHost)
        {
            manager.TimerEnded();
            ResetTimerClientRpc();
            tickedAlready = false;
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

    private IEnumerator HandleAudio()
    {
        for (int i=0;i<4;i++)
        {
            manager.PlaySFX(manager.timerTickSFX);
            yield return new WaitForSeconds(1f);
        }
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
