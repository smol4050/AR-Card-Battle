using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    public PlayerManager[] players = new PlayerManager[2];
    public List<Unit>[] activeUnits = new List<Unit>[2];

    // Temporizador de combate
    public float combatTimer { get; private set; }
    public bool isSuddenDeathActive { get; private set; }
    public float suddenDeathMultiplier = 1f;

    // Naves activas (máximo 1 por bando por ronda)
    private ShipInstance[] _activeShips = new ShipInstance[2];
    private bool[] _shipUsedThisRound = new bool[2];

    public RoundPhase currentPhase { get; private set; }
    public bool[] isPlayerReady = new bool[2];

    // ─── EVENTOS ─────────────────────────────────────────────────────────────
    public event Action<int, Unit> OnUnitSpawned;
    public event Action<int, Unit> OnUnitDied;
    public event Action<RoundPhase> OnPhaseChanged;
    public event Action<int, bool> OnPlayerReadyStatusChanged;
    public event Action<int, string> OnLogMessage;

    // Animación de ataque: el daño se aplica cuando la bala/golpe impacta.
    // Payload: (attacker, target, onImpactCallback)
    public event Action<Unit, Unit, Action> OnAttackAnimationRequested;

    // SOL-9 Disruption Barrage: un evento por proyectil.
    // Payload: (caster, target, projectileIndex, onImpactCallback)
    public event Action<Unit, Unit, int, Action> OnSOL9ProjectileRequested;

    public event Action<Unit, CardID> OnUnitSkillCast;

    // Nave spawneada/expirada
    // Payload: (ownerId, ship)
    public event Action<int, ShipInstance> OnShipSpawned;
    public event Action<int, ShipInstance> OnShipExpired;

    // Pulso de nave disparado (para animación)
    // Payload: (ownerId, ship, pulseIndex, targetsHit)
    public event Action<int, ShipInstance, int, List<Unit>> OnShipPulseFired;

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
        _activeShips[0] = null;
        _activeShips[1] = null;
        _shipUsedThisRound[0] = false;
        _shipUsedThisRound[1] = false;

        // Reiniciar variables de combate
        combatTimer = 0f;
        isSuddenDeathActive = false;
        suddenDeathMultiplier = 1f;

        // Dar 10 de energía inicial
        players[0].energy = 10;
        players[1].energy = 10;

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
            LogMessage(-1, "<color=red>¡GUERRA DECLARADA!</color>");
        }
    }

    // ─── JUGAR CARTA ──────────────────────────────────────────────────────────
    public bool PlayCard(int playerId, CardID cardId, Vector2 logicalPos, int row, int slotIndex, int cost = 1)
    {
        if (!GameplayRules.CanPlayCard(currentPhase, players[playerId].energy, cost)) return false;

        bool isShip = cardId == CardID.SolarVanguard || cardId == CardID.AbyssReaper;
        if (isShip)
        {
            if (currentPhase != RoundPhase.Combat) return false;
            if (_shipUsedThisRound[playerId]) return false;
            if (!players[playerId].ConsumeEnergy(cost)) return false;

            SpawnShip(playerId, cardId, logicalPos);
            return true;
        }

        if (currentPhase != RoundPhase.Preparation) return false;

        bool isCommanderCard = cardId == CardID.SollarCommander || cardId == CardID.VoidCommander;
        if (isCommanderCard && activeUnits[playerId].Exists(
            u => (u.cardId == CardID.SollarCommander || u.cardId == CardID.VoidCommander) && !u.IsDead))
            return false;

        // VoidHorde: 1 costo -> 4 unidades en EL MISMO SLOT
        if (cardId == CardID.VoidHorde)
        {
            if (activeUnits[playerId].Exists(u => u.slotIndex == slotIndex && !u.IsDead)) return false; // El slot debe estar libre
            if (!players[playerId].ConsumeEnergy(cost)) return false;

            for (int i = 0; i < 4; i++)
            {
                // Offset visual para que no colisionen exactamente en el mismo pixel
                Vector2 offset = new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.4f, 0.4f));
                Unit hordeUnit = new Unit(playerId, cardId, row, slotIndex,
                                           70f, 20f, 10f, 1.30f, logicalPos + offset, lifesteal: 0.15f);
                activeUnits[playerId].Add(hordeUnit);
                OnUnitSpawned?.Invoke(playerId, hordeUnit);
            }
            return true;
        }

        if (activeUnits[playerId].Exists(u => u.slotIndex == slotIndex && !u.IsDead)) return false;
        if (!players[playerId].ConsumeEnergy(cost)) return false;

        Unit newUnit = CreateUnit(playerId, cardId, row, slotIndex);
        if (newUnit == null) return false;

        activeUnits[playerId].Add(newUnit);
        OnUnitSpawned?.Invoke(playerId, newUnit);
        return true;
    }

    // ─── FACTORY DE UNIDADES ──────────────────────────────────────────────────
    private Unit CreateUnit(int playerId, CardID cardId, int row, int slotIndex)
    {
        Unit u = null;
        float hpScale = 3f;

        switch (cardId)
        {
            case CardID.SollarDuelist:
                u = new Unit(playerId, cardId, row, slotIndex, 260f * hpScale, 38f, 55f, 0.95f, Vector2.zero, lifesteal: 0.10f, regen: 0.01f);
                u.skillCooldown = 5f;
                break;
            case CardID.SollarCommander:
                u = new Unit(playerId, cardId, row, slotIndex, 320f * hpScale, 25f, 70f, 0.80f, Vector2.zero, regen: 0.02f, healPower: 0.25f);
                u.skillCooldown = 8f;
                break;
            case CardID.SollarForce:
                u = new Unit(playerId, cardId, row, slotIndex, 125f * hpScale, 48f, 25f, 1.10f, Vector2.zero);
                u.skillCooldown = 5f;
                break;
            case CardID.VoidCommander:
                u = new Unit(playerId, cardId, row, slotIndex, 190f * hpScale, 28f, 35f, 0.95f, Vector2.zero, lifesteal: 0.08f);
                u.skillCooldown = 7f;
                break;
            case CardID.VoidHeavyShooter:
                u = new Unit(playerId, cardId, row, slotIndex, 115f * hpScale, 65f, 18f, 1.05f, Vector2.zero, lifesteal: 0.12f);
                u.skillCooldown = 6f;
                break;
        }
        return u;
    }

    // ─── SPAWN DE NAVE ────────────────────────────────────────────────────────
    private void SpawnShip(int playerId, CardID cardId, Vector2 logicalPos)
    {
        ShipInstance ship;

        if (cardId == CardID.SolarVanguard)
        {
            // 3 ráfagas cada 1.5 s, buffs aliados +20% ATK +10 Speed
            ship = new ShipInstance(
                id: cardId,
                owner: playerId,
                dur: 5f,
                pulses: 3,
                interval: 1.5f,
                center: logicalPos,
                atkBuff: 0.20f,
                speedBuff: 0.10f);
        }
        else // AbyssReaper
        {
            // 5 pulsos cada 1 s, sin buffs aliados propios (los debuffs van en el tick)
            ship = new ShipInstance(
                id: cardId,
                owner: playerId,
                dur: 5f,
                pulses: 5,
                interval: 1.0f,
                center: logicalPos);
        }

        _activeShips[playerId] = ship;
        _shipUsedThisRound[playerId] = true;

        LogMessage(playerId, $"<color=yellow>[SHIP] {cardId} desplegada por {0.5f}s de cast time.</color>");
        OnShipSpawned?.Invoke(playerId, ship);
    }

    // ─── TICK DE COMBATE ──────────────────────────────────────────────────────
    public void ProcessCombatTick(float deltaTime)
    {
        if (currentPhase != RoundPhase.Combat) return;

        // Lógica de Muerte Súbita
        combatTimer += deltaTime;
        if (combatTimer >= 30f && !isSuddenDeathActive)
        {
            isSuddenDeathActive = true;
            suddenDeathMultiplier = 3f; // Todo es 3 veces más rápido/fuerte
            LogMessage(-1, "<b><color=red>¡TIEMPO AGOTADO! MUERTE SÚBITA ACTIVADA</color></b>");
        }

        UpdateShips(deltaTime);
        UpdateAurasAndPassives();
        ProcessTeamTicks(0, 1, deltaTime * suddenDeathMultiplier); // Aceleramos los ticks
        ProcessTeamTicks(1, 0, deltaTime * suddenDeathMultiplier);

        CleanUpDeadUnits(0);
        CleanUpDeadUnits(1);
        CheckWinCondition();
    }

    // ─── NAVES — UPDATE ───────────────────────────────────────────────────────
    private void UpdateShips(float deltaTime)
    {
        for (int i = 0; i < 2; i++)
        {
            ShipInstance ship = _activeShips[i];
            if (ship == null) continue;

            ship.elapsed += deltaTime;
            ship.pulseTimer += deltaTime;

            // Aplicar buffs de nave Sollar a aliados en cada tick
            if (ship.cardId == CardID.SolarVanguard)
                ApplySolarVanguardAura(ship);

            // Pulso
            if (ship.pulseTimer >= ship.pulseInterval && ship.pulsesFired < ship.totalPulses)
            {
                ship.pulseTimer -= ship.pulseInterval;
                FireShipPulse(ship, i);
            }

            if (ship.IsExpired)
            {
                // Limpiar buffs de nave al expirar
                if (ship.cardId == CardID.SolarVanguard)
                    ClearSolarVanguardAura(i);

                OnShipExpired?.Invoke(i, ship);
                LogMessage(i, $"<color=gray>[SHIP] {ship.cardId} ha expirado.</color>");
                _activeShips[i] = null;
            }
        }
    }

    // Buff de aura: se recalcula cada tick (no se acumula)
    private void ApplySolarVanguardAura(ShipInstance ship)
    {
        foreach (Unit ally in activeUnits[ship.ownerId])
        {
            if (ally.IsDead) continue;
            ally.shipAtkBonus = ally.baseAtk * ship.atkBuffPercent;
            ally.shipSpeedBonus = ship.speedBuff;
        }
    }

    private void ClearSolarVanguardAura(int ownerId)
    {
        foreach (Unit ally in activeUnits[ownerId])
        {
            ally.shipAtkBonus = 0f;
            ally.shipSpeedBonus = 0f;
        }
    }

    private void FireShipPulse(ShipInstance ship, int ownerIdx)
    {
        int enemyIdx = 1 - ownerIdx;
        int pulseIndex = ship.pulsesFired;
        ship.pulsesFired++;

        List<Unit> targetsHit = new List<Unit>();

        if (ship.cardId == CardID.SolarVanguard)
        {
            // Ráfaga de área: golpea todos los enemigos en el campo
            // (el diseño dice "prioriza zona con más enemigos" — con todas las unidades en 6 slots
            //  el área siempre cubre el tablero completo; se puede refinar con radio si se añade posición)
            foreach (Unit enemy in activeUnits[enemyIdx])
            {
                if (enemy.IsDead) continue;
                float dmg = 25f * (100f / (100f + enemy.FinalDef));
                enemy.TakeDamage(dmg);

                // Burn: 5 dps durante 3s (no stackea, solo refresca)
                enemy.burnTimer = 3f;
                enemy.burnDps = 5f;

                targetsHit.Add(enemy);
            }
            LogMessage(ownerIdx,
                $"<color=orange>[Solar Vanguard] Ráfaga {pulseIndex + 1}/3 — {targetsHit.Count} impactos</color>");
        }
        else // AbyssReaper
        {
            // Pulso de área: 10 daño + debuffs a todos los enemigos en el tablero
            foreach (Unit enemy in activeUnits[enemyIdx])
            {
                if (enemy.IsDead) continue;
                float dmg = 10f * (100f / (100f + enemy.FinalDef));
                enemy.TakeDamage(dmg);

                // Debuffs (no stackean, se refrescan)
                enemy.voidAntiHealTimer = ship.pulseInterval + 0.1f; // dura hasta el siguiente pulso
                enemy.voidDefDebuffTimer = ship.pulseInterval + 0.1f;
                enemy.voidSlowTimer = ship.pulseInterval + 0.1f;

                targetsHit.Add(enemy);
            }
            LogMessage(ownerIdx,
                $"<color=purple>[Abyss Reaper] Pulso {pulseIndex + 1}/5 — {targetsHit.Count} afectados</color>");
        }

        OnShipPulseFired?.Invoke(ownerIdx, ship, pulseIndex, targetsHit);

        CleanUpDeadUnits(0);
        CleanUpDeadUnits(1);
    }

    // ─── AURAS Y PASIVOS ──────────────────────────────────────────────────────
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
                // Ship buffs se recalculan en UpdateShips; aquí solo reseteamos
                // si la nave expiró (ClearSolarVanguardAura ya lo hace).
            }

        // Swarm Rage (Void Horde)
        int hordeCount = activeUnits[1].FindAll(u => u.cardId == CardID.VoidHorde && !u.IsDead).Count;
        float hordeMultiplier = 1f;
        if (hordeCount == 2) hordeMultiplier = 1.20f;
        else if (hordeCount == 3) hordeMultiplier = 1.35f;
        else if (hordeCount >= 4) hordeMultiplier = 1.50f;

        foreach (Unit u in activeUnits[1])
            if (u.cardId == CardID.VoidHorde) u.damageMultiplier *= hordeMultiplier;
    }

    // ─── TICK POR EQUIPO ──────────────────────────────────────────────────────
    private void ProcessTeamTicks(int attackerTeamId, int defenderTeamId, float deltaTime)
    {
        foreach (Unit attacker in activeUnits[attackerTeamId])
        {
            if (attacker.IsDead) continue;

            TickTimers(attacker, deltaTime);
            TickRegen(attacker, deltaTime);
            TickBurn(attacker, deltaTime);

            if (attacker.stunTimer > 0)
            {
                attacker.stunTimer -= deltaTime;
                continue;
            }

            TickSkill(attacker, defenderTeamId, deltaTime);
            TickBasicAttack(attacker, defenderTeamId, deltaTime);
        }
    }

    private void TickTimers(Unit u, float dt)
    {
        if (u.antiHealTimer > 0) u.antiHealTimer -= dt;
        if (u.vulnerabilityTimer > 0) u.vulnerabilityTimer -= dt;
        if (u.commanderBuffTimer > 0) u.commanderBuffTimer -= dt;
        if (u.duelistRegenBuffTimer > 0) u.duelistRegenBuffTimer -= dt;
        if (u.slowTimer > 0) u.slowTimer -= dt;
        if (u.voidAntiHealTimer > 0) u.voidAntiHealTimer -= dt;
        if (u.voidDefDebuffTimer > 0) u.voidDefDebuffTimer -= dt;
        if (u.voidSlowTimer > 0) u.voidSlowTimer -= dt;
        if (u.burnTimer > 0) u.burnTimer -= dt;
    }

    private void TickRegen(Unit u, float dt)
    {
        float rate = u.baseRegen;
        if (u.commanderBuffTimer > 0) rate += 0.04f;
        if (u.duelistRegenBuffTimer > 0) rate += 0.02f;
        if (rate > 0 && u.currentHp < u.maxHp)
            u.Heal(u.maxHp * rate * dt);
    }

    private void TickBurn(Unit u, float dt)
    {
        // El burn ya se decrementó en TickTimers; aplicamos el daño si sigue activo
        if (u.burnDps > 0 && u.burnTimer > 0)
            u.TakeDamage(u.burnDps * dt);
        else if (u.burnTimer <= 0)
            u.burnDps = 0f;
    }

    private void TickSkill(Unit attacker, int defenderTeamId, float dt)
    {
        if (attacker.skillCooldown <= 0) return;
        attacker.currentSkillTimer += dt;
        if (attacker.currentSkillTimer >= attacker.skillCooldown)
        {
            ExecuteSkill(attacker, defenderTeamId);
            attacker.currentSkillTimer = 0f;
        }
    }

    private void TickBasicAttack(Unit attacker, int defenderTeamId, float dt)
    {
        attacker.attackProgress += attacker.FinalSpeed * dt;
        if (attacker.attackProgress < 1f) return;

        Unit target = GetTarget(attacker, defenderTeamId);
        if (target != null)
            RequestAttackAnimation(attacker, target);
        else
            players[defenderTeamId].TakeDamage(Mathf.RoundToInt(attacker.FinalAtk));

        attacker.attackProgress -= 1f;
    }

    // ─── SOLICITUD DE ANIMACIÓN (ataque básico) ───────────────────────────────
    private void RequestAttackAnimation(Unit attacker, Unit target)
    {
        float snapDmgMult = attacker.damageMultiplier;
        float snapLifesteal = attacker.baseLifesteal;
        bool snapVuln = target.vulnerabilityTimer > 0;
        bool isVoidOwner = attacker.ownerId == 1;

        Action onImpact = () =>
        {
            if (attacker.IsDead || target.IsDead) return;

            float mitigation = 100f / (100f + target.FinalDef);
            float dmg = attacker.FinalAtk * mitigation * snapDmgMult;
            if (snapVuln) dmg *= 1.25f;

            target.TakeDamage(dmg);

            float ls = snapLifesteal + (isVoidOwner && snapVuln ? 0.10f : 0f);
            if (ls > 0) attacker.Heal(dmg * ls);

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
            case CardID.SollarDuelist:
                {
                    List<Unit> inRange = new List<Unit>();
                    foreach (Unit e in activeUnits[enemyTeamId])
                        if (!e.IsDead && Vector2.Distance(caster.logicalPosition, e.logicalPosition) <= 2.5f)
                            inRange.Add(e);

                    if (inRange.Count == 0) break;

                    Action onSpinImpact = () =>
                    {
                        float total = 0;
                        foreach (Unit e in inRange) { if (!e.IsDead) { e.TakeDamage(55f); total += 55f; } }
                        if (total > 0) { caster.Heal(total * 0.15f); caster.duelistRegenBuffTimer = 2f; }
                        CleanUpDeadUnits(0); CleanUpDeadUnits(1); CheckWinCondition();
                    };
                    OnAttackAnimationRequested?.Invoke(caster, inRange[0], onSpinImpact);
                    break;
                }

            case CardID.SollarCommander:
                {
                    foreach (Unit ally in activeUnits[caster.ownerId])
                        if (!ally.IsDead && Vector2.Distance(caster.logicalPosition, ally.logicalPosition) <= 5f)
                            ally.commanderBuffTimer = 4f;
                    break;
                }

            // SOL-9 Disruption Barrage — 3 proyectiles, daño diferido al impacto
            case CardID.SollarForce:
                {
                    Unit barTarget = GetTarget(caster, enemyTeamId);
                    if (barTarget == null) break;

                    barTarget.projectileHitCount = 0;

                    for (int p = 0; p < 3; p++)
                    {
                        int projIndex = p;
                        Action onHit = () =>
                        {
                            if (caster.IsDead || barTarget.IsDead) return;
                            float dmg = 25f * (100f / (100f + barTarget.FinalDef));
                            barTarget.TakeDamage(dmg);
                            barTarget.antiHealTimer = 3f;
                            barTarget.slowTimer = 2f;
                            barTarget.slowPercent = 0.10f;
                            barTarget.projectileHitCount++;

                            if (barTarget.projectileHitCount == 2)
                            {
                                barTarget.stunTimer = 0.5f;
                                LogMessage(-1, $"<color=cyan>[SOL-9] Stagger → {barTarget.cardId}</color>");
                            }
                            CleanUpDeadUnits(0); CleanUpDeadUnits(1); CheckWinCondition();
                        };
                        OnSOL9ProjectileRequested?.Invoke(caster, barTarget, p, onHit);
                    }
                    break;
                }

            case CardID.VoidCommander:
                {
                    Unit t = GetTarget(caster, enemyTeamId);
                    if (t != null) t.vulnerabilityTimer = 4f;
                    break;
                }

            case CardID.VoidHeavyShooter:
                {
                    Unit t = GetTarget(caster, enemyTeamId);
                    if (t == null) break;
                    bool hadDebuff = t.antiHealTimer > 0 || t.vulnerabilityTimer > 0
                                  || t.stunTimer > 0 || t.slowTimer > 0;
                    Action onHit = () =>
                    {
                        if (t.IsDead) return;
                        t.TakeDamage(hadDebuff ? 75f * 1.30f : 75f);
                        CleanUpDeadUnits(0); CleanUpDeadUnits(1); CheckWinCondition();
                    };
                    OnAttackAnimationRequested?.Invoke(caster, t, onHit);
                    break;
                }
        }
    }

    // ─── SELECCIÓN DE OBJETIVO ────────────────────────────────────────────────
    private Unit GetTarget(Unit attacker, int enemyTeamId)
    {
        bool frontlineAlive = activeUnits[enemyTeamId].Exists(u => !u.IsDead && u.row == 0);
        int targetRow = frontlineAlive ? 0 : 1;

        Unit best = null;
        float best_dist = float.MaxValue;

        foreach (Unit e in activeUnits[enemyTeamId])
        {
            if (e.IsDead || e.row != targetRow) continue;
            float d = Vector2.Distance(attacker.logicalPosition, e.logicalPosition);
            if (d < best_dist) { best_dist = d; best = e; }
        }
        return best;
    }

    // ─── LIMPIEZA ─────────────────────────────────────────────────────────────
    private void CleanUpDeadUnits(int playerId)
    {
        for (int i = activeUnits[playerId].Count - 1; i >= 0; i--)
        {
            if (!activeUnits[playerId][i].IsDead) continue;
            Unit fallen = activeUnits[playerId][i];
            OnUnitDied?.Invoke(playerId, fallen);
            activeUnits[playerId].RemoveAt(i);
            LogMessage(playerId, $"<color=gray>{fallen.cardId} eliminada.</color>");
        }
    }

    // ─── CONDICIÓN DE VICTORIA ────────────────────────────────────────────────
    private void CheckWinCondition()
    {
        if (currentPhase != RoundPhase.Combat) return;

        // Modificación: Contar estrictamente unidades que no estén muertas
        int p0Alive = 0;
        int p1Alive = 0;
        foreach (var u in activeUnits[0]) if (!u.IsDead) p0Alive++;
        foreach (var u in activeUnits[1]) if (!u.IsDead) p1Alive++;

        if (p0Alive > 0 && p1Alive > 0) return;

        currentPhase = RoundPhase.End;

        if (p0Alive == 0 && p1Alive == 0) LogMessage(-1, "<b><color=white>EMPATE</color></b>");
        else if (p0Alive == 0) LogMessage(-1, "<b><color=purple>VICTORIA DEL VACÍO</color></b>");
        else LogMessage(-1, "<b><color=orange>VICTORIA SOLAR</color></b>");

        OnPhaseChanged?.Invoke(currentPhase);
    }

    public void LogMessage(int playerId, string msg) => OnLogMessage?.Invoke(playerId, msg);
}