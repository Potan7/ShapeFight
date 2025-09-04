using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class Gun : NetworkBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public GameObject bulletPrefab; // 발사할 총알 프리팹
    private PlayerWorldCanvas worldCanvas; // 재장전 UI를 위한 참조

    [Header("Settings")]
    public float fireRate = 5f; // 초당 발사 횟수
    public float recoilStrength = 5f; // 반동 세기
    public float bulletSpeed = 20f; // 총알 속도
    public float bulletDamage = 10f; // 총알 피해량
    public int maxAmmo = 30; // 최대 탄약 수
    public float reloadTime = 2f;

    public bool CanFire { get; private set; } = true;
    public bool IsReloading { get; private set; } = false; // 재장전 상태 변수 추가

    void Awake()
    {
        // 자신의 하위 오브젝트에서 worldCanvas 참조를 찾습니다.
        worldCanvas = GetComponentInChildren<PlayerWorldCanvas>();
        worldCanvas.gameObject.SetActive(false); // 시작 시 비활성화
    }

    // public override void OnNetworkSpawn()
    // {
    // }

    public void RotateGun(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public bool Fire()
    {
        // 재장전 중이거나 쿨타임 중이면 발사 불가
        if (!CanFire || IsReloading) return false;
        
        CanFire = false;

        // --- 기존 클라이언트 생성 로직 삭제 ---
        // GameObject bulletObject = ObjectPooler.Instance.Get(bulletPrefab);
        // if (bulletObject.TryGetComponent<Bullet>(out var bulletComponent))
        // {
        //     bulletComponent.Fire(firePoint.right, firePoint.position, bulletSpeed, bulletDamage);
        // }
        
        // 서버에게 총알 발사를 요청합니다.
        FireServerRpc(firePoint.position, firePoint.rotation);

        // 로컬에서 쿨타임을 돌립니다.
        FireCooldown().Forget();
        return true;
    }

    /// <summary>
    /// 재장전 로직을 수행합니다.
    /// </summary>
    public async UniTask Reload()
    {
        IsReloading = true;
        CanFire = false; // 재장전 중 발사 방지
        worldCanvas.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;
            worldCanvas.UpdateReloadUI(elapsed / reloadTime);
            await UniTask.Yield();
        }

        worldCanvas.gameObject.SetActive(false);
        IsReloading = false;
        CanFire = true;
    }

    private async UniTaskVoid FireCooldown()
    {
        // fireRate를 기반으로 쿨타임(밀리초) 계산
        await UniTask.Delay((int)(1000 / fireRate));
        CanFire = true;
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 position, Quaternion rotation)
    {
        NetworkObject bulletNetworkObject = ObjectPooler.Instance.GetObject(bulletPrefab);
        
        if (bulletNetworkObject.TryGetComponent<Bullet>(out var bullet))
        {
            bullet.OriginalPrefab = bulletPrefab;
            
            // 먼저 스폰하여 모든 클라이언트에게 총알을 활성화시킵니다.
            bulletNetworkObject.Spawn(true);

            // 그 다음, ClientRpc를 호출하여 모든 클라이언트에서 발사 효과를 동시에 실행합니다.
            bullet.FireClientRpc(rotation * Vector3.right, position, bulletSpeed, bulletDamage);
        }
    }
}
