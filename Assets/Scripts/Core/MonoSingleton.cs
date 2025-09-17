using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>();
                if (instance == null)
                {
                    Debug.LogWarning($"No instance of {typeof(T)} found, creating a new one.");
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    instance = singletonObject.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
            AwakeAfter();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void AwakeAfter()
    {
        // Optional: Override this method in derived classes for initialization after Awake
    }
}
