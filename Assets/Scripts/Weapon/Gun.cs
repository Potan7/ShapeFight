using Cysharp.Threading.Tasks;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public GameObject bulletPrefab; // 발사할 총알 프리팹

    [Header("Settings")]
    public float fireRate = 5f; // 초당 발사 횟수
    public float recoilStrength = 5f; // 반동 세기
    public float bulletSpeed = 20f; // 총알 속도
    public float bulletDamage = 10f; // 총알 피해량
    public int maxAmmo = 30; // 최대 탄약 수
    public int initialPoolSize = 5; // 미리 생성해 둘 총알 개수

    public bool CanFire { get; private set; } = true;

    void Start()
    {
        // ObjectPooler에게 자신의 총알 풀을 미리 생성하도록 요청
        ObjectPooler.Instance.PrewarmPool(bulletPrefab, initialPoolSize);
    }

    public void RotateGun(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public bool Fire()
    {
        if (CanFire == false) return false;
        // CanFire 상태를 먼저 false로 만들어, 쿨타임 동안 중복 발사를 방지합니다.
        CanFire = false;

        GameObject bulletObject = ObjectPooler.Instance.Get(bulletPrefab);

        // 총알 스크립트에 원본 프리팹 정보 전달
        if (bulletObject.TryGetComponent<Bullet>(out var bulletComponent))
        {
            bulletComponent.Fire(firePoint.right, firePoint.position, bulletSpeed, bulletDamage);
        }

        // 쿨타임 비동기 메서드를 호출합니다.
        FireCooldown().Forget();
        return true;
    }

    private async UniTaskVoid FireCooldown()
    {
        // fireRate를 기반으로 쿨타임(밀리초) 계산
        await UniTask.Delay((int)(1000 / fireRate));
        CanFire = true;
    }
}
