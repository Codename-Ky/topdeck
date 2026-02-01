using System;
using UnityEngine;

[Serializable]
public class DefenderUpgradeStep
{
    [SerializeField] private string label = "Upgrade";
    [SerializeField] private GameObject prefab;
    [SerializeField, Min(0)] private int cost = 50;
    [SerializeField, Min(0.1f)] private float healthMultiplier = 1.2f;
    [SerializeField, Min(0.1f)] private float damageMultiplier = 1.2f;

    public string Label
    {
        get => label;
        set => label = value;
    }

    public GameObject Prefab
    {
        get => prefab;
        set => prefab = value;
    }

    public int Cost
    {
        get => cost;
        set => cost = value;
    }

    public float HealthMultiplier
    {
        get => healthMultiplier;
        set => healthMultiplier = value;
    }

    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = value;
    }
}
