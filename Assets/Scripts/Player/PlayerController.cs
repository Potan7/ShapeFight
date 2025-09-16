using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour, IHealthComponent
{
    [Header("References")]
    public InputActionAsset inputActionAsset;
    public GameObject tempGunPrefab;
    // public PlayerWorldCanvas worldCanvas;
    PlayerMapCanvas mapCanvas;

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
        rb = GetComponent<Rigidbody2D>();
        mapCanvas = FindAnyObjectByType<PlayerMapCanvas>();

        // gunReference 값이 변경될 때 로컬 gun 변수를 설정하는 콜백을 등록합니다.
        gunReference.OnValueChanged += (prev, current) =>
        {
            if (current.TryGet(out NetworkObject gunNetworkObject))
            {
                gun = gunNetworkObject.GetComponent<Gun>();
            }
        };

        if (!IsOwner) return;

        // 서버에게 총을 생성하고 스폰해달라고 요청합니다.
        // ServerRpc에 매개변수를 전달할 필요가 없습니다. Netcode가 자동으로 처리합니다.
        SpawnGunServerRpc();

        // --- (기존 입력 설정 코드) ---
        var playerActionMap = inputActionAsset.FindActionMap("Player");

        var moveAction = playerActionMap.FindAction("Move");
        moveAction.performed += ctx => moveDirection = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveDirection = Vector2.zero;

        var attackAction = playerActionMap.FindAction("Attack");
        // '누르기 시작했을 때' isAttacking을 true로 설정
        attackAction.started += _ => isAttacking = true;
        // '버튼을 뗐을 때' isAttacking을 false로 설정
        attackAction.canceled += _ => isAttacking = false;

        // Gun의 IsReloading 상태를 확인하여 중복 재장전 방지
        playerActionMap.FindAction("Reload").performed += ctx => 
        { 
            if (gun != null && !gun.IsReloading) Reload().Forget(); 
        };

        // "Player" 액션맵의 모든 액션을 활성화합니다.
        playerActionMap.Enable();
    }

    [ServerRpc]
    private void SpawnGunServerRpc(ServerRpcParams rpcParams = default)
    {
        // 1. 서버에서 총 프리팹을 인스턴스화합니다.
        var gunInstance = Instantiate(tempGunPrefab);
        NetworkObject gunNetworkObject = gunInstance.GetComponent<NetworkObject>();

        // 2. 이 RPC를 호출한 클라이언트에게 소유권을 부여하며 스폰합니다.
        gunNetworkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId);

        // 3. 플레이어의 자식으로 설정합니다.
        gunNetworkObject.TrySetParent(transform, false);

        // 4. NetworkVariable에 스폰된 총의 참조를 저장하여 모든 클라이언트에게 전파합니다.
        gunReference.Value = gunNetworkObject;
    }

    void Update()
    {
        if (!IsOwner || gun == null) return; // gun이 스폰되기 전까지 Update 로직을 실행하지 않도록 방어

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        gun.RotateGun(mousePosition);

        // Gun의 IsReloading 상태를 확인
        if (isAttacking && !gun.IsReloading)
        {
            if (currentAmmo <= 0)
            {
                Reload().Forget();
                return;
            }

            if (gun.Fire())
            {
                // 발사에 성공했을 때만 반동 효과를 적용합니다.
                Vector2 recoilDirection = (transform.position - mousePosition).normalized;
                rb.AddForce(recoilDirection * gun.recoilStrength, ForceMode2D.Impulse);

                currentAmmo = Math.Max(0, currentAmmo - 1); // 탄약 감소, 0 미만으로는 떨어지지 않도록
                mapCanvas.UpdateAmmo(currentAmmo, gun.maxAmmo);
            }
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        rb.AddForce(moveDirection * moveSpeed);

        // 현재 속도가 maxSpeed를 초과하면 속도를 maxSpeed로 제한합니다.
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    async UniTaskVoid Reload()
    {
        // 이미 재장전 중이면 실행하지 않음
        if (gun.IsReloading) return;

        mapCanvas.ReloadAmmo(); // 미니맵 UI 업데이트는 여기서 계속 처리

        // Gun의 재장전 프로세스가 끝날 때까지 기다립니다.
        await gun.Reload();
        
        currentAmmo = gun.maxAmmo; // 탄약을 최대치로 회복
        mapCanvas.UpdateAmmo(currentAmmo, gun.maxAmmo);
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
