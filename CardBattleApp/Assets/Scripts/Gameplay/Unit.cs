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

    // ─── SISTEMA GLOBAL DE EFECTOS ───────────────────────────────────────────
    public float baseLifesteal;
    public float baseRegen;      // decimal: 0.01 = 1% HP/s
    public float healingPower;   // amplificador de curación recibida

    // ─── TEMPORIZADORES DE BUFFS Y DEBUFFS ───────────────────────────────────
    public float antiHealTimer;
    public float vulnerabilityTimer;
    public float commanderBuffTimer;
    public float duelistRegenBuffTimer;
    public float stunTimer;

    // 🔴 NUEVO — Micro-Slow (SOL-9 Rework)
    // Reduce FinalSpeed en un porcentaje mientras el timer sea > 0
    public float slowTimer;
    public float slowPercent;   // ej. 0.10 = -10% speed

    // 🔴 NUEVO — Contador de impactos de proyectil en el tick de skill actual
    // Se usa para activar Stagger si ≥ 2 proyectiles conectan al mismo target
    public int projectileHitCount;

    // ─── COMBATE ─────────────────────────────────────────────────────────────
    public float attackProgress;
    public float skillCooldown, currentSkillTimer;
    public float passiveTimer;

    public Vector2 logicalPosition;

    // ─── PROPIEDADES ─────────────────────────────────────────────────────────
    public bool IsDead => currentHp <= 0;

    public float FinalAtk => Mathf.Max(0, baseAtk + bonusAtk);
    public float FinalDef => Mathf.Max(0, baseDef + bonusDef);

    // FinalSpeed aplica slow si el timer sigue activo
    public float FinalSpeed
    {
        get
        {
            float s = baseSpeed + bonusSpeed;
            if (slowTimer > 0) s *= (1f - slowPercent);
            return Mathf.Max(0.1f, s);
        }
    }

    // ─── CONSTRUCTOR ─────────────────────────────────────────────────────────
    public Unit(int ownerId, CardID cardId, int row, int slotIndex,
                float hp, float atk, float def, float speed, Vector2 spawnPos,
                float lifesteal = 0f, float regen = 0f, float healPower = 0f)
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

        this.damageMultiplier = 1f;
        this.damageTakenMultiplier = 1f;
        this.attackProgress = 0f;
        this.currentSkillTimer = 0f;
        this.passiveTimer = 0f;

        this.antiHealTimer = 0f;
        this.vulnerabilityTimer = 0f;
        this.commanderBuffTimer = 0f;
        this.duelistRegenBuffTimer = 0f;
        this.stunTimer = 0f;
        this.slowTimer = 0f;
        this.slowPercent = 0f;
        this.projectileHitCount = 0;
    }

    // ─── DAÑO ────────────────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        currentHp -= amount;
    }

    // ─── CURACIÓN (Anti-Heal + HealingPower) ─────────────────────────────────
    public void Heal(float amount)
    {
        if (IsDead) return;

        float finalHeal = amount * (1f + healingPower);
        if (antiHealTimer > 0) finalHeal *= 0.5f;   // 🔴 Anti-Heal 50%

        currentHp = Mathf.Min(maxHp, currentHp + finalHeal);
    }
}