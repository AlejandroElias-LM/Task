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
        var item = Instantiate(prefab, transform.localPosition, Quaternion.identity, transform.parent);
        print("Prefab spawnado ?"+ item == null);
        var itemBehaviour = item.GetComponent<InventoryItem>();
        print(itemBehaviour);
        StartCoroutine(PlaceItem(itemBehaviour));

    }

    IEnumerator PlaceItem(InventoryItem item) {
        var shape = item.shape;
        bool placed = false;
        var x = 0;
        var y = 0;
        var draggedPos = myInventory.transform.GetChild(1).GetChild(0);
        while (!placed)
        {
            if(myInventory.CanPlaceShapeAt(shape, Vector2.zero, new Vector2(x, y))){
                myInventory.PlaceShapeAt(shape, Vector2.zero, new Vector2(x, y));

                var i = y * myInventory.shape.width + x;
                var inventoryCell = myInventory.transform.GetChild(1).GetChild(i);
                
                

                var dist = inventoryCell.position - draggedPos.position;
                item.gameObject.transform.position += dist;

                item.transform.parent = transform;
                item.OnGrabbed.AddListener((Vector2 _) => { item.transform.parent = transform.parent; });
                placed = true;
            }
            x++;
            if(x >= myInventory.shape.width)
            {
                x = 0;
                y++;
                if (y >= myInventory.shape.height) break;
            }
            yield return null;

        }
        yield return null; 
    }
}
