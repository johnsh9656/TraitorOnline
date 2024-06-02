using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class SkinManager : NetworkBehaviour
{
    [SerializeField] private Button changeSkinButton;
    [SerializeField] private GameObject[] skins;
    [SerializeField] private int startIndex = 0;
    [SerializeField] private int firstDungeonIndex = 12;
    [SerializeField] private Avatar fantasyAvatar;
    [SerializeField] private Avatar dungeonAvatar;
    private Animator anim;
    [SerializeField] private GameObject fantasyRoot;
    [SerializeField] private GameObject dungeonRoot;
    private NetworkVariable<int> skinIndex = new NetworkVariable<int>
        (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    GameplayManager gm;

    private void Start()
    {
        anim = GetComponentInParent<Animator>();
        anim.avatar = fantasyAvatar;
        Hide();
        skinIndex.OnValueChanged += OnSkinIndexChanged;

        // if owner
        if (OwnerClientId == NetworkManager.LocalClientId)
        {
            Show();
            changeSkinButton.onClick.AddListener(ButtonPressed);
            skinIndex.Value = startIndex;
            //reset each player's skin
            foreach (SkinManager sm in FindObjectsOfType<SkinManager>())
            {
                sm.ResetSkin();
            }
        }
    }

    private void OnSkinIndexChanged(int oldValue, int newValue)
    {
        Debug.Log("skin index changed from " + oldValue + " to " + newValue);
        skins[oldValue].SetActive(false);
        skins[newValue].SetActive(true);

        if (!anim) anim = GetComponentInParent<Animator>();

        if (newValue == 0) anim.avatar = fantasyAvatar;
        else if (newValue == firstDungeonIndex) anim.avatar = dungeonAvatar;

        if (!gm) gm = FindObjectOfType<GameplayManager>();
        gm.PlaySFX(gm.changeSkinSFX);
    }

    private void ButtonPressed()
    {
        int newIndex = skinIndex.Value + 1;
        if (newIndex == skins.Length) newIndex = 0;
        
        skinIndex.Value = newIndex;
    }

    void ResetSkin()
    {
        foreach (GameObject skin in skins)
        {
            skin.SetActive(false);
        }

        skins[skinIndex.Value].SetActive(true);
    }

    public void Show()
    {
        changeSkinButton.gameObject.SetActive(true);
    }

    public void Hide()
    {
        changeSkinButton.gameObject.SetActive(false);
    }
}
