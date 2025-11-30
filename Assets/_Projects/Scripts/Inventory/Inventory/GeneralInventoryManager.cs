using UnityEngine;

public class GeneralInventoryManager : MonoBehaviour
{
    public static GeneralInventoryManager instance;

    public InventoryItem draggedItem;
    public Inventory currentInventory;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private Vector2 oldInvPos;
    // Update is called once per frame
    void Update()
    {
        if (draggedItem != null && currentInventory != null)
        {
            if (currentInventory.GetCellUnderPointer(Input.mousePosition, null, out Vector2 inventoryPos))
            {
                oldInvPos = inventoryPos;
                draggedItem.SetHover(true);
                bool canPlace = currentInventory.CanPlaceShapeAt(draggedItem.shape, draggedItem.clickedCell, inventoryPos);

                if (canPlace)
                {
                    draggedItem.ChangeColor(draggedItem.placeable);
                }
                else
                {
                    draggedItem.ChangeColor(draggedItem.blocked);
                }
            }
            else
            {
                draggedItem.SetHover(false);
                draggedItem.ChangeColor(draggedItem.normal);
            }
            print(inventoryPos);
        }
    }

    public void PlaceObject()
    {
        var draggedPos = draggedItem.GetChild(draggedItem.clickedCell);

        bool insideInventory = currentInventory.GetCellUnderPointer(draggedPos.position, null, out Vector2 pos);
        if (!insideInventory) return;

        bool canPlace = currentInventory.CanPlaceShapeAt(draggedItem.shape, draggedItem.clickedCell, pos);
        if (!canPlace) return;

        var inventoryCellPos = currentInventory.GetChild(pos);
        draggedItem.transform.localEulerAngles = Vector3.zero;
        draggedItem.transform.localScale = Vector3.one;

        var dist = inventoryCellPos.position - draggedPos.position;
        draggedItem.transform.position += dist;

        var list = currentInventory.PlaceShapeAt(draggedItem.shape, draggedItem.clickedCell, oldInvPos);

        draggedItem.currentFilledPosition = new FilledPosition(currentInventory, list, true);
        draggedItem.ConnectItem();
    }
}
