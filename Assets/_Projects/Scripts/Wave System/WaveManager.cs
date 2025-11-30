using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Pool & Enemy")]
    public EnemyPool pool;

    [Header("Spawn Points (set exactly 2)")]
    public Transform[] spawnPoints = new Transform[2];

    [Header("Wave config")]
    public int baseEnemiesPerWave = 1;      // inimigos iniciais na wave 1
    public float timeBetweenSpawns = 0.3f;  // intervalo entre spawn de inimigos
    public float timeBetweenWaves = 2f;     // delay opcional entre waves (após todos mortos)

    int currentWave = 0;
    int aliveEnemies = 0;
    bool isSpawning = false;

    void Start()
    {
        if (spawnPoints == null || spawnPoints.Length < 2)
            Debug.LogWarning("WaveManager: configure 2 spawn points no inspector.");

        StartNextWave();
    }

    // Calcula quantos inimigos a wave atual deve ter:
    // aumento de 1 inimigo a cada 2 waves.
    int GetEnemiesForWave(int waveNumber)
    {
        // Ex.: wave 1 -> base + floor((1-1)/2) = base + 0
        //      wave 2 -> base + floor((2-1)/2) = base + 0
        //      wave 3 -> base + floor((3-1)/2) = base + 1
        //      wave 4 -> base + floor((4-1)/2) = base + 1
        int extra = (waveNumber - 1) / 2;
        return baseEnemiesPerWave + extra;
    }

    public void StartNextWave()
    {
        if (isSpawning) return;
        currentWave++;
        int toSpawn = GetEnemiesForWave(currentWave);
        StartCoroutine(SpawnWaveRoutine(toSpawn));
    }

    IEnumerator SpawnWaveRoutine(int count)
    {
        isSpawning = true;
        aliveEnemies = count;
        int spawnPointIndex = 0;

        for (int i = 0; i < count; i++)
        {
            // Pega inimigo do pool
            EnemyController e = pool.Get();
            // escolha o spawn point alternando entre os 2 pontos
            Transform sp = spawnPoints[spawnPointIndex % spawnPoints.Length];
            spawnPointIndex++;

            // Inicializa e ativa o inimigo
            e.Init(this, sp.position, sp.rotation);

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;

        // Aqui aguardamos que todos os inimigos morram; quando todos morrerem
        // NotifyEnemyDied reduz aliveEnemies e chama StartNextWave após delay.
    }

    // Chamado por Enemy quando morre
    public void NotifyEnemyDied(EnemyController e)
    {
        // retorna ao pool
        pool.Return(e);

        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);

        if (aliveEnemies <= 0 && !isSpawning)
        {
            // Todos mortos -> inicia a próxima wave após um pequeno delay
            StartCoroutine(NextWaveDelay());
        }
    }

    IEnumerator NextWaveDelay()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        StartNextWave();
    }
}
