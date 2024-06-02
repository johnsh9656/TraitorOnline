using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minigame where money is rewarded based on click speed
public class Clickspeed : Minigame
{
    [SerializeField] private Button clickButton;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text payoutText;

    [SerializeField] private int duration;
    private int clicks = 0;
    private float cps = 0;

    private GameplayManager gm;

    private void Start()
    {
        clickButton.onClick.AddListener(() =>
        {
            Click();
        });
    }

    private void Click()
    {
        clicks++;

        if (!gm) gm = FindObjectOfType<GameplayManager>();
        gm.PlaySFX(gm.minigameClickSFX);
    }

    public override void StartMinigame()
    {
        base.EndMinigame();
        clicks = 0;
        clickButton.gameObject.SetActive(true);
        payoutText.gameObject.SetActive(false);
        StartCoroutine(Timer());
    }

    public override void EndMinigame()
    {
        base.EndMinigame();

        // calculate CPS
        cps = (float) clicks / duration;

        // determine payout
        if (cps > 6f)
        {
            SetPayout(3);
        }
        else if (cps > 4f)
        {
            SetPayout(2);
        }
        else if (cps > 2f)
        {
            SetPayout(1);
        }
        else
        {
            SetPayout(0);
        }

        gm.PlaySFX(gm.minigameEndSFX);
        clickButton.gameObject.SetActive(false);
        payoutText.gameObject.SetActive(true);
        countdownText.text = "Task Finished. CPS: " + cps.ToString("0.00");
        payoutText.text = GetPayout().ToString();

        StartCoroutine(WaitToClose());
    }

    private IEnumerator Timer()
    {
        if (!gm) gm = FindObjectOfType<GameplayManager>();

        for (int i = duration; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
            gm.PlaySFX(gm.timerTickSFX);
        }
        EndMinigame();
    }

    private IEnumerator WaitToClose()
    {
        yield return new WaitForSeconds(2f);
        
        gm.MinigameEnded();
    }
}
