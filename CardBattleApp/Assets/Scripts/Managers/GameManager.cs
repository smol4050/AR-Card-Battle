using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public PlayerManager[] players = new PlayerManager[2];
    public List<Unit>[] activeUnits = new List<Unit>[2];

    public RoundPhase currentPhase { get; private set; }
    public bool[] isPlayerReady = new bool[2];

    // ─── EVENTOS ─────────────────────────────────────────────────────────────
    public event Action<int, Unit> OnUnitSpawned;
    public event Action<int, Unit> OnUnitDied;
    public event Action<RoundPhase> OnPhaseChanged;
    public event Action<int, bool> OnPlayerReadyStatusChanged;
    public event Action<int, string> OnLogMessage;
    public event Action<Unit, Unit, string> OnVisualEffectRequested;

    // El VisualController escucha este evento para iniciar la animación.
    // El daño NO se aplica aquí — se aplica cuando el Visual llama ApplyAttackDamage.
    // Payload: (attacker, target, damageCallback)
    public event Action<Unit, Unit, Action> OnAttackAnimationRequested;

    // SOL-9: igual que OnAttackAnimationRequested pero para cada proyectil de la ráfaga.
    // Payload: (caster, target, projectileIndex, damageCallback)
    public event Action<Unit, Unit, int, Action> OnSOL9ProjectileRequested;

    public event Action<Unit, CardID> OnUnitSkillCast;

    // ─── INICIALIZACIÓN ───────────────────────────────────────────────────────
    public void InitializeGame()
    {
        players[0] = new PlayerManager(0);
        players[1] = new PlayerManager(1);
        activeUnits[0] = new List<Unit>();
        activeUnits[1] = new List<Unit>();
        StartNewRound();
    }

    public void StartNewRound()
    {
        currentPhase = RoundPhase.Preparation;
        isPlayerReady[0] = false;
        isPlayerReady[1] = false;
        players[0].StartTurn();
        players[1].StartTurn();
        OnPhaseChanged?.Invoke(currentPhase);
        LogMessage(-1, "<color=yellow>Fase de Preparación: Despliega tus tropas.</color>");
    }

    private void Update()
    {
        if (currentPhase == RoundPhase.Combat)
            ProcessCombatTick(Time.deltaTime);
    }

    // ─── LISTO ────────────────────────────────────────────────────────────────
    public void SetPlayerReady(int playerId)
    {
        if (currentPhase != RoundPhase.Preparation) return;
        isPlayerReady[playerId] = true;
        OnPlayerReadyStatusChanged?.Invoke(playerId, true);

        if (isPlayerReady[0] && isPlayerReady[1])
        {
            currentPhase = RoundPhase.Combat;
            OnPhaseChanged?.Invoke(currentPhase);
            LogMessage(-1, "<color=red>¡GUERRA DECLARADA! Conteo de unidades activo.</color>");
        }
    }

    // ─── JUGAR CARTA ──────────────────────────────────────────────────────────
    public bool PlayCard(int playerId, CardID cardId, Vector2 dummyPos, int row, int slotIndex, int cost = 1)
    {
        if (currentPhase != RoundPhase.Preparation) return false;

        bool isCommanderCard = cardId == CardID.SollarCommander || cardId == CardID.VoidCommander;
        if (isCommanderCard && activeUnits[playerId].Exists(
            u => (u.cardId == CardID.SollarCommander || u.cardId == CardID.VoidCommander) && !u.IsDead))
            return false;

        if (!players[playerId].ConsumeEnergy(cost)) return false;

        // ── VoidHorde: 1 costo → 4 unidades en slots libres ─────────────────
        if (cardId == CardID.VoidHorde)
        {
            List<int> freeSlots = new List<int>();
            for (int s = 0; s < 6 && freeSlots.Count < 4; s++)
                if (!activeUnits[playerId].Exists(u => u.slotIndex == s && !u.IsDead))
                    freeSlots.Add(s);

            foreach (int assignedSlot in freeSlots)
            {
                int assignedRow = (assignedSlot < 3) ? 0 : 1;
                Unit hordeUnit = new Unit(playerId, cardId, assignedRow, assignedSlot,
                                          70f, 20f, 10f, 1.30f, Vector2.zero,
                                          lifesteal: 0.15f);
                activeUnits[playerId].Add(hordeUnit);
                OnUnitSpawned?.Invoke(playerId, hordeUnit);
            }
            return true;
        }

        // ── Cartas normales: 1 costo → 1 unidad ──────────────────────────────
        if (activeUnits[playerId].Exists(u => u.slotIndex == slotIndex && !u.IsDead)) return false;

        Unit newUnit = null;
        switch (cardId)
        {
            case CardID.SollarDuelist:
                newUnit = new Unit(playerId, cardId, row, slotIndex,
                                   260f, 38f, 55f, 0.95f, Vector2.zero,
                                   lifesteal: 0.10f, regen: 0.01f);
                newUnit.skillCooldown = 5f;
                break;
            case CardID.SollarCommander:
                newUnit = new Unit(playerId, cardId, row, slotIndex,
                                   320f, 25f, 70f, 0.80f, Vector2.zero,
                                   regen: 0.02f, healPower: 0.25f);
                newUnit.skillCooldown = 8f;
                break;
            case CardID.SollarForce:
                newUnit = new Unit(playerId, cardId, row, slotIndex,
                                   125f, 48f, 25f, 1.10f, Vector2.zero);
                newUnit.skillCooldown = 5f;
                break;
            case CardID.VoidCommander:
                newUnit = new Unit(playerId, cardId, row, slotIndex,
                                   190f, 28f, 35f, 0.95f, Vector2.zero,
                                   lifesteal: 0.08f);
                newUnit.skillCooldown = 7f;
                break;
            case CardID.VoidHeavyShooter:
                newUnit = new Unit(playerId, cardId, row, slotIndex,
                                   115f, 65f, 18f, 1.05f, Vector2.zero,
                                   lifesteal: 0.12f);
                newUnit.skillCooldown = 6f;
                break;
        }

        if (newUnit != null)
        {
            activeUnits[playerId].Add(newUnit);
            OnUnitSpawned?.Invoke(playerId, newUnit);
            return true;
        }
        return false;
    }

    // ─── TICK DE COMBATE ──────────────────────────────────────────────────────
    public void ProcessCombatTick(float deltaTime)
    {
        if (currentPhase != RoundPhase.Combat) return;

        UpdateAurasAndPassives();
        ProcessTeamTicks(0, 1, deltaTime);
        ProcessTeamTicks(1, 0, deltaTime);

        CleanUpDeadUnits(0);
        CleanUpDeadUnits(1);

        CheckWinCondition();
    }

    private void UpdateAurasAndPassives()
    {
        foreach (var list in activeUnits)
            foreach (var u in list)
            {
                u.bonusSpeed = 0;
                u.damageMultiplier = 1f;
                if (u.commanderBuffTimer > 0)
                {
                    u.damageMultiplier *= 1.20f;
                    u.bonusSpeed += 0.15f;
                }
            }

        int hordeCount = activeUnits[1].FindAll(u => u.cardId == CardID.VoidHorde && !u.IsDead).Count;
        float hordeMultiplier = 1f;
        if (hordeCount == 2) hordeMultiplier = 1.20f;
        else if (hordeCount == 3) hordeMultiplier = 1.35f;
        else if (hordeCount >= 4) hordeMultiplier = 1.50f;

        foreach (Unit u in activeUnits[1])
            if (u.cardId == CardID.VoidHorde) u.damageMultiplier *= hordeMultiplier;
    }

    private void ProcessTeamTicks(int attackerTeamId, int defenderTeamId, float deltaTime)
    {
        foreach (Unit attacker in activeUnits[attackerTeamId])
        {
            if (attacker.IsDead) continue;

            if (attacker.antiHealTimer > 0) attacker.antiHealTimer -= deltaTime;
            if (attacker.vulnerabilityTimer > 0) attacker.vulnerabilityTimer -= deltaTime;
            if (attacker.commanderBuffTimer > 0) attacker.commanderBuffTimer -= deltaTime;
            if (attacker.duelistRegenBuffTimer > 0) attacker.duelistRegenBuffTimer -= deltaTime;
            if (attacker.slowTimer > 0) attacker.slowTimer -= deltaTime;

            float regenRate = attacker.baseRegen;
            if (attacker.commanderBuffTimer > 0) regenRate += 0.04f;
            if (attacker.duelistRegenBuffTimer > 0) regenRate += 0.02f;
            if (regenRate > 0 && attacker.currentHp < attacker.maxHp)
                attacker.Heal(attacker.maxHp * regenRate * deltaTime);

            if (attacker.stunTimer > 0)
            {
                attacker.stunTimer -= deltaTime;
                continue;
            }

            if (attacker.skillCooldown > 0)
            {
                attacker.currentSkillTimer += deltaTime;
                if (attacker.currentSkillTimer >= attacker.skillCooldown)
                {
                    ExecuteSkill(attacker, defenderTeamId);
                    attacker.currentSkillTimer = 0f;
                }
            }

            attacker.attackProgress += attacker.FinalSpeed * deltaTime;
            if (attacker.attackProgress >= 1f)
            {
                Unit target = GetTarget(attacker, defenderTeamId);
                if (target != null)
                    RequestAttackAnimation(attacker, target);
                else
                    players[defenderTeamId].TakeDamage(Mathf.RoundToInt(attacker.FinalAtk));

                attacker.attackProgress -= 1f;
            }
        }
    }

    // ─── SOLICITUD DE ANIMACIÓN (ataque básico) ───────────────────────────────
    // No aplica daño aquí. Calcula cuánto daño hará y lo empaqueta en una Action
    // que el VisualController llamará cuando la bala/puño llegue al objetivo.
    private void RequestAttackAnimation(Unit attacker, Unit target)
    {
        // Capturamos todo lo que necesitamos en el closure para que sea válido
        // aunque el estado del juego cambie durante la animación.
        float savedDamageMultiplier = attacker.damageMultiplier;
        float savedLifesteal = attacker.baseLifesteal;
        bool savedVulnActive = target.vulnerabilityTimer > 0;
        bool savedVoidOwner = attacker.ownerId == 1;
        int defenderTeamId = 1 - attacker.ownerId;

        Action onImpact = () =>
        {
            // Guard: si alguno murió durante la animación, cancelamos el daño
            if (attacker.IsDead || target.IsDead) return;

            float mitigationFactor = 100f / (100f + target.FinalDef);
            float realDamage = attacker.FinalAtk * mitigationFactor * savedDamageMultiplier;
            if (savedVulnActive) realDamage *= 1.25f;

            target.TakeDamage(realDamage);

            float activeLifesteal = savedLifesteal;
            if (savedVoidOwner && savedVulnActive) activeLifesteal += 0.10f;
            if (activeLifesteal > 0) attacker.Heal(realDamage * activeLifesteal);

            LogMessage(attacker.ownerId,
                $"<color=white>{attacker.cardId} → {target.cardId}: -{realDamage:F0} dmg</color>");

            // Limpieza y revisión de muerte inline (sin esperar al siguiente tick)
            CleanUpDeadUnits(0);
            CleanUpDeadUnits(1);
            CheckWinCondition();
        };

        OnAttackAnimationRequested?.Invoke(attacker, target, onImpact);
    }

    // ─── SKILLS ───────────────────────────────────────────────────────────────
    private void ExecuteSkill(Unit caster, int enemyTeamId)
    {
        OnUnitSkillCast?.Invoke(caster, caster.cardId);

        switch (caster.cardId)
        {
            // Sollar Duelist — AoE melee: el daño también se difiere al impacto
            case CardID.SollarDuelist:
                {
                    // Recopilamos los objetivos en rango AHORA (antes de la animación)
                    List<Unit> targetsInRange = new List<Unit>();
                    foreach (Unit enemy in activeUnits[enemyTeamId])
                        if (!enemy.IsDead && Vector2.Distance(caster.logicalPosition, enemy.logicalPosition) <= 2.5f)
                            targetsInRange.Add(enemy);

                    if (targetsInRange.Count == 0) break;

                    Action onSpinImpact = () =>
                    {
                        float totalDamageDealt = 0;
                        foreach (Unit enemy in targetsInRange)
                        {
                            if (enemy.IsDead) continue;
                            enemy.TakeDamage(55f);
                            totalDamageDealt += 55f;
                        }
                        if (totalDamageDealt > 0)
                        {
                            caster.Heal(totalDamageDealt * 0.15f);
                            caster.duelistRegenBuffTimer = 2.0f;
                        }
                        CleanUpDeadUnits(0); CleanUpDeadUnits(1); CheckWinCondition();
                    };

                    // Reutilizamos el canal de animación con un "target" representativo
                    // para que el VisualController sepa dónde animar el spin.
                    // El callback de impacto afectará a TODOS los targets en rango.
                    OnAttackAnimationRequested?.Invoke(caster, targetsInRange[0], onSpinImpact);
                    break;
                }

            // Sollar Commander — buff instantáneo (no necesita animación de impacto)
            case CardID.SollarCommander:
                {
                    foreach (Unit ally in activeUnits[caster.ownerId])
                        if (!ally.IsDead && Vector2.Distance(caster.logicalPosition, ally.logicalPosition) <= 5f)
                            ally.commanderBuffTimer = 4.0f;
                    break;
                }

            // SOL-9 "Aegis Unit" — Disruption Barrage
            // Cada proyectil tiene su propio callback de impacto diferido.
            case CardID.SollarForce:
                {
                    Unit barTarget = GetTarget(caster, enemyTeamId);
                    if (barTarget == null) break;

                    const int PROJECTILES = 3;
                    const float DMG_PER = 25f;
                    const float ANTI_HEAL_DUR = 3f;
                    const float SLOW_DUR = 2f;
                    const float SLOW_AMT = 0.10f;
                    const float STAGGER_DUR = 0.5f;
                    const int STAGGER_THRESHOLD = 2;

                    // Reset del contador ANTES de lanzar los proyectiles
                    barTarget.projectileHitCount = 0;

                    for (int p = 0; p < PROJECTILES; p++)
                    {
                        // Capturamos p por valor en el closure
                        int projIndex = p;

                        Action onProjectileImpact = () =>
                        {
                            if (caster.IsDead || barTarget.IsDead) return;

                            float mitigation = 100f / (100f + barTarget.FinalDef);
                            barTarget.TakeDamage(DMG_PER * mitigation);

                            barTarget.antiHealTimer = ANTI_HEAL_DUR;
                            barTarget.slowTimer = SLOW_DUR;
                            barTarget.slowPercent = SLOW_AMT;
                            barTarget.projectileHitCount++;

                            // Stagger al segundo impacto (no esperamos al tercero)
                            if (barTarget.projectileHitCount == STAGGER_THRESHOLD)
                            {
                                barTarget.stunTimer = STAGGER_DUR;
                                LogMessage(-1,
                                    $"<color=cyan>[SOL-9] Stagger aplicado a {barTarget.cardId}!</color>");
                            }

                            LogMessage(-1,
                                $"<color=cyan>[SOL-9] Impacto {projIndex + 1}/3 → {barTarget.cardId}</color>");

                            CleanUpDeadUnits(0); CleanUpDeadUnits(1); CheckWinCondition();
                        };

                        OnSOL9ProjectileRequested?.Invoke(caster, barTarget, p, onProjectileImpact);
                    }
                    break;
                }

            // Void Commander — debuff instantáneo
            case CardID.VoidCommander:
                {
                    Unit targetCom = GetTarget(caster, enemyTeamId);
                    if (targetCom != null) targetCom.vulnerabilityTimer = 4.0f;
                    break;
                }

            // Void Heavy Shooter — daño diferido al impacto del proyectil
            case CardID.VoidHeavyShooter:
                {
                    Unit targetShooter = GetTarget(caster, enemyTeamId);
                    if (targetShooter == null) break;

                    bool hadDebuffAtCast = targetShooter.antiHealTimer > 0
                                        || targetShooter.vulnerabilityTimer > 0
                                        || targetShooter.stunTimer > 0
                                        || targetShooter.slowTimer > 0;

                    Action onShooterImpact = () =>
                    {
                        if (targetShooter.IsDead) return;
                        float finalDmg = hadDebuffAtCast ? 75f * 1.30f : 75f;
                        targetShooter.TakeDamage(finalDmg);
                        CleanUpDeadUnits(0); CleanUpDeadUnits(1); CheckWinCondition();
                    };

                    OnAttackAnimationRequested?.Invoke(caster, targetShooter, onShooterImpact);
                    break;
                }
        }
    }

    // ─── SELECCIÓN DE OBJETIVO ────────────────────────────────────────────────
    private Unit GetTarget(Unit attacker, int enemyTeamId)
    {
        bool isFrontlineAlive = activeUnits[enemyTeamId].Exists(u => !u.IsDead && u.row == 0);
        int targetRow = isFrontlineAlive ? 0 : 1;

        Unit bestTarget = null;
        float closestDist = float.MaxValue;

        foreach (Unit enemy in activeUnits[enemyTeamId])
        {
            if (enemy.IsDead) continue;
            if (enemy.row != targetRow) continue;
            float dist = Vector2.Distance(attacker.logicalPosition, enemy.logicalPosition);
            if (dist < closestDist) { closestDist = dist; bestTarget = enemy; }
        }
        return bestTarget;
    }

    // ─── LIMPIEZA ─────────────────────────────────────────────────────────────
    private void CleanUpDeadUnits(int playerId)
    {
        for (int i = activeUnits[playerId].Count - 1; i >= 0; i--)
        {
            if (activeUnits[playerId][i].IsDead)
            {
                Unit fallen = activeUnits[playerId][i];
                OnUnitDied?.Invoke(playerId, fallen);
                activeUnits[playerId].RemoveAt(i);
                LogMessage(playerId, $"<color=gray>Unidad {fallen.cardId} eliminada.</color>");
            }
        }
    }

    // ─── CONDICIÓN DE VICTORIA ────────────────────────────────────────────────
    private void CheckWinCondition()
    {
        if (currentPhase != RoundPhase.Combat) return;

        int p0Count = activeUnits[0].Count;
        int p1Count = activeUnits[1].Count;

        if (p0Count == 0 || p1Count == 0)
        {
            currentPhase = RoundPhase.End;

            if (p0Count == 0 && p1Count == 0)
                LogMessage(-1, "<b><color=white>RESULTADO: EMPATE</color></b>");
            else if (p0Count == 0)
                LogMessage(-1, "<b><color=purple>RESULTADO: VICTORIA DEL VACÍO</color></b>");
            else
                LogMessage(-1, "<b><color=orange>RESULTADO: VICTORIA SOLAR</color></b>");

            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    public void LogMessage(int playerId, string message)
    {
        OnLogMessage?.Invoke(playerId, message);
    }
}