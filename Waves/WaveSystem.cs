using NTC.Global.Pool;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal.Internal;
using Random = UnityEngine.Random;
using MyStarterAssets;
using Guns.Enemy;

public class WaveSystem : MonoBehaviour
{
    public static WaveSystem instance;

    private WaveFunctionCollapse WFC;

    private void Awake()
    {
        instance = this;
    }

    [Header("Main Settings")]
    public int currentWave = 0; // Текущая волна (начинаем с 1)

    [Space]
    [Header("Increase Settings")]
    public float spawn_multiplier = 1.2f; // Множитель процента количества появления врагов на сцене
    //public float strength_multiplier = 1.1f; // Множитель силы противников

    [Space]
    [Header("Start Settings")]
    public int start_summons = 10; // Количество противников в 1 волне
    [HideInInspector]
    public int current_summons = 10; // Текущее количество противников, порождённых в последующих волнах

    [HideInInspector]
    public int summons_spawned = 0; // Сколько врагов было порождено
    [HideInInspector]
    public int current_summons_alive = 0; // Сколько противников в текущей волне живы
    [HideInInspector]
    public int current_summons_dead = 0; // Сколько противников в текущей убито

    [Space]
    private int current_max_summons_once = 30; // Сколько противников должно быть живы в текущей волне
    public int start_max_summons_once = 30; // Начальное значение количсества противников

    public int allEnemies = 0;

    //[Space]
    //public float start_strength = 1f; // Множитель силы противников в 1 волне
    //private float current_strength = 1f; // Множитель силы противников в последующих волнах

    [Header("Time Settings")]
    public float time_between_waves = 30f; // Время между волнами
    [HideInInspector]
    public float time_end_last_wave = 30f; // Время, когда закончилась последняя волна

    [HideInInspector]
    public bool waveIsRunning = false; // Флаг, идёт сейчас волна или нет

    [Header("Spawn Settings")]
    public GameObject[] enemies; // Типы врагов, которых нужно заспавнить
    public Enemy[] enemiesE; // Типы врагов, которых нужно заспавнить

    public Transform player; // Позиция игрока
    public float spawnRadiusMin; // Минимальный радиус спавна от игрока
    public float spawnRadiusMax; // Максимальный радиус спавна от игрока
    public LayerMask obstacleMask; // Маска слоя препятствий (Build по дефолту)

    [Header("Weight Enemies Settings")]
    public int[] enemiesWeight; // Веса видов врагов (больший вес появляется чаще, чем меньший)

    public float mapSideSize; // Половина размера карты (квадрата)

    [Header("Wave Info")]
    public TextMeshProUGUI waveCountText;
    public TextMeshProUGUI enemyCountLeftText;
    public TextMeshProUGUI enemyCountAllText;
    public Image waveCountImage;
    public Image enemyCountLeftImage;
    public Image enemyCountAllImage;

    public Image nextWaveInfoImage;
    public TextMeshProUGUI nextWaveInfoTimer;
    public TextMeshProUGUI skipWave;
    public TextMeshProUGUI seconds;
    public TextMeshProUGUI nextWaveInfoText;

    public GameObject deathScreen;
    public Image deathScreenExitButtonBG;
    public Image deathScreenExitButtonText;

    [SerializeField]
    public Animator animatorNextWave;
    [SerializeField]
    public Animator statisticAfterDeath;

    private PlayerHealth playerHealth;
    private PlayerInputActionsCode playerInputActionCode;
    private float timeLeft = 0f;

    public void Update()
    {
        if(!PauseController.instance.isPause)
        {
            if (!playerHealth.isDead)
            {
                waveCountText.SetText(currentWave.ToString());
                enemyCountLeftText.SetText(current_summons_alive.ToString());
                enemyCountAllText.SetText(allEnemies.ToString());

                WaveLoop();
            }
            else
            {
                statisticAfterDeath.SetTrigger("Show");
                deathScreen.SetActive(true);
            }
        }
    }

    public void Starting(WaveFunctionCollapse WFC)
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerInputActionCode = player.GetComponent<PlayerInputActionsCode>();
        playerHealth = player.GetComponent<PlayerHealth>();

        this.WFC = WFC;
        mapSideSize = WFC.gridOffset * WFC.size.x; // Потому что карта всегда квадратная

        current_summons_alive = 0;
        current_summons_dead = 0;
        summons_spawned = 0;
        current_summons = start_summons;
        current_max_summons_once = start_max_summons_once;
        //current_strength = start_strength;
        time_end_last_wave = Time.time;

        NewWave();

        waveCountText.SetText("0");
        enemyCountLeftText.SetText("0");
        enemyCountAllText.SetText("0");
    }

    public void NewWave()
    {
        waveIsRunning = false;
        currentWave++;
        time_end_last_wave = Time.time;
        // Обновить состояние врагов
        current_summons = (int)((float)current_summons * spawn_multiplier);
        //current_strength = current_strength * strength_multiplier;
        // Обновить данные волны
        current_summons_alive = 0;
        summons_spawned = 0;
        current_summons_dead = 0;
        timeLeft = time_between_waves;
    }

    public void WaveLoop()
    {
        // Если волна ещё идёт
        if (waveIsRunning && !PauseController.instance.isPause)
        {
            // Если не все враги появились в сцене
            if (summons_spawned < current_summons)
            {
                // Если живых врагов меньше, чем должно их быть на сцене (по макс значению)
                if (current_summons_alive < current_max_summons_once)
                {
                    // Продожить спавн врагов
                    StartCoroutine(SpawnEnemy());

                    // Увеличение счётчиков
                    current_summons_alive++;
                    summons_spawned++;

                }
                
            } // Если все враги уже появились в сцене
            else
            {
                // Проверяем, убиты ли все враги, если да, то начинаем следующую волну
                if (current_summons_dead == summons_spawned)
                {
                    // Все мертвы - новая волна
                    NewWave();
                }
            }
        } // Если волна закончилась
        else
        {
            ShowWaveTimer();

            timeLeft -= Time.deltaTime;
            nextWaveInfoTimer.SetText(
                    String.Format("{0:00}:{1:00}", 
                    Mathf.FloorToInt(timeLeft / 60),
                    Mathf.FloorToInt(timeLeft % 60))
                );

            // Если истекло время ожидания волны, то начинаем новую
            if ((Time.time > time_end_last_wave + time_between_waves || playerInputActionCode.jump) && !PauseController.instance.isPause)
            {
                timeLeft = 0f;
                nextWaveInfoTimer.SetText(
                        String.Format("{0:00}:{1:00}",
                        Mathf.FloorToInt(timeLeft / 60),
                        Mathf.FloorToInt(timeLeft % 60))
                    );

                playerInputActionCode.jump = false;

                HideWaveTimer();

                // Начинаем новую волну
                waveIsRunning = true;
            }
        }
    }

    public void HideWaveTimer()
    {
        animatorNextWave.SetBool("Show", false);
    }

    public void ShowWaveTimer()
    {
        animatorNextWave.SetBool("Show", true);
    }

    /*public void SpawnEnemy()
    {
        // Генерируем случайную позицию внутри заданного радиуса
        Vector3 randomPosition = Random.insideUnitCircle.normalized * Random.Range(spawnRadiusMin, spawnRadiusMax);
        randomPosition += new Vector3(player.transform.position.x, player.transform.position.y, 0f);

        // Проверяем, находится ли позиция внутри коллайдеров препятствий
        Collider[] colliders = Physics.OverlapSphere(randomPosition, 1f, obstacleMask);
        while (*//*colliders.Length > 0*//*true)
        {
            // Если позиция находится внутри коллайдера, генерируем новую позицию
            randomPosition = Random.insideUnitCircle.normalized * Random.Range(spawnRadiusMin, spawnRadiusMax);
            randomPosition += new Vector3(player.transform.position.x, player.transform.position.y, 0f);
            colliders = Physics.OverlapSphere(randomPosition, 1f, obstacleMask);

            // Если позиция не пересекает коллайдеры
            if(colliders.Length == 0 && IsPositionInsideMap(randomPosition))
            {
                // Создаем луч, исходящий из позиции камеры в направлении объекта
                Ray ray = new Ray(Camera.main.transform.position, randomPosition - Camera.main.transform.position);

                RaycastHit hitInfo;
                // Проверяем, пересекает ли луч какой-либо коллайдер на своем пути
                if (Physics.Linecast(ray.origin, randomPosition, out hitInfo))
                {
                    // Если луч пересекает коллайдер перед объектом, значит объект не виден
                    if (hitInfo.collider.gameObject != player.gameObject)
                    {
                        break;
                    }
                    else
                    {
                        //Debug.Log("Object is visible");
                    }
                }
                else
                {
                    // Если луч не пересекает коллайдеры, объект виден
                    //Debug.Log("Object is visible");
                }
            }
        }

        int indexToSpawn = GetIndexEnemyToSpawn();

        // Заспавниваем объект на полученной позиции
        NightPool.Spawn(enemies[indexToSpawn], randomPosition, Quaternion.identity);

        // Increase Data
        current_summons_alive++;
        summons_spawned++;
    }*/

    /*public void SpawnEnemy()
    {
        // Генерируем случайную позицию внутри заданного радиуса
        Vector3 randomPosition = player.position + Random.insideUnitSphere * spawnRadiusMax;
        //randomPosition += new Vector3(player.transform.position.x, player.transform.position.y, 0f);

        // Проверяем, находится ли позиция внутри коллайдеров препятствий
        Collider[] colliders = Physics.OverlapSphere(randomPosition, 1f, obstacleMask);
        while (*//*colliders.Length > 0*//*true)
        {
            // Если позиция находится внутри коллайдера, генерируем новую позицию
            randomPosition = player.position + Random.insideUnitSphere * spawnRadiusMax;
            colliders = Physics.OverlapSphere(randomPosition, 1f, obstacleMask);

            // Если позиция не пересекает коллайдеры
            if (colliders.Length == 0 && IsPositionInsideMap(randomPosition) && IsPositionFarPlayer(randomPosition))
            {
                // Создаем луч, исходящий из позиции камеры в направлении объекта
                Ray ray = new Ray(Camera.main.transform.position, randomPosition - Camera.main.transform.position);

                RaycastHit hitInfo;
                // Проверяем, пересекает ли луч какой-либо коллайдер на своем пути
                if (Physics.Linecast(ray.origin, randomPosition, out hitInfo))
                {
                    // Если луч пересекает коллайдер перед объектом, значит объект не виден
                    if (hitInfo.collider.gameObject != player.gameObject)
                    {
                        break;
                    }
                    else
                    {
                        //Debug.Log("Object is visible");
                    }
                }
                else
                {
                    // Если луч не пересекает коллайдеры, объект виден
                    //Debug.Log("Object is visible");
                }
            }
        }

        int indexToSpawn = GetIndexEnemyToSpawn();

        // Заспавниваем объект на полученной позиции
        GameObject enemy = NightPool.Spawn(enemies[indexToSpawn], randomPosition, Quaternion.identity);
        enemy.transform.position = new Vector3(enemy.transform.position.x, 0f, enemy.transform.position.z);
        // Increase Data
        current_summons_alive++;
        summons_spawned++;
    }*/

    IEnumerator SpawnEnemy()
    {
        yield return new WaitForSeconds(0.1f);

        bool get_correct_point = false;
        Vector3 randomPoint = Vector3.zero;

        while(!get_correct_point)
        {
            NavMeshHit hit;

            NavMesh.SamplePosition(Random.insideUnitSphere * spawnRadiusMax + player.position, out hit, spawnRadiusMax, NavMesh.AllAreas);
            randomPoint = hit.position;

            // Спавн только в случае, когда игрок не видит

            if ((randomPoint.y >= 0 && randomPoint.y <= 0.1) && IsPositionFarPlayer(randomPoint) && IsPositionNearPlayer(randomPoint) && Physics.OverlapSphere(randomPoint, 1f, obstacleMask).Length == 0)
            {
                get_correct_point = true;
            }
        }

        

        int indexToSpawn = GetIndexEnemyToSpawn();

        // Заспавниваем объект на полученной позиции
        //GameObject enemy = NightPool.Spawn(enemies[indexToSpawn], player.parent, Quaternion.identity);
        //StartCoroutine(testc(enemy, randomPoint));
        GameObject enemy = NightPool.Spawn(enemies[indexToSpawn], randomPoint, Quaternion.identity);
        //enemiesE[indexToSpawn].transform.parent
        //Instantiate(enemies[indexToSpawn], randomPoint, Quaternion.identity);

        //Enemy tempy = enemy.transform.GetComponentInChildren<Enemy>();
        //tempy.SetRandomPointToSpawn(randomPoint);
        //tempy.SpawnEnemy();
		
        //enemy.transform.position = new Vector3(enemy.transform.position.x, 0f, enemy.transform.position.z);
        // Increase Data
        //current_summons_alive++;
        //summons_spawned++;
    }
    IEnumerator testc(GameObject enemy, Vector3 randomPoint)
    {
        yield return new WaitForSeconds(1f);
        enemy.transform.SetPositionAndRotation(randomPoint, Quaternion.identity);
        //enemy.transform.SetParent(WFC.gameObject.transform, false);
    }
    private bool IsPositionFarPlayer(Vector3 position)
    {
        if (Vector3.Distance(player.transform.position, position) >= spawnRadiusMin)
        {
            return true;
        } 
        else
        {
            return false;
        }
    }
	
	private bool IsPositionNearPlayer(Vector3 position)
    {
        if (Vector3.Distance(player.transform.position, position) <= spawnRadiusMax)
        {
            return true;
        } 
        else
        {
            return false;
        }
    }

    private bool IsPositionInsideMap(Vector3 position)
    {
        // Проверяем, находится ли позиция внутри карты (квадрата)
        if (Mathf.Abs(position.x) > mapSideSize || Mathf.Abs(position.z) > mapSideSize)
        {
            return false;
        }

        return true;
    }

    private int GetIndexEnemyToSpawn()
    {
        int total = 0;
        foreach (int weight in enemiesWeight)
            total += weight;

        total = Random.Range(0, total);

        float weightSum = 0f;

        // Итерируем по объектам и сравниваем их веса с сгенерированным случайным числом
        for (int i = 0; i < enemiesWeight.Length; i++)
        {
            weightSum += enemiesWeight[i];

            // Если сумма весов превышает сгенерированное число, спавним объект
            if (weightSum >= total)
            {
                return i;
            }
        }

        return 0;
    }
}
