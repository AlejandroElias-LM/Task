using UnityEngine;

public class ItemShard : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject[] possibleItems;
    public bool dropped = false;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.attachedRigidbody == null) return;

        if(collision.attachedRigidbody.CompareTag("Player") && !dropped)
            if(ItemDeliver.instance != null)
            {
                dropped = true;
                var spawnPrefab = possibleItems[Random.Range(0, possibleItems.Length)];
                ItemDeliver.instance.DeliverItem(spawnPrefab);
                Destroy(gameObject);
            }
    }
}
