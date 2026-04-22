using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public PlayerManager[] players = new PlayerManager[2];
    public Unit[,] board = new Unit[2, 3];

    public RoundPhase currentPhase { get; private set; }
    public bool[] isPlayerReady = new bool[2];

    // Eventos visuales
    public event Action<int, int, Unit> OnUnitSpawned;
    public event Action<int, int> OnUnitDied;
    public event Action<RoundPhase> OnPhaseChanged;
    public event Action<int, bool> OnPlayerReadyStatusChanged;
    public event Action<int, string> OnLogMessage;

    public void InitializeGame()
    {
        players[0] = new PlayerManager(0);
        players[1] = new PlayerManager(1);
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
        OnLogMessage?.Invoke(-1, "Preparation Phase started. Both players can play.");
    }

    // Nuevo: Sistema Ready Simultáneo
    public void SetPlayerReady(int playerId)
    {
        if (currentPhase != RoundPhase.Preparation) return;

        isPlayerReady[playerId] = true;
        OnPlayerReadyStatusChanged?.Invoke(playerId, true);
        OnLogMessage?.Invoke(playerId, $"Player {playerId} is Ready.");

        // Si ambos están listos, se bloquea el input y pasa a combate
        if (isPlayerReady[0] && isPlayerReady[1])
        {
            ExecuteCombatPhase();
        }
    }
    public bool PlayCard(int playerId, int laneIndex, int hp, int atk, int cost, CardID cardId)
    {
        if (currentPhase != RoundPhase.Preparation) return false;
        if (board[playerId, laneIndex] != null) return false;
        if (!players[playerId].ConsumeEnergy(cost)) return false;

        Unit newUnit = new Unit(playerId, hp, atk, laneIndex, cardId);
        board[playerId, laneIndex] = newUnit;
        OnUnitSpawned?.Invoke(playerId, laneIndex, newUnit);

        // HABILIDAD ON-PLAY: Quick Deploy (Scout Unit)
        if (cardId == CardID.ScoutUnit)
        {
            int enemyId = 1 - playerId;
            if (board[enemyId, laneIndex] != null)
            {
                board[enemyId, laneIndex].TakeDamage(1);
                CheckDeath(enemyId, laneIndex, board[enemyId, laneIndex]);
                OnLogMessage?.Invoke(playerId, "Quick Deploy: Dealt 1 damage to enemy unit.");
            }
        }

        return true;
    }

    public bool ActivateCard(int playerId, int laneIndex, int cost)
    {
        if (currentPhase != RoundPhase.Preparation) return false;

        Unit unit = board[playerId, laneIndex];
        if (unit == null || unit.IsDead) return false;
        if (!players[playerId].ConsumeEnergy(cost)) return false;

        // HABILIDADES ON-ACTIVATE
        switch (unit.cardId)
        {
            case CardID.BladeMonk: // Focus
                unit.tempAttack += 2;
                break;
            case CardID.PulseTank: // Shield Pulse
                unit.health += 2;
                break;
            case CardID.VoidKnight: // Rage
                unit.health -= 1;
                unit.attack += 2;
                CheckDeath(playerId, laneIndex, unit);
                break;
            case CardID.LightCommander: // Inspire
                for (int i = 0; i < 3; i++)
                    if (board[playerId, i] != null) board[playerId, i].tempAttack += 1;
                break;
            case CardID.DarkCommander: // Corrupt
                int enemyId = 1 - playerId;
                for (int i = 0; i < 3; i++)
                    if (board[enemyId, i] != null) board[enemyId, i].attack = Mathf.Max(0, board[enemyId, i].attack - 1);
                break;
        }

        OnLogMessage?.Invoke(playerId, $"Activated ability of {unit.cardId}.");
        return true;
    }

    public bool PlaySpell(int playerId, int targetLane, int cost, SpellID spellId, bool targetingEnemy)
    {
        if (currentPhase != RoundPhase.Preparation) return false;
        if (!players[playerId].ConsumeEnergy(cost)) return false;

        int targetPlayer = targetingEnemy ? 1 - playerId : playerId;
        Unit targetUnit = board[targetPlayer, targetLane];

        if (targetUnit != null)
        {
            if (spellId == SpellID.EnergyBurst) targetUnit.TakeDamage(3);
            else if (spellId == SpellID.ShieldField) targetUnit.health += 2;

            CheckDeath(targetPlayer, targetLane, targetUnit);
            return true;
        }
        return false;
    }


    private void ExecuteCombatPhase()
    {
        currentPhase = RoundPhase.Combat;
        OnPhaseChanged?.Invoke(currentPhase);
        OnLogMessage?.Invoke(-1, "Combat Phase executing...");

        for (int lane = 0; lane < 3; lane++)
        {
            Unit u0 = board[0, lane];
            Unit u1 = board[1, lane];

            int dmg0 = u0 != null ? u0.attack + u0.tempAttack : 0;
            int dmg1 = u1 != null ? u1.attack + u1.tempAttack : 0;

            if (u0 != null && u0.cardId == CardID.SiegeWalker && u1 == null) dmg0 += 2;
            if (u1 != null && u1.cardId == CardID.SiegeWalker && u0 == null) dmg1 += 2;

            if (u0 != null && u1 != null)
            {
                u0.TakeDamage(dmg1);
                u1.TakeDamage(dmg0);
            }
            else if (u0 != null && u1 == null) players[1].TakeDamage(dmg0);
            else if (u1 != null && u0 == null) players[0].TakeDamage(dmg1);

            CheckDeath(0, lane, u0);
            CheckDeath(1, lane, u1);
        }

        CheckWinCondition(); // NUEVO: Evaluamos si alguien murió antes de reiniciar la ronda
    }

    private void CheckDeath(int playerId, int lane, Unit unit)
    {
        if (unit != null && unit.IsDead)
        {
            // NUEVO: Limpiamos el tablero PRIMERO para evitar un ciclo infinito si dos Drones se matan a la vez
            board[playerId, lane] = null;
            OnUnitDied?.Invoke(playerId, lane);

            if (unit.cardId == CardID.DroneSwarm)
            {
                int enemyId = 1 - playerId;
                Unit enemyUnit = board[enemyId, lane];

                if (enemyUnit != null)
                {
                    enemyUnit.TakeDamage(1);
                    // NUEVO: Llamado recursivo. Si el daño del Drone Swarm mata a la unidad enemiga, la destruye.
                    CheckDeath(enemyId, lane, enemyUnit);
                }
                else
                {
                    players[enemyId].TakeDamage(1);
                }
            }
        }
    }

    // NUEVO: Método recuperado de la versión anterior
    private void CheckWinCondition()
    {
        bool p0Dead = players[0].hp <= 0;
        bool p1Dead = players[1].hp <= 0;

        if (p0Dead || p1Dead)
        {
            currentPhase = RoundPhase.End;
            if (p0Dead && p1Dead) OnLogMessage?.Invoke(-1, "Game Over: DRAW!");
            else if (p0Dead) OnLogMessage?.Invoke(-1, "Game Over: Void Dominion WINS!");
            else if (p1Dead) OnLogMessage?.Invoke(-1, "Game Over: Solar Alliance WINS!");
            return; // Detenemos el flujo, ya no se llama a EndRound()
        }

        //EndRound(); // Si nadie murió, la ronda termina y se reinicia
    }
}