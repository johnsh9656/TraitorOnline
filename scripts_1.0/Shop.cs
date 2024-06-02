using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    GameplayManager gm;
    [SerializeField] ShopItem[] shopItems;
    [SerializeField] Button resetButton;
    [SerializeField] TMP_Text resetPriceText;
    [SerializeField] Item[] availableItems;
    [SerializeField] Item[] allItems;

    void Start()
    {
        gm = GetComponentInParent<GameplayManager>();
        foreach (ShopItem s in shopItems)
        {
            s.AssignGM(gm);
        }

        resetButton.onClick.AddListener(() =>
        {
            ResetButtonPressed();
        });
    }

    public void SetUpShop(int day)
    {
        if (day == 1)
        {
            shopItems[0].SetItem(ChooseNewItem());
            shopItems[1].SetItem(allItems[0]); // day 2 placeholder
            shopItems[2].SetItem(allItems[1]); // day 3 placeholder
        } else
        {
            for (int i = 0; i < shopItems.Length; i++)
            {
                // reset item if newly available or was bought last day and not placeholder
                if (i == day - 1 || shopItems[i].Sold() && !shopItems[i].GetItem().placeholder)
                {
                    shopItems[i].SetItem(ChooseNewItem());
                }
            }
        }

        resetPriceText.text = gm.GetDay().ToString();
    }

    private void ResetButtonPressed()
    {
        // check if player has sufficient funds
        if (gm.GetLocalPlayer().GetMoney() < gm.GetDay())
        {
            // insufficient funds
            return;
        }

        // charge player
        gm.GetLocalPlayer().ChangeMoney(-gm.GetDay());
        gm.PlaySFX(gm.buyItemSFX);

        SetUpShop(gm.GetDay());

        foreach (ShopItem s in shopItems)
        {
            if (s.GetItem().placeholder || s.Sold())
            {
                // do not reset
                continue;
            } else
            {
                // reset
                s.SetItem(ChooseNewItem());
            }
        }
    }

    private Item ChooseNewItem()
    {
        return availableItems[Random.Range(0, availableItems.Length)];
    }

    public void AssignGM(GameplayManager _gm)
    {
        gm = _gm;
        
        foreach (ShopItem s in shopItems)
        {
            s.AssignGM(_gm);
        }
    }
}
