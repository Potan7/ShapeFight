using System.Threading.Tasks;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Transform firePoint;
    public GameObject bulletPrefab; // 발사할 총알 프리팹
    public float fireRate = 5f; // 초당 발사 횟수
    public bool CanFire { get; private set; } = true;

    void Start()
    {
        // Pooler에게 자신의 총알 풀을 미리 생성하도록 등록
        ObjectPooler.Instance.Release(ObjectPooler.Instance.Get(bulletPrefab));
    }

    public void RotateGun(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Fire()
    {
        // CanFire 상태를 먼저 false로 만들어, 쿨타임 동안 중복 발사를 방지합니다.
        CanFire = false;

        GameObject bulletObject = ObjectPooler.Instance.Get(bulletPrefab);

        // 총알 스크립트에 원본 프리팹 정보 전달
        if (bulletObject.TryGetComponent<Bullet>(out var bulletComponent))
        {
            bulletComponent.Fire(firePoint.right, firePoint.position, bulletComponent.speed, bulletComponent.lifeTime);
        }
        
        // 쿨타임 비동기 메서드를 호출합니다.
        _ = FireCooldown();
    }

    private async Task FireCooldown()
    {
        // fireRate를 기반으로 쿨타임(밀리초) 계산
        await Task.Delay((int)(1000 / fireRate));
        CanFire = true;
    }
}
