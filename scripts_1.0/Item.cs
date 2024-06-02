using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableObject
{
    [Header("Info")]
    [SerializeField] public string item_name;
    [SerializeField] public string item_description;
    [SerializeField] public Sprite item_image;
    [SerializeField] public int item_price;
    [SerializeField] public bool placeholder = false;

    [Header("Effects")]
    [SerializeField] public int defense_dif = 0;
    [SerializeField] public bool temp_increase;
    [SerializeField] public int vote_multiplier = 1;
    [SerializeField] public int attack_dif = 0;
    // other effects

}
