using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class PoolManager : MonoBehaviourPunCallbacks
{
    public static PoolManager Instance { get; private set; }

    [SerializeField] private EnemyDatabase _enemyDatabase;

    [SerializeField] private string _groundItemPrefabName = "GroundItem";

    [SerializeField] private int _prewarmEnemyCount = 20;
    [SerializeField] private int _prewarmItemCount = 20;

    private readonly Dictionary<EnemyId, Queue<EnemyController>> _enemyPools = new Dictionary<EnemyId, Queue<EnemyController>>();

    private readonly Dictionary<ItemId, Queue<GameObject>> _itemPools = new Dictionary<ItemId, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeEnemyPools();
        InitializeItemPools();
    }

    private void InitializeEnemyPools()
    {
        if (_enemyDatabase == null)
        {
            return;
        }

        foreach (EnemyData data in _enemyDatabase.GetAll())
        {
            if (data.id == EnemyId.NONE)
            {
                continue;
            }

            Queue<EnemyController> queue = new Queue<EnemyController>();
            _enemyPools.Add(data.id, queue);

            for (int i = 0; i < _prewarmEnemyCount; i++)
            {
                CreateEnemyInstance(data, queue);
            }
        }
    }

    private void InitializeItemPools()
    {
        foreach (ItemId id in System.Enum.GetValues(typeof(ItemId)))
        {
            if (id == ItemId.NONE)
            {
                continue;
            }

            Queue<GameObject> queue = new Queue<GameObject>();
            _itemPools.Add(id, queue);

            for (int i = 0; i < _prewarmItemCount; i++)
            {
                CreateItemInstance(queue);
            }
        }
    }

    private void CreateEnemyInstance(EnemyData data, Queue<EnemyController> queue)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        GameObject obj = PhotonNetwork.InstantiateRoomObject(
            data.prefabName,
            Vector3.zero,
            Quaternion.identity
        );

        obj.SetActive(false);

        EnemyController enemy = obj.GetComponent<EnemyController>();
        queue.Enqueue(enemy);
    }

    private void CreateItemInstance(Queue<GameObject> queue)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        GameObject obj = PhotonNetwork.InstantiateRoomObject(
            _groundItemPrefabName,
            Vector3.zero,
            Quaternion.identity
        );

        obj.SetActive(false);
        queue.Enqueue(obj);
    }

    public EnemyController GetEnemy(EnemyId id, Vector2 position)
    {
        if (!_enemyPools.TryGetValue(id, out Queue<EnemyController> queue))
        {
            return null;
        }

        if (queue.Count == 0)
        {
            EnemyData data = _enemyDatabase.Get(id);
            CreateEnemyInstance(data, queue); //풀 부족 시 추가 생성
        }

        EnemyController enemy = queue.Dequeue();

        enemy.transform.position = position;
        enemy.gameObject.SetActive(true);

        return enemy;
    }

    public void ReturnEnemy(EnemyController enemy)
    {
        if (enemy == null)
        {
            return;
        }

        EnemyId id = enemy.DataId;

        enemy.ResetForPool();

        enemy.gameObject.SetActive(false);

        if (_enemyPools.TryGetValue(id, out Queue<EnemyController> queue))
        {
            queue.Enqueue(enemy);
        }
    }

    public GameObject GetItem(ItemId id, Vector2 position)
    {
        if (!_itemPools.TryGetValue(id, out Queue<GameObject> queue))
        {
            return null;
        }

        if (queue.Count == 0)
        {
            CreateItemInstance(queue); //부족 시 추가 생성
        }

        GameObject obj = queue.Dequeue();

        obj.transform.position = position;
        obj.SetActive(true);

        return obj;
    }

    public void ReturnItem(GameObject item)
    {
        if (item == null)
        {
            return;
        }

        GroundItemNetwork net = item.GetComponent<GroundItemNetwork>();
        if (net == null)
        {
            return;
        }

        ItemId id = net.ItemId;

        net.ResetForPool();

        item.SetActive(false);

        if (_itemPools.TryGetValue(id, out Queue<GameObject> queue))
        {
            queue.Enqueue(item);
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        enabled = PhotonNetwork.IsMasterClient;
    }
}
