using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndUI : MonoBehaviour
{
    [SerializeField] Button menuButton;
    [SerializeField] TextMeshProUGUI winnerText;
    [SerializeField] TextMeshProUGUI wasTraitorText;

    private void Start()
    {
        menuButton.onClick.AddListener(() =>
        {
            menuButton.enabled = false;
            Loader.Load(Loader.Scene.Lobby);
        });
    }

    public void Set(string winnerStr, string traitorName)
    {
        winnerText.text = winnerStr;
        wasTraitorText.text = traitorName + " was the traitor";
    }
}
