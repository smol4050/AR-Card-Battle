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

    // Eventos visuales (Sin laneIndex)
    public event Action<int, Unit> OnUnitSpawned;
    public event Action<int, Unit> OnUnitDied;
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
        LogMessage(-1, "Preparation Phase started. Both players can play.");
    }

    public void SetPlayerReady(int playerId)
    {
        if (currentPhase != RoundPhase.Preparation) return;

        isPlayerReady[playerId] = true;
        OnPlayerReadyStatusChanged?.Invoke(playerId, true);
        LogMessage(playerId, $"Player {playerId} is Ready.");

        if (isPlayerReady[0] && isPlayerReady[1])
        {
            ExecuteCombatPhase();
        }
    }

    public bool PlayCard(int playerId, int hp, int atk, int cost, CardID cardId)
    {
        if (currentPhase != RoundPhase.Preparation) return false;
        if (!players[playerId].ConsumeEnergy(cost)) return false;

        Unit newUnit = new Unit(playerId, hp, atk, cardId);
        activeUnits[playerId].Add(newUnit); // Se añade a la lista abierta

        OnUnitSpawned?.Invoke(playerId, newUnit);

        // HABILIDAD ON-PLAY: Quick Deploy (Busca un objetivo dinámico)
        if (cardId == CardID.ScoutUnit)
        {
            int enemyId = 1 - playerId;
            Unit target = GetValidTarget(enemyId);
            if (target != null)
            {
                target.TakeDamage(1);
                CheckDeath(enemyId, target);
                LogMessage(playerId, "Quick Deploy: Dealt 1 damage to an enemy unit.");
            }
        }

        return true;
    }

    // Activar habilidad ahora requiere el índice de la unidad en la lista
    public bool ActivateCard(int playerId, int unitIndex, int cost)
    {
        if (currentPhase != RoundPhase.Preparation) return false;
        if (unitIndex < 0 || unitIndex >= activeUnits[playerId].Count) return false;

        Unit unit = activeUnits[playerId][unitIndex];
        if (unit == null || unit.IsDead) return false;
        if (!players[playerId].ConsumeEnergy(cost)) return false;

        switch (unit.cardId)
        {
            case CardID.BladeMonk:
                unit.tempAttack += 2;
                break;
            case CardID.PulseTank:
                unit.health += 2;
                break;
            case CardID.VoidKnight:
                unit.health -= 1;
                unit.attack += 2;
                CheckDeath(playerId, unit);
                break;
            case CardID.LightCommander:
                foreach (Unit u in activeUnits[playerId]) u.tempAttack += 1;
                break;
            case CardID.DarkCommander:
                int enemyId = 1 - playerId;
                foreach (Unit u in activeUnits[enemyId]) u.attack = Mathf.Max(0, u.attack - 1);
                break;
        }

        LogMessage(playerId, $"Activated ability of {unit.cardId}.");
        return true;
    }

    private void ExecuteCombatPhase()
    {
        currentPhase = RoundPhase.Combat;
        OnPhaseChanged?.Invoke(currentPhase);
        LogMessage(-1, "Combat Phase executing (Auto-Battler Style)...");

        // Diccionario para acumular todo el daño que se recibirá simultáneamente
        Dictionary<Unit, int> incomingDamage = new Dictionary<Unit, int>();

        // Lógica del Jugador 0 apuntando al Jugador 1
        foreach (Unit u0 in activeUnits[0])
        {
            Unit target = GetValidTarget(1);
            int dmg = u0.attack + u0.tempAttack;

            if (target != null)
            {
                if (u0.cardId == CardID.SiegeWalker) dmg += 2; // Overload Shot
                if (!incomingDamage.ContainsKey(target)) incomingDamage[target] = 0;
                incomingDamage[target] += dmg;
            }
            else players[1].TakeDamage(dmg);
        }

        // Lógica del Jugador 1 apuntando al Jugador 0
        foreach (Unit u1 in activeUnits[1])
        {
            Unit target = GetValidTarget(0);
            int dmg = u1.attack + u1.tempAttack;

            if (target != null)
            {
                if (u1.cardId == CardID.SiegeWalker) dmg += 2;
                if (!incomingDamage.ContainsKey(target)) incomingDamage[target] = 0;
                incomingDamage[target] += dmg;
            }
            else players[0].TakeDamage(dmg);
        }

        // Aplicamos el daño masivo a los objetivos
        foreach (var kvp in incomingDamage)
        {
            kvp.Key.TakeDamage(kvp.Value);
        }

        CleanUpDeadUnits(0);
        CleanUpDeadUnits(1);

        CheckWinCondition();
    }

    // Retorna la primera unidad enemiga viva
    private Unit GetValidTarget(int enemyPlayerId)
    {
        foreach (Unit u in activeUnits[enemyPlayerId])
        {
            if (!u.IsDead) return u;
        }
        return null;
    }

    private void CleanUpDeadUnits(int playerId)
    {
        for (int i = activeUnits[playerId].Count - 1; i >= 0; i--)
        {
            Unit u = activeUnits[playerId][i];
            if (u.IsDead)
            {
                activeUnits[playerId].RemoveAt(i);
                OnUnitDied?.Invoke(playerId, u);

                // Habilidad On-Death (Drone Swarm)
                if (u.cardId == CardID.DroneSwarm)
                {
                    int enemyId = 1 - playerId;
                    Unit enemyUnit = GetValidTarget(enemyId);
                    if (enemyUnit != null)
                    {
                        enemyUnit.TakeDamage(1);
                        CheckDeath(enemyId, enemyUnit);
                    }
                    else players[enemyId].TakeDamage(1);
                }
            }
        }
    }

    private void CheckDeath(int playerId, Unit unit)
    {
        if (unit != null && unit.IsDead)
        {
            activeUnits[playerId].Remove(unit);
            OnUnitDied?.Invoke(playerId, unit);

            if (unit.cardId == CardID.DroneSwarm)
            {
                int enemyId = 1 - playerId;
                Unit enemyUnit = GetValidTarget(enemyId);
                if (enemyUnit != null)
                {
                    enemyUnit.TakeDamage(1);
                    CheckDeath(enemyId, enemyUnit);
                }
                else players[enemyId].TakeDamage(1);
            }
        }
    }

    private void CheckWinCondition()
    {
        bool p0Dead = players[0].hp <= 0;
        bool p1Dead = players[1].hp <= 0;

        if (p0Dead || p1Dead)
        {
            currentPhase = RoundPhase.End;
            if (p0Dead && p1Dead) LogMessage(-1, "Game Over: DRAW!");
            else if (p0Dead) LogMessage(-1, "Game Over: Void Dominion WINS!");
            else if (p1Dead) LogMessage(-1, "Game Over: Solar Alliance WINS!");
            return;
        }

        EndRound();
    }

    public void LogMessage(int playerId, string message)
    {
        OnLogMessage?.Invoke(playerId, message);
    }

    private void EndRound()
    {
        currentPhase = RoundPhase.End;
        OnPhaseChanged?.Invoke(currentPhase);

        for (int p = 0; p < 2; p++)
        {
            foreach (Unit u in activeUnits[p])
            {
                u.ResetTurnModifiers();
            }
        }

        StartNewRound();
    }
}