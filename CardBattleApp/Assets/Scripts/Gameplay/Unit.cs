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
    public float bonusAtk, bonusDef, bonusSpeed;
    public float damageMultiplier, damageTakenMultiplier;

    // ─── Efectos globales ─────────────────────────────────────────────────────
    public float baseLifesteal;
    public float baseRegen;      // 0.01 = 1 % HP/s
    public float healingPower;   // multiplicador de curación recibida

    // ─── Timers de estado ────────────────────────────────────────────────────
    public float antiHealTimer;
    public float vulnerabilityTimer;
    public float commanderBuffTimer;
    public float duelistRegenBuffTimer;
    public float stunTimer;
    public float slowTimer;
    public float slowPercent;        // 0.10 = −10 % speed

    // ─── Ship buffs (aplicados por aura mientras la nave aliada esté activa) ──
    public float shipAtkBonus;       // bonus flat de ATK por la nave
    public float shipSpeedBonus;     // bonus flat de Speed por la nave
    // Burn (Solar Vanguard): daño por segundo
    public float burnTimer;
    public float burnDps;
    // Void ship debuffs sobre esta unidad
    public float voidAntiHealTimer;  // reduce healing recibido 60 %
    public float voidDefDebuffTimer; // reduce DEF 10 %
    public float voidSlowTimer;      // −15 % speed

    // ─── Contador de proyectiles (SOL-9 Stagger) ──────────────────────────────
    public int projectileHitCount;

    // ─── Combate ──────────────────────────────────────────────────────────────
    public float attackProgress;
    public float skillCooldown, currentSkillTimer;

    public Vector2 logicalPosition;

    // ─── Propiedades ──────────────────────────────────────────────────────────
    public bool IsDead => currentHp <= 0;

    public float FinalAtk => Mathf.Max(0, baseAtk + bonusAtk + shipAtkBonus);
    public float FinalDef => Mathf.Max(0,
        baseDef + bonusDef - (voidDefDebuffTimer > 0 ? baseDef * 0.10f : 0f));

    public float FinalSpeed
    {
        get
        {
            float s = baseSpeed + bonusSpeed + shipSpeedBonus;
            if (slowTimer > 0) s *= (1f - slowPercent);
            if (voidSlowTimer > 0) s *= 0.85f;   // −15 % de la nave Void
            return Mathf.Max(0.1f, s);
        }
    }

    // ─── Constructor ─────────────────────────────────────────────────────────
    public Unit(int ownerId, CardID cardId, int row, int slotIndex,
                float hp, float atk, float def, float speed, Vector2 spawnPos,
                float lifesteal = 0f, float regen = 0f, float healPower = 0f)
    {
        this.ownerId = ownerId;
        this.cardId = cardId;
        this.row = row;
        this.slotIndex = slotIndex;
        maxHp = currentHp = hp;
        baseAtk = atk;
        baseDef = def;
        baseSpeed = speed;
        logicalPosition = spawnPos;

        baseLifesteal = lifesteal;
        baseRegen = regen;
        healingPower = healPower;

        damageMultiplier = 1f;
        damageTakenMultiplier = 1f;
    }

    // ─── Daño ─────────────────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        currentHp -= amount;
    }

    // ─── Curación (Anti-Heal + HealingPower) ──────────────────────────────────
    public void Heal(float amount)
    {
        if (IsDead) return;

        float finalHeal = amount * (1f + healingPower);

        // Anti-heal: el más severo gana (no acumulan)
        float antiHealMult = 1f;
        if (antiHealTimer > 0) antiHealMult = Mathf.Min(antiHealMult, 0.50f);
        if (voidAntiHealTimer > 0) antiHealMult = Mathf.Min(antiHealMult, 0.40f); // −60 %
        finalHeal *= antiHealMult;

        currentHp = Mathf.Min(maxHp, currentHp + finalHeal);
    }
}