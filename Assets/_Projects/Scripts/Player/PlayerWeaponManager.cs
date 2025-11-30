using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    [SerializeField] private WeaponSwipe _anim;
    public float baseDamage = 5;
    public float currentDamage = 5;
    public float baseRange = 1;
    public float currentRange = 1;
    public float baseAtkSpd = 1;
    public float currentAtkSpd = 1;
    bool isAttacking => _anim.canHit;

    private List<Rigidbody2D> hittedBodies;
    private void Start()
    {
        hittedBodies = new();
        currentDamage = baseDamage;
        currentRange = baseRange;
        currentAtkSpd = baseAtkSpd;
        _anim.onSwipeFinish.AddListener(() => { hittedBodies.Clear(); });
    }

    public void Attack()
    {
        _anim.Swipe(currentAtkSpd);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttacking) return;
        if (other.attachedRigidbody == null) return;
        if (hittedBodies.Contains(other.attachedRigidbody)) return;

        if (! other.attachedRigidbody.CompareTag("Player"))
        {
            other.attachedRigidbody.TryGetComponent(out IHitable hitableObj);
            if (hitableObj != null)
            {
                hitableObj.ApplyHit(currentDamage, gameObject);
                if(hitableObj is IKnockbackable)
                {
                    (hitableObj as IKnockbackable).KnockbackTarget(10f, (other.transform.position - transform.position).normalized);
                }
                hittedBodies.Add(other.attachedRigidbody);
            }
        }
    }

    public void LoadDamageBuffs()
    {
        if (PlayerBuffManager.instance == null) return;
        var instance = PlayerBuffManager.instance;
        var flat = instance.GetFlatStats();

        print(flat);
        var damage = instance.GetBuffBucket(ModifierType.BonusDamage);
        if (damage != null)
        {
            currentDamage = baseDamage + flat.dmg + damage.GetValue();
            print("Current damage Changed");
        }
        else
        {
            currentDamage = baseDamage + flat.dmg;
        }
        var range = instance.GetBuffBucket(ModifierType.BonusRange);
        if (range != null)
        {
            print("Current range Changed");
            currentRange = baseRange + (flat.range+range.GetValue())/10f;
            _anim.transform.localScale = Vector3.one * currentRange;
        }
        else
        {
            currentRange = baseRange + (flat.range / 10f);
            _anim.transform.localScale = Vector3.one * currentRange;
        }
        var atkSp = instance.GetBuffBucket(ModifierType.BonusAttackSpeed);
        if (atkSp != null)
        {
            print("Current atk Changed");
            currentAtkSpd = baseAtkSpd + (flat.atkSpd+atkSp.GetValue())/10f;
        }
        else
        {
            currentAtkSpd = baseAtkSpd + (flat.atkSpd / 10f);
        }
    }
}
