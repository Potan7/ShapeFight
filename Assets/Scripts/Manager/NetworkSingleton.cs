using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkBehaviour를 위한 싱글턴 기본 클래스.
/// OnNetworkSpawn 시점에 인스턴스를 설정합니다.
/// </summary>
/// <typeparam name="T">싱글턴으로 만들 NetworkBehaviour 클래스</typeparam>
public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
    public static T Instance { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"중복된 {typeof(T)} 인스턴스를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        Instance = this as T;

        // 씬 전환 시 파괴되지 않도록 설정 (필요에 따라 사용)
        // DontDestroyOnLoad(gameObject);

        OnSingletonReady();
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 싱글턴 인스턴스가 준비된 후 호출되는 가상 메서드.
    /// 초기화 로직이 필요할 경우 자식 클래스에서 오버라이드하여 사용합니다.
    /// </summary>
    protected virtual void OnSingletonReady()
    {
        // 자식 클래스에서 구현
    }
}
