using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public PlayerManager[] players = new PlayerManager[2];

    // MODELO TFT: Listas dinámicas en lugar de matriz
    public List<Unit>[] activeUnits = new List<Unit>[2];

    public RoundPhase currentPhase { get; private set; }
    public bool[] isPlayerReady = new bool[2];

    // Eventos visuales adaptados a parámetros flotantes
    public event Action<int, Unit> OnUnitSpawned;
    public event Action<int, Unit> OnUnitDied;
    public event Action<Unit, Unit, float> OnUnitAttacked; // Atacante, Defensor, DañoReal
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

    // Instanciador con Stats Base de la Tabla
    public bool PlayCard(int playerId, CardID cardId, Vector2 spawnPos, int cost = 1)
    {
        if (currentPhase != RoundPhase.Preparation) return false;
        if (!players[playerId].ConsumeEnergy(cost)) return false;

        int spawnCount = (cardId == CardID.VoidHorde) ? 4 : 1;

        for (int i = 0; i < spawnCount; i++)
        {
            // Espaciamos ligeramente la horda para evitar superposición exacta
            Vector2 finalPos = spawnPos + new Vector2(i * 0.3f, 0);
            Unit newUnit = null;

            switch (cardId)
            {
                case CardID.SollarDuelist:
                    newUnit = new Unit(playerId, cardId, 180f, 38f, 40f, 0.95f, finalPos);
                    newUnit.skillCooldown = 6f;
                    break;
                case CardID.VoidHorde:
                    newUnit = new Unit(playerId, cardId, 55f, 15f, 10f, 1.25f, finalPos);
                    break;
                case CardID.VoidCommander:
                    newUnit = new Unit(playerId, cardId, 150f, 30f, 30f, 0.9f, finalPos);
                    newUnit.skillCooldown = 7f;
                    break;
                case CardID.SollarForce:
                    newUnit = new Unit(playerId, cardId, 140f, 20f, 35f, 0.85f, finalPos);
                    newUnit.skillCooldown = 5f;
                    break;
                case CardID.VoidHeavyShooter:
                    newUnit = new Unit(playerId, cardId, 120f, 60f, 20f, 0.8f, finalPos);
                    newUnit.skillCooldown = 6f;
                    break;
                case CardID.SollarCommander:
                    newUnit = new Unit(playerId, cardId, 160f, 28f, 35f, 0.85f, finalPos);
                    newUnit.skillCooldown = 8f;
                    break;
            }

            if (newUnit != null)
            {
                activeUnits[playerId].Add(newUnit);
                OnUnitSpawned?.Invoke(playerId, newUnit);
            }
        }

        LogMessage(playerId, $"Deployed {cardId}.");
        return true;
    }

    // --- MOTOR EN TIEMPO REAL ---
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
        // 1. Limpiar modificadores volátiles en cada tick
        foreach (var list in activeUnits)
        {
            foreach (var u in list)
            {
                u.bonusSpeed = 0;
                u.damageMultiplier = 1f;
                u.damageTakenMultiplier = 1f; // Se modificaría por Marca de Aniquilación
            }
        }

        // 2. Pasiva: Fuego Coordinado (Void Horde)
        int hordeCount = activeUnits[1].FindAll(u => u.cardId == CardID.VoidHorde).Count;
        float hordeMultiplier = 1f;
        if (hordeCount == 2) hordeMultiplier = 1.20f;
        else if (hordeCount == 3) hordeMultiplier = 1.35f;
        else if (hordeCount >= 4) hordeMultiplier = 1.50f;

        // 3. Evaluar pasivas globales por unidad
        foreach (Unit u in activeUnits[1])
        {
            if (u.cardId == CardID.VoidHorde)
            {
                u.damageMultiplier *= hordeMultiplier;
            }
            if (u.cardId == CardID.VoidCommander)
            {
                // +5% velocidad de ataque por cada unidad Void viva
                u.bonusSpeed += (activeUnits[1].Count * 0.05f);
            }
        }
    }

    private void ProcessTeamTicks(int attackerTeamId, int defenderTeamId, float deltaTime)
    {
        foreach (Unit attacker in activeUnits[attackerTeamId])
        {
            // Procesar Stun
            if (attacker.stunTimer > 0)
            {
                attacker.stunTimer -= deltaTime;
                continue;
            }

            // Procesar Pasiva de Sollar Force (Stun pasivo cada 4s)
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

            // Procesar Habilidades (Cooldowns)
            if (attacker.skillCooldown > 0)
            {
                attacker.currentSkillTimer += deltaTime;
                // Pasiva Sollar Commander reduce cooldowns
                if (attacker.ownerId == 0 && activeUnits[0].Exists(u => u.cardId == CardID.SollarCommander))
                {
                    attacker.currentSkillTimer += (deltaTime * 0.10f); // 10% más rápido
                }

                if (attacker.currentSkillTimer >= attacker.skillCooldown)
                {
                    ExecuteSkill(attacker, defenderTeamId);
                    attacker.currentSkillTimer = 0f;
                }
            }

            // Procesar Ataque Básico
            attacker.attackProgress += attacker.FinalSpeed * deltaTime;
            if (attacker.attackProgress >= 1f)
            {
                Unit target = GetClosestTarget(attacker, defenderTeamId);
                if (target != null)
                {
                    ExecuteBasicAttack(attacker, target);
                }
                else
                {
                    // SOLUCIÓN CS1503: Convertimos el ataque flotante al entero más cercano antes de golpear al jugador
                    int damageToPlayer = Mathf.RoundToInt(attacker.FinalAtk);
                    players[defenderTeamId].TakeDamage(damageToPlayer);
                }

                attacker.attackProgress -= 1f; // Reiniciamos restando 1 entero
            }
        }
    }

    private void ExecuteBasicAttack(Unit attacker, Unit target)
    {
        // Aplicación estricta de la fórmula de daño mitigado
        float mitigationFactor = 100f / (100f + target.FinalDef);
        float realDamage = attacker.FinalAtk * mitigationFactor;

        // Aplicar multiplicadores (Buffs/Debuffs)
        realDamage *= attacker.damageMultiplier;
        realDamage *= target.damageTakenMultiplier;

        target.TakeDamage(realDamage);
        OnUnitAttacked?.Invoke(attacker, target, realDamage);
    }

    private void ExecuteSkill(Unit caster, int enemyTeamId)
    {
        switch (caster.cardId)
        {
            case CardID.SollarDuelist: // Corte Radiante
                foreach (Unit enemy in activeUnits[enemyTeamId])
                {
                    if (Vector2.Distance(caster.logicalPosition, enemy.logicalPosition) <= 2.5f)
                    {
                        enemy.TakeDamage(30f);
                        OnLogMessage?.Invoke(caster.ownerId, "Corte Radiante deal 30 AoE damage!");
                    }
                }
                break;

            case CardID.VoidHeavyShooter: // Modo Ráfaga (3 disparos de 25 dmg puro)
                Unit target = GetClosestTarget(caster, enemyTeamId);
                if (target != null)
                {
                    target.TakeDamage(75f); // 3 * 25
                    OnLogMessage?.Invoke(caster.ownerId, "Modo Ráfaga hit for 75 total damage!");
                }
                break;
        }
    }

    private Unit GetClosestTarget(Unit attacker, int enemyTeamId)
    {
        Unit bestTarget = null;
        float closestDist = float.MaxValue;

        foreach (Unit enemy in activeUnits[enemyTeamId])
        {
            if (enemy.IsDead) continue;
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
            return;
        }
    }

    public void LogMessage(int playerId, string message)
    {
        OnLogMessage?.Invoke(playerId, message);
    }
}