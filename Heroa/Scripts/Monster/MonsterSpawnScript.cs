using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterSpawner
{
    public string MonsterName;
    public float firstProbability;
    public float lastProbability;
}

public class MonsterSpawnScript : MonoBehaviour
{

    public static MonsterSpawnScript instance;

    [SerializeField]
    private int maxProbability = 100; // 확률
    [SerializeField]
    private GameObject spawnPoint = null;

    [SerializeField]
    private MonsterSpawner[] monsterSpawners;

    private float probability = 0f;
    private GameObject prefab_Monster;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        FirstRandomMonsterSpawn();
    }

    public void FirstRandomMonsterSpawn()
    {
        probability = Random.Range(1, maxProbability); // float은 최댓값 포함

        for (int i = 0; i < monsterSpawners.Length; i++)
        {
            if (probability >= monsterSpawners[i].firstProbability && probability < monsterSpawners[i].lastProbability + 1) // 마지막 확률 포함
            {
                prefab_Monster = Resources.Load<GameObject>("Prefabs/MonsterPrefabs/" + monsterSpawners[i].MonsterName);
                InstantiateMonster();
            }
        }
    }

    public void RandomMonsterSpawn()
    {
        probability = Random.Range(1, maxProbability); // float은 최댓값 포함

        for (int i = 0; i < monsterSpawners.Length; i++)
        {
            if (probability >= monsterSpawners[i].firstProbability && probability < monsterSpawners[i].lastProbability + 1) // 마지막 확률 포함
            {
                prefab_Monster = Resources.Load<GameObject>("Prefabs/MonsterPrefabs/" + monsterSpawners[i].MonsterName);
                StartCoroutine(WaitSpawnMonster());
            }
        }
    }

    public void MonsterSpawn(string _MonsterName)
    {
        prefab_Monster = Resources.Load<GameObject>("Prefabs/MonsterPrefabs/" + _MonsterName);
        StartCoroutine(WaitSpawnMonster());
    }

    // 이거랑
    IEnumerator WaitSpawnMonster()
    {
        if (prefab_Monster.GetComponent<MonsterScript>().GetIsBoss())
        {
            StartCoroutine(GameManager.instance.ShowBossAlarm());
            
        }

        yield return new WaitForSeconds(2f);
        InstantiateMonster();
    }

    // 이거랑 합쳐도 되지 않을까
    private void InstantiateMonster()
    {
        // 몬스터 소환 및 단어 재생성, 타이머 시작

        Instantiate(prefab_Monster, spawnPoint.transform);
        TimerScript.instance.ResetTime();
        TimerScript.instance.timerOn = true;
        GameManager.instance.SetCanWordChecking(true);

        if (WordCollector.instance == null)
            Debug.LogError("No WordCollector instance");

        WordCollector.instance.SetIsCollecting(false);
        GameManager.instance.SetIsMonsterDead(false);
    }
}
