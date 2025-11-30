using System.Collections.Generic;
using UnityEngine;

public class WeaponItem
{
    public string name;
    public float damage;
    public float attackSpeed;
    public float range;

    public List<Modifier> passives;
    public void Setup(WeaponItemData data)
    {
        Debug.Log("Creating Item");
        this.damage = Random.Range(data.minDamage, data.maxDamage);
        this.attackSpeed = data.attackSpeed;
        this.range = data.range;
        this.name = data.itemName;
        Debug.Log($"Stats( Dmg = {damage} | atkSpeed = {attackSpeed} | range = {range}");

        passives = new List<Modifier>();

        
        foreach(var mod in data.possibleModifiers)
        {
            var modRoll = mod.GetModifier();
            if (modRoll.type == ModifierType.Null) continue;

            Debug.Log("Got: "+modRoll.GetModifierAsText());
            passives.Add(modRoll);
            if (passives.Count >= data.randomPickCount) break;
        }
    }

    public void Subscribe()
    {
        var instance = PlayerBuffManager.instance;
        if(instance != null)
        {
            foreach(var m in passives)
            {
                instance.AddModifier(m);
            }
        }
    }
    public void Unsubscribe()
    {
        var instance = PlayerBuffManager.instance;
        if (instance != null)
        {
            foreach (var m in passives)
            {
                instance.RemoveModifier(m);
            }
        }
    }
}
