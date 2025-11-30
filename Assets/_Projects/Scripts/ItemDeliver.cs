using System.Collections;
using UnityEngine;

public class ItemDeliver : MonoBehaviour
{
    public static ItemDeliver instance;
    [SerializeField] Inventory myInventory;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DeliverItem(GameObject prefab)
    {
        var item = Instantiate(prefab, transform.position, Quaternion.identity, transform.parent);
        print("Prefab spawnado ?"+ item == null);
        var itemBehaviour = item.GetComponent<InventoryItem>();
        print(itemBehaviour);
        StartCoroutine(PlaceItem(itemBehaviour));

    }

    IEnumerator PlaceItem(InventoryItem item) {
        item.transform.localScale = Vector3.one * 0.75f;
        yield return null;
        bool placed = false;
        var x = 0;
        var y = 0;
        while (!placed)
        {

            placed = PlaceObject(item, new Vector2(x, y));

            x++;
            if(x >= myInventory.shape.width)
            {
                x = 0;
                y++;
                if (y >= myInventory.shape.height) break;
            }
            yield return null;

        }
        if (!placed) print("Not placed, cannot place that shape here");
        else print("Placed in " + x + " " + y);
        yield return null; 
    }

    public bool PlaceObject(InventoryItem item,Vector2 pos)
    {
        var draggedPos = item.GetChild(Vector2.zero);

        bool insideInventory = myInventory.GetChild(pos);
        if (!insideInventory) return false;

        bool canPlace = myInventory.CanPlaceShapeAt(item.shape, item.clickedCell, pos);
        if (!canPlace) return false;

        var inventoryCellPos = myInventory.GetChild(pos);
        item.transform.localEulerAngles = Vector3.zero;
        item.transform.localScale = Vector3.one * .75f;

        var dist = inventoryCellPos.position - draggedPos.position;
        item.transform.position += dist;

        var list = myInventory.PlaceShapeAt(item.shape, item.clickedCell, pos);

        item.currentFilledPosition = new FilledPosition(myInventory, list, true);
        item.OnPlaced?.Invoke();
        item.GenerateItemData();
        item.insideInventory = false;
        //item.ConnectItem();
        return true;
    }
}
