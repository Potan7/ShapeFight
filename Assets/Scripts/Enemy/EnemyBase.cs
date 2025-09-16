using UnityEngine;

public class EnemyBase : MonoBehaviour, IHealthComponent
{
    [Header("Stats")]
    public float currentHealth = 50f;
    public float maxHealth = 50f;

    public float CurrentHealth => currentHealth;

    public float MaxHealth => maxHealth;

    public bool IsAlive => currentHealth > 0;

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