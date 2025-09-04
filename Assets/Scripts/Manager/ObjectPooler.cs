using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectPooler : NetworkSingleton<ObjectPooler>
{
    [Header("Prefabs to Prewarm")]
    [SerializeField] private List<GameObject> prewarmPrefabs;
    public int InitialPoolSize = 10;

    private Dictionary<GameObject, Queue<NetworkObject>> pooledObjects = new Dictionary<GameObject, Queue<NetworkObject>>();

    protected override void OnSingletonReady()
    {
        // 씬 전환 시 파괴되지 않도록 설정
        DontDestroyOnLoad(this.gameObject);

        // 서버에서만 풀을 미리 생성합니다.
        if (IsServer)
        {
            foreach (var item in prewarmPrefabs)
            {
                if (!pooledObjects.ContainsKey(item))
                {
                    pooledObjects[item] = new Queue<NetworkObject>();
                }

                PrewarmPool(item, InitialPoolSize);
            }
        }
    }

    /// <summary>
    /// 풀에서 오브젝트를 가져옵니다. (서버 전용)
    /// </summary>
    public NetworkObject GetObject(GameObject prefab)
    {
        if (pooledObjects.TryGetValue(prefab, out var objectQueue) && objectQueue.Count > 0)
        {
            var networkObject = objectQueue.Dequeue();
            networkObject.gameObject.SetActive(true);
            return networkObject;
        }
        return Instantiate(prefab).GetComponent<NetworkObject>();
    }

    /// <summary>
    /// 오브젝트를 풀로 반환합니다. (서버 전용)
    /// </summary>
    public void ReturnObject(NetworkObject networkObject, GameObject prefab)
    {
        if (!pooledObjects.ContainsKey(prefab))
        {
            pooledObjects[prefab] = new Queue<NetworkObject>();
        }
        networkObject.gameObject.SetActive(false);
        pooledObjects[prefab].Enqueue(networkObject);
    }

    /// <summary>
    /// 풀을 미리 채워둡니다. (서버 전용)
    /// </summary>
    public void PrewarmPool(GameObject prefab, int amount)
    {
        if (!IsServer) return;

        for (int i = 0; i < amount; i++)
        {
            var instance = Instantiate(prefab);
            instance.SetActive(false);
            var networkObject = instance.GetComponent<NetworkObject>();
            pooledObjects[prefab].Enqueue(networkObject);
        }
    }
}