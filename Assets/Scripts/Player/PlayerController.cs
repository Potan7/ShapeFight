using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public InputActionAsset inputActionAsset;
    PlayerCanvas playerCanvas;

    [Header("Stats")]
    public float moveSpeed = 5f;
    public float maxSpeed = 10f;

    public float currentHealth = 100f;
    public float maxHealth = 100f;

    public int currentAmmo = 30;

    Rigidbody2D rb;
    Vector2 moveDirection;
    Gun gun;

    private bool isAttacking = false; // 공격 중인지 상태를 저장하는 변수
    private bool isReloading = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gun = GetComponentInChildren<Gun>();

        playerCanvas = FindAnyObjectByType<PlayerCanvas>();

        var playerActionMap = inputActionAsset.FindActionMap("Player");

        var moveAction = playerActionMap.FindAction("Move");
        moveAction.performed += ctx => moveDirection = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveDirection = Vector2.zero;

        var attackAction = playerActionMap.FindAction("Attack");
        // '누르기 시작했을 때' isAttacking을 true로 설정
        attackAction.started += _ => isAttacking = true;
        // '버튼을 뗐을 때' isAttacking을 false로 설정
        attackAction.canceled += _ => isAttacking = false;

        playerActionMap.FindAction("Reload").performed += ctx => { if (!isReloading) Reload().Forget(); };

        // "Player" 액션맵의 모든 액션을 활성화합니다.
        playerActionMap.Enable();
    }

    void Update()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        gun.RotateGun(mousePosition);

        // isAttacking이 true이면 매 프레임 발사를 시도합니다.
        if (isAttacking && !isReloading)
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
                playerCanvas.UpdateAmmo(currentAmmo, gun.maxAmmo);
            }
        }
    }

    void FixedUpdate()
    {
        rb.AddForce(moveDirection * moveSpeed);

        // 현재 속도가 maxSpeed를 초과하면 속도를 maxSpeed로 제한합니다.
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    async UniTaskVoid Reload()
    {
        isReloading = true;
        await UniTask.Delay(1000); // 1초 대기
        currentAmmo = 30; // 탄약을 최대치로 회복
        isReloading = false;
    }

}
