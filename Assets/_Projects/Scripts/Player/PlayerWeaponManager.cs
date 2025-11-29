using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{

    [SerializeField] private Animator _anim;
    public float damage = 5;
    bool isAttacking = false;
    public void Attack()
    {
        _anim.Play("Attack");
        isAttacking = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttacking) return;
        if (other.attachedRigidbody == null) return;

        if (! other.attachedRigidbody.CompareTag("Player"))
        {
            other.attachedRigidbody.TryGetComponent(out IHitable hitableObj);
            if (hitableObj != null)
            {
                hitableObj.ApplyHit(damage);
                if(hitableObj is IKnockbackable)
                {
                    (hitableObj as IKnockbackable).KnockbackTarget(10f, (other.transform.position - transform.position).normalized);
                }
            }
        }
    }


}
