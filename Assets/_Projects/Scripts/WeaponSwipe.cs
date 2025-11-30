using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct SimplePoint
{
    public Vector3 localPos;
    public Vector3 localRot;
}

public class WeaponSwipe : MonoBehaviour
{
    public float baseSwipeTime = .5f;

    public SimplePoint startPoint;
    public SimplePoint endPoint;

    public bool canHit = false;

    public UnityEvent onSwipeFinish;

    Coroutine SwipeRoutine;
    [Button]
    public void SetStart()
    {
        startPoint = new SimplePoint();
        startPoint.localPos = transform.localPosition;
        startPoint.localRot = transform.localEulerAngles;
        print("Start saved");
    }

    [Button]
    public void SetEnd()
    {
        
        endPoint = new SimplePoint();
        endPoint.localPos = transform.localPosition;
        endPoint.localRot = transform.localEulerAngles;
        print("End saved");
    }

    public void SetRange(float f)
    {
        this.transform.localScale = Vector3.one * (1 + f);
    }

    public void Swipe(float speed)
    {
        if (SwipeRoutine != null) return;

        SwipeRoutine = StartCoroutine(SwipeAnimation(speed));
    }

    IEnumerator SwipeAnimation(float speed)
    {
        var time = baseSwipeTime;
        var currentTime = 0f;

        canHit = true;

        print($"start point = {startPoint.localPos} | {startPoint.localRot}");
        print($"end point = {endPoint.localPos} | {endPoint.localRot}");
        while(currentTime < time)
        {
            
            currentTime += Time.deltaTime * speed;
            var perc = Mathf.Clamp01(currentTime / time);

            transform.localPosition = Vector3.Lerp(startPoint.localPos, endPoint.localPos, perc);
            transform.localEulerAngles = -Vector3.Lerp(startPoint.localRot, endPoint.localRot, perc);
            yield return null;
        }
        canHit = false;
        onSwipeFinish?.Invoke();
        SwipeRoutine = null;
    }
}
