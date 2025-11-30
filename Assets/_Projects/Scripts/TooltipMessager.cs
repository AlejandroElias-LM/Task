using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipMessager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    [SerializeField] private InventoryItem item;
    public void OnPointerEnter(PointerEventData eventData)
    {
        var instance = TooltipManager.instance;
        if (instance != null && item.GetItemData != null)
            instance.ShowInformation(item.GetItemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        item = GetComponent<InventoryItem>();
        if (item == null) Destroy(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
