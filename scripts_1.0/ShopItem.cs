using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    [SerializeField] TMP_Text name_text;
    [SerializeField] TMP_Text description_text;
    [SerializeField] TMP_Text price_text;
    [SerializeField] Image item_image;
    [SerializeField] Button buyButton;
    [SerializeField] GameObject priceImage;
    Item item;
    GameplayManager gm;
    bool sold = false;

    public void SetItem(Item i)
    {
        item = i;
        name_text.text = item.item_name;
        description_text.text = item.item_description;
        price_text.text = item.item_price.ToString();
        item_image.sprite = item.item_image;
        sold = false;
        

        if (item.placeholder)
        {
            priceImage.SetActive(false);
            price_text.gameObject.SetActive(false);
            buyButton.gameObject.SetActive(false);
        } else
        {
            priceImage.SetActive(true);
            price_text.gameObject.SetActive(true);
            buyButton.gameObject.SetActive(true);
        }
    }


    void Start()
    {
        buyButton.onClick.AddListener(() =>
        {
            BuyButtonPressed();
        });
    }

    private void BuyButtonPressed()
    {
        if (item.placeholder || sold) return;

        // check if user has sufficient funds
        if (item.item_price > gm.GetLocalPlayer().GetMoney())
        {
            // insufficient funds

            // some sort of visuaL?
            return;
        }

        // charge player
        gm.GetLocalPlayer().ChangeMoney(-item.item_price);

        // apply effects of item
        gm.GetLocalPlayer().ChangeDefense(item.defense_dif);
        gm.GetLocalPlayer().ChangeAttack(item.attack_dif);
        if (item.temp_increase)
        {
            gm.GetLocalPlayer().SetTempDefense(item.defense_dif);
        }
        if (item.vote_multiplier > 1)
        {
            gm.SetVoteMultiplier(item.vote_multiplier);
        }

        // sfx
        gm.PlaySFX(gm.buyItemSFX);

        // sold out visuals
        name_text.text = "Sold out!";
        description_text.text = "Come back for a new item tomorrow!";

        sold = true;
    }

    public bool Sold()
    {
        return sold;
    }

    public Item GetItem()
    {
        return item;
    }

    public void AssignGM(GameplayManager gm_assigned)
    {
        gm = gm_assigned;
    }
}
