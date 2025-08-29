using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public InputActionAsset inputActionAsset;

    [Header("Stats")]
    public float moveSpeed = 5f;
    public float maxSpeed = 10f;                                

    Rigidbody2D rb;
    Vector2 moveDirection;
    Gun gun;
                                        
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gun = GetComponentInChildren<Gun>();

        var playerActionMap = inputActionAsset.FindActionMap("Player");
        var moveAction = playerActionMap.FindAction("Move");

        moveAction.performed += Move;
        moveAction.canceled += Move;

        var attackAction = playerActionMap.FindAction("Attack");
        attackAction.performed += Attack;

        moveAction.Enable();
    }
    void Update()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        gun.RotateGun(mousePosition);
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

    private void Attack(InputAction.CallbackContext context)
    {
        if (gun.CanFire)
        {
            gun.Fire();
        }
    }

    void Move(InputAction.CallbackContext context)
    {
        moveDirection = context.ReadValue<Vector2>();
    }

}
