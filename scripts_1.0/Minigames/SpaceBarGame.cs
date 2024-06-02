using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// minigame where money is rewarded based on clicking the
// space bar at the right time (shown by ui)
public class SpaceBarGame : Minigame
{
    [SerializeField] private KeyCode key = KeyCode.Space;
    [SerializeField] private RectTransform mover;
    [SerializeField] private RectTransform sliderThing;
    [SerializeField] private RectTransform clickArea;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text payoutText;

    [SerializeField] private float maxTransX = 300f;

    [SerializeField] private int duration = 10;
    private int score = 0;
    private bool gameActive = false;
    private float lerpValue = 0f;
    private bool forward = true;
    private bool canScore = true;

    private GameplayManager gm;

    private void Update()
    {
        if (!gameActive) return;

        if (Input.GetKeyDown(key))
        {
            Debug.Log("click");
            if (canScore) Click();
        }

        maxTransX = sliderThing.rect.x - mover.rect.x;

        if (mover.localPosition.x >= clickArea.rect.x && mover.localPosition.x <= -clickArea.rect.x)
        {
            Debug.Log("in");
        }
        float dirModifier = forward ? 1f : -1f; 
        lerpValue += dirModifier * ((float)score + 1) * .5f * Time.deltaTime;

        mover.SetLocalPositionAndRotation(new Vector2(Mathf.Lerp(-maxTransX, maxTransX, lerpValue), 0), Quaternion.identity);

        // change direction at ends
        if (lerpValue > 1 || lerpValue < 0)
        {
            forward = !forward;
            lerpValue = Mathf.Clamp01(lerpValue);
            Debug.Log("changing direction");
        }
    }

    private void Click()
    {
        Debug.Log("Click");
        // check if within bounds
        if (mover.localPosition.x >= clickArea.rect.x && mover.localPosition.x <= -clickArea.rect.x)
        {
            score++;
            StartCoroutine(ScoreCooldown());
            Debug.Log("score increase");

            if (!gm) gm = FindObjectOfType<GameplayManager>();
            gm.PlaySFX(gm.minigamePointSFX);
        }
    }

    private IEnumerator ScoreCooldown()
    {
        clickArea.gameObject.SetActive(false);
        canScore = false;
        yield return new WaitForSeconds(.25f);
        clickArea.gameObject.SetActive(true);
        canScore = true;
    }

    public override void StartMinigame()
    {
        base.EndMinigame();
        score = 0;
        lerpValue = 0f;
        payoutText.gameObject.SetActive(false);

        mover.localPosition = Vector2.zero;
        forward = true;
        gameActive = true;

        StartCoroutine(Timer());
    }

    public override void EndMinigame()
    {
        base.EndMinigame();
        gameActive = false;

        // determine payout
        if (score > 8)
        {
            SetPayout(3);
        }
        else if (score > 6)
        {
            SetPayout(2);
        }
        else if (score > 4)
        {
            SetPayout(1);
        }
        else
        {
            SetPayout(0);
        }

        gm.PlaySFX(gm.minigameEndSFX);
        payoutText.gameObject.SetActive(true);
        countdownText.text = "Task Finished. Score: " + score.ToString();
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
