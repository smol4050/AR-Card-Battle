using UnityEngine;
using System;

public class CharacterCombat : MonoBehaviour
{
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public int AttackPower { get; private set; }
    public bool IsDead { get; private set; }

    public event Action OnHealthChanged;
    public event Action OnDeath;

    public void InitializeStats(int maxHealth, int attackPower)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        AttackPower = attackPower;
        IsDead = false;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            IsDead = true;
            OnDeath?.Invoke();
        }

        OnHealthChanged?.Invoke();
    }
}