using System;
using System.Collections.Generic;
using UnityEngine;


// =====================
// Item Data (ScriptableObject)
// =====================
[CreateAssetMenu(fileName ="New Weapon" ,menuName = "Items/WeaponItem")]
public class WeaponItemData : ScriptableObject
{
    [Header("Base stats")]
    public string itemName = "Rusted";
    public int minDamage = 1;
    public int maxDamage = 3;
    public float attackSpeed = 1f; // attacks per second
    public float range = 1f; // used as scale multiplier if desired


    [Header("Randomization")]
    [Tooltip("How many random modifiers to pick from possibleRandomModifiers when an ItemInstance is created.")]
    public int randomPickCount = 1;

    public ModifierChance[] possibleModifiers;


    [Header("Unique passives (these are instantiated per item instance)")]
    public UniquePassiveData[] uniquePassives;
}


[Serializable]
public struct ModifierChance
{
    public ModifierType type;
    public ModifierOperation operation;
    [Range(0,100)]
    public int chanceToGet;
    public float minValue;
    public float maxValue;

    public Modifier GetModifier()
    {
        var random = UnityEngine.Random.Range(0, 100);
        if (random > chanceToGet)
        {
            var nullMod = new Modifier();
            nullMod.type = ModifierType.Null;
            return nullMod;
        }
        else
        {
            var modifier = new Modifier();
            modifier.type = this.type;
            modifier.operation = this.operation;
            modifier.value = Mathf.Floor(UnityEngine.Random.Range(minValue, maxValue));
            return modifier;
        }
    }

}
[Serializable]
public struct Modifier
{
    public ModifierType type;
    public ModifierOperation operation;
    public float value;

    public string GetModifierAsText()
    {
        string sign = "";
        string opWord = "";
        float displayValue = value;

        switch (operation)
        {
            case ModifierOperation.Sum:
                sign = value >= 0 ? "+" : "-";
                opWord = " ";
                displayValue = Mathf.Abs(value);
                break;

            case ModifierOperation.Multiplication:
                sign = "x";
                opWord = " ";
                break;
        }

        string effect = type switch
        {
            ModifierType.HealthPerSecond => $"{sign}{displayValue} Health per second",
            ModifierType.HealthPerHit => $"{sign}{displayValue} Health per hit",
            ModifierType.HealthPerKill => $"{sign}{displayValue} Health per kill",
            ModifierType.MaxHealth => $"{sign}{displayValue} Max Health",

            ModifierType.BonusAttackSpeed => $"{sign}{displayValue} Attack Speed",
            ModifierType.BonusDamage => $"{sign}{displayValue} Damage",
            ModifierType.BonusRange => $"{sign}{displayValue} Range",
            ModifierType.BonusKnockback => $"{sign}{displayValue} Knockback",

            ModifierType.BonusMoveSpeed => $"{sign}{displayValue} Move Speed",
            ModifierType.BonusChanceToBlock => $"{sign}{displayValue * 100}% Block Chance",
            ModifierType.BonusDropChance => $"{sign}{displayValue * 100}% Drop Chance",

            ModifierType.Null => "",
            _ => ""
        };

        return effect;
    }
}

public enum ModifierOperation
{
    Sum,
    Multiplication,
}
public enum ModifierType
{
    Null,

    // Health
    HealthPerSecond,
    HealthPerHit,
    HealthPerKill,
    MaxHealth,


    // Combat
    BonusAttackSpeed, // additive
    BonusDamage, // additive
    BonusRange, // multiplicative on range/scale
    BonusKnockback,


    // Utility
    BonusMoveSpeed,
    BonusChanceToBlock, // 0..1
    BonusDropChance, // 0..1
}


// Unique passive description stored in ScriptableObject
[Serializable]
public class UniquePassiveData
{
    public string id; // simple identifier shown in inspector
    public UniquePassiveType passiveType;
    [Tooltip("Chance 0..1 (if applicable)")]
    public float chance = 1f;
    public float magnitude = 1f; // meaning depends on passive
    public float radius = 1f; // area effects
}


public enum UniquePassiveType
{
    OnKillEffect,
    HitEffect,
    // Add more as needed
}