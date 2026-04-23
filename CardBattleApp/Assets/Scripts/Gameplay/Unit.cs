using UnityEngine;

[System.Serializable]
public class Unit
{
    public int ownerId;
    public CardID cardId;

    // Stats Base
    public float maxHp, currentHp;
    public float baseAtk, baseDef, baseSpeed;

    // Stats Dinámicos Temporales
    public float bonusAtk, bonusDef, bonusSpeed, damageMultiplier, damageTakenMultiplier;

    // Sistema de Tiempo y Habilidades
    public float attackProgress;
    public float skillCooldown, currentSkillTimer;
    public float passiveTimer; // Para pasivas basadas en tiempo (Ej. Sollar Fuerza)
    public float stunTimer;

    // Posición Lógica
    public Vector2 logicalPosition;

    public bool IsDead => currentHp <= 0;

    // Propiedades de stats finales calculados en tiempo real
    public float FinalAtk => Mathf.Max(0, baseAtk + bonusAtk);
    public float FinalDef => Mathf.Max(0, baseDef + bonusDef);
    public float FinalSpeed => Mathf.Max(0.1f, baseSpeed + bonusSpeed);

    public Unit(int ownerId, CardID cardId, float hp, float atk, float def, float speed, Vector2 spawnPos)
    {
        this.ownerId = ownerId;
        this.cardId = cardId;
        this.maxHp = hp;
        this.currentHp = hp;
        this.baseAtk = atk;
        this.baseDef = def;
        this.baseSpeed = speed;
        this.logicalPosition = spawnPos;

        this.damageMultiplier = 1.0f;
        this.damageTakenMultiplier = 1.0f;
        this.attackProgress = 0f;
        this.currentSkillTimer = 0f;
        this.passiveTimer = 0f;
        this.stunTimer = 0f;
    }

    public void TakeDamage(float amount)
    {
        currentHp -= amount;
    }
}