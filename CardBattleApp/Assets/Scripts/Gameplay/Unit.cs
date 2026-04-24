using UnityEngine;

[System.Serializable]
public class Unit
{
    public int ownerId;
    public CardID cardId;
    public int row;
    public int slotIndex;

    public float maxHp, currentHp;
    public float baseAtk, baseDef, baseSpeed;
    public float bonusAtk, bonusDef, bonusSpeed, damageMultiplier, damageTakenMultiplier;

    // 🧪 SISTEMA GLOBAL DE EFECTOS
    public float baseLifesteal;
    public float baseRegen; // Representado en decimal (0.01 = 1% HP/sec)
    public float healingPower; // Amplificador de curación recibida

    // ⏱️ TEMPORIZADORES DE BUFFS Y DEBUFFS
    public float antiHealTimer;
    public float vulnerabilityTimer;
    public float commanderBuffTimer;
    public float duelistRegenBuffTimer;

    public float attackProgress;
    public float skillCooldown, currentSkillTimer;
    public float passiveTimer;
    public float stunTimer;

    public Vector2 logicalPosition;

    public bool IsDead => currentHp <= 0;

    public float FinalAtk => Mathf.Max(0, baseAtk + bonusAtk);
    public float FinalDef => Mathf.Max(0, baseDef + bonusDef);
    public float FinalSpeed => Mathf.Max(0.1f, baseSpeed + bonusSpeed);

    // Actualizamos el constructor para recibir Lifesteal, Regen y Healing Power
    public Unit(int ownerId, CardID cardId, int row, int slotIndex, float hp, float atk, float def, float speed, Vector2 spawnPos, float lifesteal = 0f, float regen = 0f, float healPower = 0f)
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

        this.baseLifesteal = lifesteal;
        this.baseRegen = regen;
        this.healingPower = healPower;

        this.damageMultiplier = 1.0f;
        this.damageTakenMultiplier = 1.0f;
        this.attackProgress = 0f;
        this.currentSkillTimer = 0f;
        this.passiveTimer = 0f;
        this.stunTimer = 0f;

        this.antiHealTimer = 0f;
        this.vulnerabilityTimer = 0f;
        this.commanderBuffTimer = 0f;
        this.duelistRegenBuffTimer = 0f;
    }

    public void TakeDamage(float amount)
    {
        currentHp -= amount;
    }

    // 🟢 NUEVA LÓGICA DE CURACIÓN (Soporta Anti-Heal y Healing Power)
    public void Heal(float amount)
    {
        if (IsDead) return;

        float finalHeal = amount * (1f + healingPower);

        if (antiHealTimer > 0)
            finalHeal *= 0.5f; // 🔴 Castigo del 50% por Anti-Heal

        currentHp = Mathf.Min(maxHp, currentHp + finalHeal);
    }
}