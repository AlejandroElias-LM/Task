using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{

    public static TooltipManager instance;

    [Header("References")]
    public TextMeshProUGUI name, damage, atkSpeed, range, modifiers;

    private WeaponItem currentItem;
    private void Awake()
    {
        if (instance != null)
            Destroy(this);

        instance = this;
    }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowInformation(WeaponItem item)
    {
        if (item == currentItem) return;

        currentItem = item;
        name.text = item.name;
        damage.text = $"{item.damage} Damage";
        atkSpeed.text = $"{item.attackSpeed:F2} Attack Speed";
        range.text = $"{item.range:F2} Range";

        var extra = "";
        foreach(var m in item.passives)
        {
            extra += m.GetModifierAsText() + "\n";
        }
        modifiers.text = extra;
    }
}
