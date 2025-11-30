using UnityEngine;

public class ItemShard : MonoBehaviour
{
    public GameObject spawnPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.attachedRigidbody == null) return;

        if(collision.attachedRigidbody.CompareTag("Player"))
            if(ItemDeliver.instance != null)
            {
                ItemDeliver.instance.DeliverItem(spawnPrefab);
                Destroy(gameObject);
            }
    }
}
