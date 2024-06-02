using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// minigame where money is rewarded based on # of randomly
// spawned targets clicked in time interval
public class TargetGame : Minigame
{
    [SerializeField] private RectTransform targetButton;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text payoutText;
    [SerializeField] private RectTransform targetArea;

    [SerializeField] private int duration = 10;
    private int clicks = 0;

    private GameplayManager gm;

    private void Start()
    {
        targetButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            Click();
        });
    }

    private void Click()
    {
        clicks++;

        // set to new position
        Vector2 newPos = GetRandomPosition();
        Debug.Log("new position: " + newPos);
        targetButton.SetLocalPositionAndRotation(newPos, Quaternion.identity);

        if (!gm) gm = FindObjectOfType<GameplayManager>();
        gm.PlaySFX(gm.minigamePointSFX);
    }

    private Vector2 GetRandomPosition()
    {
        float targetLength = targetButton.rect.width; // x and y values are equal
        float areaLength = targetArea.rect.width;
        float areaHeight = targetArea.rect.height;

        float posX = Random.Range(-areaLength/2, areaLength/2 - targetLength);
        float posY = Random.Range(-areaHeight/2, areaHeight/2 - targetLength);

        return new Vector2(posX, posY);
    }

    public override void StartMinigame()
    {
        base.EndMinigame();
        clicks = 0;
        targetButton.gameObject.SetActive(true);
        payoutText.gameObject.SetActive(false);

        targetButton.SetLocalPositionAndRotation(GetRandomPosition(), Quaternion.identity);

        StartCoroutine(Timer());
    }

    public override void EndMinigame()
    {
        base.EndMinigame();

        // determine payout
        if (clicks > 12)
        {
            SetPayout(3);
        }
        else if (clicks > 9)
        {
            SetPayout(2);
        }
        else if (clicks > 6)
        {
            SetPayout(1);
        }
        else
        {
            SetPayout(0);
        }

        gm.PlaySFX(gm.minigameEndSFX);
        targetButton.gameObject.SetActive(false);
        payoutText.gameObject.SetActive(true);
        countdownText.text = "Task Finished. Score: " + clicks.ToString();
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

        if (!gm) gm = FindObjectOfType<GameplayManager>();

        gm.MinigameEnded();
    }
}
