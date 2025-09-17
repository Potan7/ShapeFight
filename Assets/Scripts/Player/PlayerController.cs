using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour, IHealthComponent
{
    [Header("Player References")]
    public InputActionAsset inputActionAsset;
    public GameObject tempGunPrefab;
    private PlayerMapCanvas mapCanvas;

    [Header("Stats")]
    public float moveSpeed = 5f;
    public float maxSpeed = 10f;

    protected float currentHealth = 100f;
    protected float maxHealth = 100f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public bool IsAlive => currentHealth > 0;

    protected int currentAmmo = 30;

    Rigidbody2D rb;
    Vector2 moveDirection;
    Gun gun;

    private bool isAttacking = false; // 공격 중인지 상태를 저장하는 변수
    // private bool isReloading = false; // isReloading 상태는 이제 Gun이 관리하므로 PlayerController에서는 삭제합니다.

    // NetworkVariable을 사용하여 모든 클라이언트에서 gun 참조를 동기화합니다.
    private NetworkVariable<NetworkObjectReference> gunReference = new NetworkVariable<NetworkObjectReference>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // 부모의 OnNetworkSpawn 실행이 매우 중요!

        mapCanvas = FindAnyObjectByType<PlayerMapCanvas>();

        if (!IsOwner) return;

        SpawnGunServerRpc();
        SetupInput();
    }

    private void SetupInput()
    {
        var playerActionMap = inputActionAsset.FindActionMap("Player");
        
        playerActionMap.FindAction("Move").performed += ctx => moveDirection = ctx.ReadValue<Vector2>();
        playerActionMap.FindAction("Move").canceled += ctx => moveDirection = Vector2.zero;

        playerActionMap.FindAction("Attack").started += _ => isAttacking = true;
        playerActionMap.FindAction("Attack").canceled += _ => isAttacking = false;

        playerActionMap.FindAction("Reload").performed += _ => Reload().Forget();

        playerActionMap.Enable();
    }

    [ServerRpc]
    private void SpawnGunServerRpc(ServerRpcParams rpcParams = default)
    {
        var gunInstance = Instantiate(tempGunPrefab);
        var gunNetworkObject = gunInstance.GetComponent<NetworkObject>();
        gunNetworkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
        gunNetworkObject.TrySetParent(transform, false);
        gunReference.Value = gunNetworkObject;
    }

    void Update()
    {
        if (!IsOwner || gun == null) return;

        gun.RotateGun(Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()));

        if (isAttacking)
        {
            if (currentAmmo <= 0)
            {
                Reload().Forget();
                return;
            }
            if (TryFire())
            {
                // 반동 효과
                Vector2 recoilDirection = (transform.position - gun.transform.position).normalized;
                rb.AddForce(recoilDirection * gun.recoilStrength, ForceMode2D.Impulse);
            }
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        Move(moveDirection); // 부모의 Move 메서드 호출
    }

    // --- UI 업데이트를 위한 오버라이드 ---
    protected override void OnAmmoChanged(int current, int max)
    {
        if (IsOwner) mapCanvas.UpdateAmmo(current, max);
    }

    protected override void OnReloadStarted()
    {
        if (IsOwner) mapCanvas.ReloadAmmo();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }
}
