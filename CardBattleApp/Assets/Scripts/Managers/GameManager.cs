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
        LogMessage(-1, "Preparation Phase started. Deploy units.");
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
        LogMessage(playerId, $"Player {playerId} is Ready.");

        if (isPlayerReady[0] && isPlayerReady[1])
        {
            currentPhase = RoundPhase.Combat;
            OnPhaseChanged?.Invoke(currentPhase);
            LogMessage(-1, "Combat Phase executing (Tick-based Simulation)...");
        }
    }

    public bool PlayCard(int playerId, CardID cardId, Vector2 spawnPos, int row, int slotIndex, int cost = 1)
    {
        if (currentPhase != RoundPhase.Preparation) return false;

        // NUEVO: Validación de Casilla Única. Rechaza la compra si ya hay una unidad viva allí.
        if (activeUnits[playerId].Exists(u => u.slotIndex == slotIndex && !u.IsDead))
        {
            LogMessage(playerId, $"Slot {slotIndex} is already occupied!");
            return false;
        }

        if (!players[playerId].ConsumeEnergy(cost)) return false;

        int spawnCount = (cardId == CardID.VoidHorde) ? 4 : 1;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 finalPos = spawnPos + new Vector2(i * 0.3f, 0);
            Unit newUnit = null;

            switch (cardId)
            {
                case CardID.SollarDuelist:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 180f, 38f, 40f, 0.95f, finalPos);
                    newUnit.skillCooldown = 6f;
                    break;
                case CardID.SollarForce:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 140f, 20f, 35f, 0.85f, finalPos);
                    newUnit.skillCooldown = 5f;
                    break;
                case CardID.SollarCommander:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 160f, 28f, 35f, 0.85f, finalPos);
                    newUnit.skillCooldown = 8f;
                    break;
                case CardID.VoidHorde:
                    // La Horda inyectará el MISMO slotIndex a sus 4 individuos, reclamando la casilla para el escuadrón completo
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 50f, 15f, 10f, 1.25f, finalPos);
                    break;
                case CardID.VoidCommander:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 180f, 30f, 30f, 0.9f, finalPos);
                    newUnit.skillCooldown = 7f;
                    break;
                case CardID.VoidHeavyShooter:
                    newUnit = new Unit(playerId, cardId, row, slotIndex, 120f, 60f, 20f, 1.0f, finalPos);
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
        CleanUpDeadUnits(0);
        CleanUpDeadUnits(1);
        CheckWinCondition();
    }

    private void UpdateAurasAndPassives()
    {
        // 1. Limpieza base
        foreach (var list in activeUnits)
        {
            foreach (var u in list)
            {
                u.bonusSpeed = 0;
                u.damageMultiplier = 1f;
            }
        }

        // 2. Fuego Coordinado (Void Horde Multiplier)
        int hordeCount = activeUnits[1].FindAll(u => u.cardId == CardID.VoidHorde).Count;
        float hordeMultiplier = 1f;
        if (hordeCount == 2) hordeMultiplier = 1.20f;
        else if (hordeCount == 3) hordeMultiplier = 1.35f;
        else if (hordeCount >= 4) hordeMultiplier = 1.50f;

        foreach (Unit u in activeUnits[1])
        {
            if (u.cardId == CardID.VoidHorde)
            {
                u.damageMultiplier *= hordeMultiplier;
            }
            if (u.cardId == CardID.VoidCommander)
            {
                // BUFF: Ahora otorga 8% (0.08f) por unidad en lugar de 5% (0.05f)
                u.bonusSpeed += (activeUnits[1].Count * 0.08f);
            }
        }
    }

    private void ProcessTeamTicks(int attackerTeamId, int defenderTeamId, float deltaTime)
    {
        foreach (Unit attacker in activeUnits[attackerTeamId])
        {
            if (attacker.stunTimer > 0)
            {
                attacker.stunTimer -= deltaTime;
                continue;
            }

            if (attacker.cardId == CardID.SollarForce)
            {
                attacker.passiveTimer += deltaTime;
                if (attacker.passiveTimer >= 4f)
                {
                    Unit closest = GetClosestTarget(attacker, defenderTeamId);
                    if (closest != null) closest.stunTimer = 0.5f;
                    attacker.passiveTimer = 0f;
                }
            }

            if (attacker.skillCooldown > 0)
            {
                attacker.currentSkillTimer += deltaTime;
                if (attacker.ownerId == 0 && activeUnits[0].Exists(u => u.cardId == CardID.SollarCommander))
                {
                    attacker.currentSkillTimer += (deltaTime * 0.10f);
                }

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
        realDamage *= target.damageTakenMultiplier;

        target.TakeDamage(realDamage);
        OnUnitAttacked?.Invoke(attacker, target, realDamage);
    }

    private void ExecuteSkill(Unit caster, int enemyTeamId)
    {
        OnUnitSkillCast?.Invoke(caster, caster.cardId);

        switch (caster.cardId)
        {
            case CardID.SollarDuelist:
                foreach (Unit enemy in activeUnits[enemyTeamId])
                {
                    if (Vector2.Distance(caster.logicalPosition, enemy.logicalPosition) <= 2.5f)
                        enemy.TakeDamage(30f);
                }
                break;
            case CardID.SollarForce:
                Unit targetForce = GetClosestTarget(caster, enemyTeamId);
                if (targetForce != null)
                {
                    targetForce.TakeDamage(30f);
                    targetForce.stunTimer = 0.5f;
                    targetForce.logicalPosition = new Vector2(targetForce.logicalPosition.x + 1.5f, targetForce.logicalPosition.y);
                }
                break;
            case CardID.SollarCommander:
                foreach (Unit ally in activeUnits[caster.ownerId])
                {
                    ally.damageMultiplier *= 1.20f;
                }
                break;
            case CardID.VoidHeavyShooter:
                Unit targetShooter = GetClosestTarget(caster, enemyTeamId);
                if (targetShooter != null) targetShooter.TakeDamage(75f);
                break;
            case CardID.VoidCommander:
                Unit targetCom = GetClosestTarget(caster, enemyTeamId);
                if (targetCom != null) targetCom.damageTakenMultiplier = 1.25f;
                break;
        }
    }

    private Unit GetClosestTarget(Unit attacker, int enemyTeamId)
    {
        Unit bestTarget = null;
        float closestDist = float.MaxValue;

        // 1. Verificar si la Primera Línea (row 0) del enemigo sigue viva
        bool isFrontlineAlive = activeUnits[enemyTeamId].Exists(u => !u.IsDead && u.row == 0);

        // 2. Establecer la línea objetivo: Si hay frente, se ataca el frente. Si no, se avanza a la retaguardia.
        int targetRow = isFrontlineAlive ? 0 : 1;

        // 3. Buscar al enemigo más cercano DENTRO de la línea objetivo
        foreach (Unit enemy in activeUnits[enemyTeamId])
        {
            if (enemy.IsDead) continue;

            // FILTRO DE FOCUS: Ignoramos al enemigo si no pertenece a la línea que debemos atacar
            if (enemy.row != targetRow) continue;

            float dist = Vector2.Distance(attacker.logicalPosition, enemy.logicalPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = enemy;
            }
        }
        return bestTarget;
    }

    private void CleanUpDeadUnits(int playerId)
    {
        for (int i = activeUnits[playerId].Count - 1; i >= 0; i--)
        {
            if (activeUnits[playerId][i].IsDead)
            {
                OnUnitDied?.Invoke(playerId, activeUnits[playerId][i]);
                activeUnits[playerId].RemoveAt(i);
            }
        }
    }

    private void CheckWinCondition()
    {
        bool p0Dead = players[0].hp <= 0 || activeUnits[0].Count == 0;
        bool p1Dead = players[1].hp <= 0 || activeUnits[1].Count == 0;

        if (p0Dead || p1Dead)
        {
            currentPhase = RoundPhase.End;
            if (p0Dead && p1Dead) LogMessage(-1, "Game Over: DRAW!");
            else if (p0Dead) LogMessage(-1, "Game Over: Void Dominion WINS!");
            else if (p1Dead) LogMessage(-1, "Game Over: Solar Alliance WINS!");
        }
    }

    public void LogMessage(int playerId, string message)
    {
        OnLogMessage?.Invoke(playerId, message);
    }
}