using UnityEngine;

[System.Serializable]
public class Unit
{
    public int ownerId;
    public CardID cardId;
    public int row;
    public int slotIndex; // NUEVO: Identificador de la casilla que ocupa (0 a 5)

    public float maxHp, currentHp;
    public float baseAtk, baseDef, baseSpeed;
    public float bonusAtk, bonusDef, bonusSpeed, damageMultiplier, damageTakenMultiplier;

    public float attackProgress;
    public float skillCooldown, currentSkillTimer;
    public float passiveTimer;
    public float stunTimer;

    public Vector2 logicalPosition;

    public bool IsDead => currentHp <= 0;

    public float FinalAtk => Mathf.Max(0, baseAtk + bonusAtk);
    public float FinalDef => Mathf.Max(0, baseDef + bonusDef);
    public float FinalSpeed => Mathf.Max(0.1f, baseSpeed + bonusSpeed);

    // Actualizamos el constructor para recibir el parámetro 'slotIndex'
    public Unit(int ownerId, CardID cardId, int row, int slotIndex, float hp, float atk, float def, float speed, Vector2 spawnPos)
    {
        this.ownerId = ownerId;
        this.cardId = cardId;
        this.row = row;
        this.slotIndex = slotIndex;
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