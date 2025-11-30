using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffManager : MonoBehaviour
{
    public static PlayerBuffManager instance;

    private Dictionary<ModifierType, TypeBucket> playerBuffs;

    private void Awake()
    {
        if (instance != null)
            Destroy(this);

        instance = this;
    }

    void Start()
    {
        playerBuffs = new();
    }

    public void AddModifier(Modifier mod)
    {
        if (playerBuffs.ContainsKey(mod.type))
        {
            playerBuffs[mod.type].AddModifier(mod);
        }
        else
        {
            var tb = new TypeBucket(this, mod);
            playerBuffs[mod.type] = tb;
        }
    }

    public void RemoveModifier(Modifier mod)
    {
        if (playerBuffs.ContainsKey(mod.type))
        {
            playerBuffs[mod.type].RemoveModifier(mod);
        }
    }

    public void RemoveBucket(ModifierType type)
    {
        if (playerBuffs.ContainsKey(type))
            playerBuffs.Remove(type);
    }

    public float GetModValue(ModifierType type)
    {
        if (playerBuffs.ContainsKey(type))
        {
            return playerBuffs[type].GetValue();
        }
        else return 0;
    }

    [Button]
    public void PrintAllBuffs()
    {
        foreach(var dict in playerBuffs.Values)
        {
            dict.Print();
        }
    }
}

public class TypeBucket
{
    public ModifierType bucketType;
    public LinkedList<Modifier> sumAndSub;
    public LinkedList<Modifier> multAndDiv;
    public bool isDirty;
    public float oldValue;

    private PlayerBuffManager manager;

    public TypeBucket(PlayerBuffManager manager, Modifier mod)
    {
        this.manager = manager;
        sumAndSub = new();
        multAndDiv = new();

        bucketType = mod.type;

        AddModifier(mod);
    }

    public float GetValue()
    {
        if(isDirty)
        {
            var sum = 0f;
            var multiplier = 1f;
            foreach(var mod in sumAndSub)
            {
                sum += mod.value;
            }
            foreach(var mod in multAndDiv)
            {
                multiplier *= mod.value;
            }
            oldValue = sum * multiplier;
            isDirty = false;
            return sum * multiplier;
        }
        else
        {
            return oldValue;
        }
    }

    public void AddModifier(Modifier mod)
    {
        if (mod.operation == ModifierOperation.Sum)
            sumAndSub.AddLast(mod);
        else
            multAndDiv.AddLast(mod);

        isDirty = true; // forces the system to calculate again
    }
    public void RemoveModifier(Modifier mod)
    {
        if (mod.operation == ModifierOperation.Sum)
            sumAndSub.Remove(mod);
        else
            multAndDiv.Remove(mod);

        isDirty = true; // forces the system to calculate again
        if (sumAndSub.Count == 0 && multAndDiv.Count == 0) //if Empty, then remove the bucket from the memory
        {
            manager.RemoveBucket(bucketType);
        }

    }

    public void Print()
    {
        Debug.LogWarning($"Starting the {bucketType} bucket print");
        Debug.Log($"This class have {sumAndSub.Count} sum modifiers and {multAndDiv.Count} multiplication modifiers");
        var i = 0;
        var sum = 0f;
        foreach(var m in sumAndSub)
        {
            Debug.Log($"{i} mod value of sum = {m.value}");
            sum += m.value;
        }
        Debug.Log($"the sum results in {sum}"); 
        i = 0;
        var multi = 1f;
        foreach(var m in multAndDiv)
        {
            Debug.Log($"{i} mod value of multiplication = {m.value}");
            multi *= m.value;
        }
        Debug.Log($"the multiplication results in {multi}");
        Debug.Log($"Final Value =  {(sum * multi):F2}");
    }
}
