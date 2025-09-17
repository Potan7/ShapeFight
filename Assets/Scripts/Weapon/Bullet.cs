using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float damage = 10f;
    public Rigidbody2D rb;

    // 이 총알의 원본 프리팹. 서버에서 풀에 반환할 때 필요합니다.
    public GameObject OriginalPrefab { get; set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Fire 메서드를 ClientRpc로 변경합니다.
    [ClientRpc]
    public void FireClientRpc(Vector2 dir, Vector3 pos, float speed, float damage)
    {
        // 이제 이 코드는 모든 클라이언트에서 실행됩니다.
        transform.position = pos; // Rigidbody 위치 대신 Transform 위치를 설정
        rb.linearVelocity = dir.normalized * speed; // velocity 대신 rb.velocity 사용
        this.damage = damage;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsServer)
        {
            // Despawn을 호출하면 모든 클라이언트에서 OnNetworkDespawn이 트리거됩니다.
            NetworkObject.Despawn(false); // 즉시 파괴하지 않음

            // 충돌한 객체가 IHitable 인터페이스를 구현하는지 확인
            if (collision.gameObject.TryGetComponent(out IHitable hitable))
            {
                hitable.Hit(this);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        // Despawn될 때 물리적 속도를 0으로 만들어 다음 사용을 대비합니다.
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 서버에서만 이 로직을 실행하여 풀에 객체를 반환합니다.
        if (IsServer)
        {
            ObjectPooler.Instance.ReturnObject(this.NetworkObject, OriginalPrefab);
        }
    }
}
