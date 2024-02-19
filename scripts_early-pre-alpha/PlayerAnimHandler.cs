using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimHandler : MonoBehaviour
{
    private Player localPlayer;
    private Animator anim;
    private KeyCode[] keyCodes =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9,
    };
    private bool isLocal = false;

    private void Start()
    {
        localPlayer = GetComponent<Player>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (localPlayer.IsDead() || !isLocal) return;

        for (int i=0;i<keyCodes.Length;i++)
        {
            if (Input.GetKeyDown(keyCodes[i]))
            {
                PlayEmote(i);
                return;
            }
        }
    }

    private void PlayEmote(int emoteIndex)
    {
        Debug.Log("Playing emote " + emoteIndex);
        anim.SetInteger("EmoteIndex", emoteIndex);
        //StartCoroutine(ResetEmoteIndex());
    }

    private IEnumerator ResetEmoteIndex()
    {
        yield return new WaitForSeconds(0.1f);
        anim.SetInteger("EmoteIndex", -1);
    }

    public void SetAsLocalPlayer()
    {
        isLocal = true;
    }

    public void ResetEmoteIndexEvent()
    {
        if (!isLocal) return;
        anim.SetInteger("EmoteIndex", -1);
    }
}
