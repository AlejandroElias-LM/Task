using UnityEngine;

public class SimpleDropper : MonoBehaviour
{
    [System.Serializable]
    public struct DropItem
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float chance; 
    }

    public DropItem[] possibleDrops;

    public void TryDropItem()
    {
        float roll = Random.value; 
        float cumulative = 0f;

        foreach (var drop in possibleDrops)
        {
            cumulative += drop.chance;
            if (roll <= cumulative)
            {
                Instantiate(drop.prefab, transform.position, Quaternion.identity); // Droppec!
                break;
            }
        }
    }
}
