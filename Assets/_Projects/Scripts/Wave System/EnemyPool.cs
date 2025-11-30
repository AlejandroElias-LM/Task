using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public EnemyController prefab;
    public int initialSize = 10;

    Queue<EnemyController> pool = new Queue<EnemyController>();

    void Awake()
    {
        for (int i = 0; i < initialSize; i++)
            CreateNewToPool();
    }

    EnemyController CreateNewToPool()
    {
        EnemyController e = Instantiate(prefab, transform);
        e.gameObject.SetActive(false);
        pool.Enqueue(e);
        return e;
    }

    // Pega um inimigo do pool (se necessário, cria mais)
    public EnemyController Get()
    {
        if (pool.Count == 0)
            CreateNewToPool();
        EnemyController e = pool.Dequeue();
        return e;
    }

    // Retorna para o pool
    public void Return(EnemyController e)
    {
        e.gameObject.SetActive(false);
        pool.Enqueue(e);
    }
}
