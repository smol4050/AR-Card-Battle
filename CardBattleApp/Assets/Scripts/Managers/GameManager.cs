using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public PlayerManager[] players = new PlayerManager[2];
    public List<Unit>[] activeUnits = new List<Unit>[2];

    public RoundPhase currentPhase { get; private set; }
    public bool[] isPlayerReady = new bool[2];

    public event Action<int, Unit> OnUnitSpawned;
    public event Action<int, Unit> OnUnitDied;
    public event Action<Unit, Unit, float> OnUnitAttacked;
    public event Action<Unit, CardID> OnUnitSkillCast;
    public event Action<RoundPhase> OnPhaseChanged;
    public event Action<int, bool> OnPlayerReadyStatusChanged;
    public event Action<int, string> OnLogMessage;
    public event Action<Unit, Unit, string> OnVisualEffectRequested;

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
        {
            ProcessCombatTick(Time.deltaTime);
        }
    }

    public void SetPlayerReady(int playerId)
    {
        if (currentPhase != RoundPhase.Preparation) return;
        isPlayerReady[playerId] = true;
        OnPlayerReadyStatusChanged?.Invoke(playerId, true);

        if (isPlayerReady[0] && isPlayerReady[1])
        {
            currentPhase = RoundPhase.Combat;
            OnPhaseChanged?.Invoke(currentPhase);
            LogMessage(-1, "<color=red>¡QUE COMIENCE LA BATALLA!</color>");
        }
    }

    public bool PlayCard(int playerId, CardID cardId, Vector2 dummyPos, int row, int slotIndex, int cost = 1)
    {
        if (currentPhase != RoundPhase.Preparation) return false;
        if (activeUnits[playerId].Exists(u => u.slotIndex == slotIndex && !u.IsDead)) return false;

        bool isCommanderCard = cardId == CardID.SollarCommander || cardId == CardID.VoidCommander;
        if (isCommanderCard && activeUnits[playerId].Exists(u => (u.cardId == CardID.SollarCommander || u.cardId == CardID.VoidCommander) && !u.IsDead))
            return false;

        if (!players[playerId].ConsumeEnergy(cost)) return false;

        int spawnCount = (cardId == CardID.VoidHorde) ? 4 : 1;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 initialLogicalPos = Vector2.zero;
            Unit newUnit = null;

            switch (cardId)
            {
                case CardID.SollarDuelist:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 260f, 38f, 55f, 0.95f, initialLogicalPos, lifesteal: 0.10f, regen: 0.01f);
                    newUnit.skillCooldown = 5f;
                    break;
                case CardID.SollarCommander:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 320f, 25f, 70f, 0.80f, initialLogicalPos, regen: 0.02f, healPower: 0.25f);
                    newUnit.skillCooldown = 8f;
                    break;
                case CardID.SollarForce:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 110f, 60f, 20f, 1.15f, initialLogicalPos);
                    newUnit.skillCooldown = 5f;
                    break;
                case CardID.VoidHorde:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 70f, 20f, 10f, 1.30f, initialLogicalPos, lifesteal: 0.15f);
                    break;
                case CardID.VoidCommander:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 190f, 28f, 35f, 0.95f, initialLogicalPos, lifesteal: 0.08f);
                    newUnit.skillCooldown = 7f;
                    break;
                case CardID.VoidHeavyShooter:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 115f, 65f, 18f, 1.05f, initialLogicalPos, lifesteal: 0.12f);
                    newUnit.skillCooldown = 6f;
                    break;
            }

            if (newUnit != null)
            {
                activeUnits[playerId].Add(newUnit);
                OnUnitSpawned?.Invoke(playerId, newUnit);
            }
        }
        return true;
    }

    public void ProcessCombatTick(float deltaTime)
    {
        if (currentPhase != RoundPhase.Combat) return;

        UpdateAurasAndPassives();
        ProcessTeamTicks(0, 1, deltaTime);
        ProcessTeamTicks(1, 0, deltaTime);

        // 1. Limpiar unidades caídas
        CleanUpDeadUnits(0);
        CleanUpDeadUnits(1);

        // 2. Verificar condición de victoria basada en el conteo de la lista
        CheckWinCondition();
    }

    private void UpdateAurasAndPassives()
    {
        foreach (var list in activeUnits)
        {
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
        }

        int hordeCount = activeUnits[1].FindAll(u => u.cardId == CardID.VoidHorde && !u.IsDead).Count;
        float hordeMultiplier = 1f;
        if (hordeCount == 2) hordeMultiplier = 1.20f;
        else if (hordeCount == 3) hordeMultiplier = 1.35f;
        else if (hordeCount >= 4) hordeMultiplier = 1.50f;

        foreach (Unit u in activeUnits[1])
        {
            if (u.cardId == CardID.VoidHorde) u.damageMultiplier *= hordeMultiplier;
        }
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

            float currentRegenRate = attacker.baseRegen;
            if (attacker.commanderBuffTimer > 0) currentRegenRate += 0.04f;
            if (attacker.duelistRegenBuffTimer > 0) currentRegenRate += 0.02f;

            if (currentRegenRate > 0 && attacker.currentHp < attacker.maxHp)
            {
                attacker.Heal(attacker.maxHp * currentRegenRate * deltaTime);
            }

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
                Unit target = GetClosestTarget(attacker, defenderTeamId);
                if (target != null) ExecuteBasicAttack(attacker, target);
                else players[defenderTeamId].TakeDamage(Mathf.RoundToInt(attacker.FinalAtk));

                attacker.attackProgress -= 1f;
            }
        }
    }

    private void ExecuteBasicAttack(Unit attacker, Unit target)
    {
        float mitigationFactor = 100f / (100f + target.FinalDef);
        float realDamage = attacker.FinalAtk * mitigationFactor;
        realDamage *= attacker.damageMultiplier;
        if (target.vulnerabilityTimer > 0) realDamage *= 1.25f;

        target.TakeDamage(realDamage);
        OnUnitAttacked?.Invoke(attacker, target, realDamage);

        float activeLifesteal = attacker.baseLifesteal;
        if (attacker.ownerId == 1 && target.vulnerabilityTimer > 0) activeLifesteal += 0.10f;
        if (activeLifesteal > 0) attacker.Heal(realDamage * activeLifesteal);
    }

    private void ExecuteSkill(Unit caster, int enemyTeamId)
    {
        OnUnitSkillCast?.Invoke(caster, caster.cardId);
        switch (caster.cardId)
        {
            case CardID.SollarDuelist:
                float totalDamageDealt = 0;
                foreach (Unit enemy in activeUnits[enemyTeamId])
                {
                    if (!enemy.IsDead && Vector2.Distance(caster.logicalPosition, enemy.logicalPosition) <= 2.5f)
                    {
                        enemy.TakeDamage(55f);
                        totalDamageDealt += 55f;
                    }
                }
                if (totalDamageDealt > 0) { caster.Heal(totalDamageDealt * 0.15f); caster.duelistRegenBuffTimer = 2.0f; }
                break;
            case CardID.SollarCommander:
                foreach (Unit ally in activeUnits[caster.ownerId])
                {
                    if (!ally.IsDead && Vector2.Distance(caster.logicalPosition, ally.logicalPosition) <= 5f)
                        ally.commanderBuffTimer = 4.0f;
                }
                break;
            case CardID.SollarForce:
                Unit targetForce = GetClosestTarget(caster, enemyTeamId);
                if (targetForce != null)
                {
                    targetForce.TakeDamage(70f);
                    targetForce.stunTimer = 1.0f;
                    targetForce.antiHealTimer = 3.0f;
                    OnVisualEffectRequested?.Invoke(caster, targetForce, "ForceHit");
                }
                break;
            case CardID.VoidCommander:
                Unit targetCom = GetClosestTarget(caster, enemyTeamId);
                if (targetCom != null) targetCom.vulnerabilityTimer = 4.0f;
                break;
            case CardID.VoidHeavyShooter:
                Unit targetShooter = GetClosestTarget(caster, enemyTeamId);
                if (targetShooter != null)
                {
                    float finalDmg = 75f;
                    if (targetShooter.antiHealTimer > 0 || targetShooter.vulnerabilityTimer > 0 || targetShooter.stunTimer > 0) finalDmg *= 1.30f;
                    targetShooter.TakeDamage(finalDmg);
                }
                break;
        }
    }

    private Unit GetClosestTarget(Unit attacker, int enemyTeamId)
    {
        Unit bestTarget = null;
        float closestDist = float.MaxValue;
        bool isFrontlineAlive = activeUnits[enemyTeamId].Exists(u => !u.IsDead && u.row == 0);
        int targetRow = isFrontlineAlive ? 0 : 1;

        foreach (Unit enemy in activeUnits[enemyTeamId])
        {
            if (enemy.IsDead) continue;
            if (enemy.row != targetRow) continue;
            float dist = Vector2.Distance(attacker.logicalPosition, enemy.logicalPosition);
            if (dist < closestDist) { closestDist = dist; bestTarget = enemy; }
        }
        return bestTarget;
    }

    private void CleanUpDeadUnits(int playerId)
    {
        for (int i = activeUnits[playerId].Count - 1; i >= 0; i--)
        {
            if (activeUnits[playerId][i].IsDead)
            {
                Unit fallen = activeUnits[playerId][i];
                OnUnitDied?.Invoke(playerId, fallen);
                activeUnits[playerId].RemoveAt(i);
                LogMessage(playerId, $"<color=gray>La unidad {fallen.cardId} ha caído en combate.</color>");
            }
        }
    }

    private void CheckWinCondition()
    {
        // Conteo de unidades vivas por bando
        int playerUnitsCount = activeUnits[0].Count;
        int aiUnitsCount = activeUnits[1].Count;

        if (playerUnitsCount == 0 || aiUnitsCount == 0)
        {
            currentPhase = RoundPhase.End;

            if (playerUnitsCount == 0 && aiUnitsCount == 0)
            {
                LogMessage(-1, "<b>RESULTADO: EMPATE TÉCNICO.</b> Ambos ejércitos han sido aniquilados.");
            }
            else if (playerUnitsCount == 0)
            {
                LogMessage(-1, "<color=purple><b>RESULTADO: VICTORIA DEL VACÍO.</b> La oscuridad consume el campo de batalla.</color>");
            }
            else if (aiUnitsCount == 0)
            {
                LogMessage(-1, "<color=orange><b>RESULTADO: VICTORIA SOLAR.</b> La luz ha purgado la corrupción.</color>");
            }

            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    public void LogMessage(int playerId, string message)
    {
        OnLogMessage?.Invoke(playerId, message);
    }
}