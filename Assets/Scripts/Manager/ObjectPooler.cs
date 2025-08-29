using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public interface IPoolable
{
    /// <summary>
    /// 이 오브젝트의 원본 프리팹. 어느 풀로 돌아갈지 결정하는 데 사용됩니다.
    /// </summary>
    GameObject OriginalPrefab { get; set; }

    /// <summary>
    /// 풀에서 오브젝트를 가져올 때 호출됩니다.
    /// </summary>
    void OnGetFromPool();

    /// <summary>
    /// 오브젝트를 풀로 반환할 때 호출됩니다.
    /// </summary>
    void OnReleaseToPool();
}

public class ObjectPooler : MonoSingleton<ObjectPooler>
{
    [Header("Pooling Settings")]
    public int defaultCapacity = 10; // 풀의 기본 크기
    public int maxCapacity = 20;     // 풀의 최대 크기

    // 각 프리팹에 대한 오브젝트 풀을 저장하는 딕셔너리
    private readonly Dictionary<GameObject, IObjectPool<GameObject>> poolDictionary = new Dictionary<GameObject, IObjectPool<GameObject>>();

    /// <summary>
    /// 게임 시작 시점에 특정 프리팹의 풀을 미리 생성하고 채워둡니다.
    /// </summary>
    public void PrewarmPool(GameObject prefab, int initialAmount)
    {
        // 풀이 이미 존재하면 아무것도 하지 않음
        if (poolDictionary.ContainsKey(prefab))
            return;

        // 풀을 생성하고, 생성된 오브젝트를 임시 리스트에 저장
        var pool = GetPoolFor(prefab);
        var prewarmedObjects = new GameObject[initialAmount];
        for (int i = 0; i < initialAmount; i++)
        {
            prewarmedObjects[i] = pool.Get();
        }

        // 리스트에 있는 모든 오브젝트를 다시 풀로 반환하여 비활성화 상태로 만듦
        foreach (var obj in prewarmedObjects)
        {
            pool.Release(obj);
        }
    }

    /// <summary>
    /// 지정된 프리팹으로 풀에서 오브젝트를 가져옵니다.
    /// </summary>
    /// <param name="prefab">가져올 오브젝트의 프리팹</param>
    /// <returns>풀에서 나온 활성화된 게임 오브젝트</returns>
    public GameObject Get(GameObject prefab)
    {
        return GetPoolFor(prefab).Get();
    }

    /// <summary>
    /// 오브젝트를 원래의 풀로 반환합니다.
    /// </summary>
    /// <param name="instance">반환할 게임 오브젝트 인스턴스</param>
    public void Release(GameObject instance)
    {
        // IPoolable 컴포넌트를 가져와서 원본 프리팹 정보를 얻음
        if (instance.TryGetComponent<IPoolable>(out var poolable))
        {
            if (poolDictionary.ContainsKey(poolable.OriginalPrefab))
            {
                poolDictionary[poolable.OriginalPrefab].Release(instance);
            }
            else
            {
                Debug.LogWarning($"풀에 없는 오브젝트({instance.name})를 반환하려고 합니다. 오브젝트를 파괴합니다.");
                Destroy(instance);
            }
        }
        else
        {
            Debug.LogError($"반환하려는 오브젝트({instance.name})에 IPoolable 인터페이스가 없습니다. 오브젝트를 파괴합니다.");
            Destroy(instance);
        }
    }

    /// <summary>
    /// 특정 프리팹에 대한 풀을 가져오거나, 없으면 새로 생성합니다.
    /// </summary>
    private IObjectPool<GameObject> GetPoolFor(GameObject prefab)
    {
        if (poolDictionary.TryGetValue(prefab, out var pool))
        {
            return pool;
        }

        var newPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var obj = Instantiate(prefab, transform);
                // 생성 시 IPoolable 컴포넌트에 원본 프리팹 정보 저장
                if (obj.TryGetComponent<IPoolable>(out var poolable))
                {
                    poolable.OriginalPrefab = prefab;
                }
                return obj;
            },
            actionOnGet: (obj) =>
            {
                obj.SetActive(true);
                // IPoolable 인터페이스의 초기화 메서드 호출
                obj.GetComponent<IPoolable>()?.OnGetFromPool();
            },
            actionOnRelease: (obj) =>
            {
                // IPoolable 인터페이스의 반환 메서드 호출
                obj.GetComponent<IPoolable>()?.OnReleaseToPool();
                obj.SetActive(false);
            },
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: true, // 중복 반환 체크
            defaultCapacity: defaultCapacity,
            maxSize: maxCapacity
        );

        poolDictionary.Add(prefab, newPool);
        return newPool;
    }
}