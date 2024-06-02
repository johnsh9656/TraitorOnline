using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// base class for other minigmames
public class Minigame : MonoBehaviour
{
    private int payout = 0;

    public virtual int GetPayout()
    {
        return payout;
    }

    public virtual void SetPayout(int p)
    {
        payout = p;
    }

    public virtual void StartMinigame()
    {
        //
    }

    public virtual void EndMinigame()
    {
        //
    }
}
