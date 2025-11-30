using UnityEngine;

public interface IHitable
{
    public void ApplyHit(float damage, GameObject aggressor);
}
