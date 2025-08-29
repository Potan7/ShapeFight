using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
    public float damage = 10f;
    public Rigidbody2D rb;

    // 이 총알의 원본 프리팹. 총을 발사할 때 설정해줍니다.
    public GameObject OriginalPrefab { get; set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Fire(Vector2 dir, Vector3 pos, float speed, float damage)
    {
        rb.position = pos;
        rb.linearVelocity = dir.normalized * speed;
        this.damage = damage;
    }

    // IPoolable 인터페이스 구현
    public void OnGetFromPool()
    {
        
    }

    public void OnReleaseToPool()
    {
        // 풀로 돌아가기 전 처리할 로직 (예: 속도 0으로 만들기)
        rb.linearVelocity = Vector2.zero;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        ObjectPooler.Instance.Release(gameObject);
    }
}
