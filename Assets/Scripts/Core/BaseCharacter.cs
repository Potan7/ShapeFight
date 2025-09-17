using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class BaseCharacter : NetworkBehaviour, IHitable
{
    [Header("Base Stats")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float maxSpeed = 10f;
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected int maxAmmo = 30;

    // --- Components & State ---
    protected Rigidbody2D rb;
    protected Gun gun;
    protected int currentAmmo;

    // --- Network Variables ---
    protected NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    protected NetworkVariable<NetworkObjectReference> gunReference = new NetworkVariable<NetworkObjectReference>();

    #region Unity & Network Lifecycle
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        // 체력 변경 시 콜백 등록
        currentHealth.OnValueChanged += OnHealthChanged;

        // 총 참조 변경 시 콜백 등록
        gunReference.OnValueChanged += (prev, current) =>
        {
            if (current.TryGet(out NetworkObject gunNetworkObject))
            {
                OnGunSpawned(gunNetworkObject.GetComponent<Gun>());
            }
        };

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
        currentAmmo = maxAmmo;
    }
    #endregion

    #region Core Logic
    /// <summary>
    /// 캐릭터에게 물리적인 힘을 가해 움직입니다.
    /// </summary>
    protected void Move(Vector2 direction)
    {
        rb.AddForce(direction * moveSpeed);
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    /// <summary>
    /// 총 발사를 시도합니다. 성공 시 true를 반환합니다.
    /// </summary>
    protected bool TryFire()
    {
        if (gun == null || gun.IsReloading || currentAmmo <= 0) return false;

        if (gun.Fire())
        {
            currentAmmo--;
            OnAmmoChanged(currentAmmo, maxAmmo);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 재장전을 시작합니다.
    /// </summary>
    protected async UniTask Reload()
    {
        if (gun == null || gun.IsReloading) return;

        OnReloadStarted();
        await gun.Reload();
        currentAmmo = maxAmmo;
        OnAmmoChanged(currentAmmo, maxAmmo);
    }

    /// <summary>
    /// 총알에 맞았을 때 호출됩니다. 서버에서만 체력을 변경합니다.
    /// </summary>
    public void Hit(Bullet bullet)
    {
        if (!IsServer) return;

        currentHealth.Value -= bullet.damage;
        if (currentHealth.Value <= 0)
        {
            OnDeath();
        }
    }
    #endregion

    #region Virtual Methods for Overriding
    // 자식 클래스(PlayerController, EnemyController)에서 UI 업데이트 등을 위해 오버라이드할 수 있는 가상 메서드들
    protected virtual void OnHealthChanged(float previousValue, float newValue) { }
    protected virtual void OnAmmoChanged(int current, int max) { }
    protected virtual void OnReloadStarted() { }
    protected virtual void OnGunSpawned(Gun newGun) { this.gun = newGun; }
    protected virtual void OnDeath() { Debug.Log($"{gameObject.name} has been killed."); }
    #endregion
}