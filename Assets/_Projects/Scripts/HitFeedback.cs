using System.Collections;
using UnityEngine;

public class HitFeedback : MonoBehaviour
{
    public SpriteRenderer[] relatedSprites;
    public AnimationCurve flashBehaviour;
    public float animationTime = .5f;

    public void StartEffect()
    {
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    public void OnDisable()
    {
        foreach (var sprite in relatedSprites)
        {
            sprite.material.SetFloat("_FlashBlend", 0);
        }
    }

    IEnumerator Animate()
    {
        var time = 0f;
        while(time < animationTime)
        {
            time += Time.deltaTime;
            var perc = Mathf.Clamp01(time / animationTime);
            var value = flashBehaviour.Evaluate(perc);
            foreach(var sprite in relatedSprites)
            {
                sprite.material.SetFloat("_FlashBlend", value);
            }
            yield return null;
        }
    }
}
