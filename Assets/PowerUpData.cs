using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "PowerUp/Create New PowerUp")]
public class PowerUpData : ScriptableObject {
    public string itemName;
    public Sprite overlaySprite;
    public GameObject prefab;
    public float dropRate;
}