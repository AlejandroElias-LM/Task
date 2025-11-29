using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public Image healthBar, lateHealthBar;
    public float healthBarDelay = .3f;
    public float decreaseSpeed = 3f;
    private float lastTick = 0;

    private void Update()
    {
        if (lateHealthBar.fillAmount <= healthBar.fillAmount || Time.time - lastTick < healthBarDelay ) return;

        lateHealthBar.fillAmount = Mathf.Lerp(lateHealthBar.fillAmount, healthBar.fillAmount, Time.deltaTime * decreaseSpeed);
    }

    public void SetSize(float healthNormalized)
    {
        lastTick = Time.time;
        healthBar.fillAmount = healthNormalized;
    }
}
