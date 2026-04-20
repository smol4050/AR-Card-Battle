using System;
using UnityEngine;

public enum GameState
{
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public PlayerManager[] players = new PlayerManager[2];
    public Unit[,] board = new Unit[2, 3]; // [playerId, laneIndex]

    public GameState currentState { get; private set; }

    // Eventos para la Capa Visual (Frontend)
    public event Action<int, int, Unit> OnUnitSpawned;
    public event Action<int, int> OnUnitDied;
    public event Action<int, int> OnPlayerDamaged;
    public event Action<int, int, int> OnUnitBuffed; // playerId, laneIndex, buffAmount
    public event Action<int> OnGameOver; // Envía el ID del jugador ganador

    public void InitializeGame()
    {
        players[0] = new PlayerManager(0);
        players[1] = new PlayerManager(1);
        currentState = GameState.Playing;
    }

    public bool PlayCard(int playerId, int laneIndex, int cardHealth, int cardAttack, int energyCost)
    {
        if (currentState != GameState.Playing) return false;
        if (board[playerId, laneIndex] != null) return false;
        if (!players[playerId].ConsumeEnergy(energyCost)) return false;

        Unit newUnit = new Unit(playerId, cardHealth, cardAttack, laneIndex);
        board[playerId, laneIndex] = newUnit;

        OnUnitSpawned?.Invoke(playerId, laneIndex, newUnit);
        return true;
    }

    // NUEVO: Método API para activar una habilidad de carta
    public bool ActivateCard(int playerId, int laneIndex, int energyCost, int buffAmount)
    {
        if (currentState != GameState.Playing) return false;

        Unit targetUnit = board[playerId, laneIndex];

        // Validamos que la unidad exista y haya energía suficiente
        if (targetUnit == null || targetUnit.IsDead) return false;
        if (!players[playerId].ConsumeEnergy(energyCost)) return false;

        // Efecto de la habilidad: "Awakening" (Incrementa salud y ataque)
        targetUnit.health += buffAmount;
        targetUnit.attack += buffAmount;

        OnUnitBuffed?.Invoke(playerId, laneIndex, buffAmount);
        return true;
    }

    public void ResolveCombat()
    {
        if (currentState != GameState.Playing) return;

        for (int lane = 0; lane < 3; lane++)
        {
            Unit p0Unit = board[0, lane];
            Unit p1Unit = board[1, lane];

            if (p0Unit != null && p1Unit != null)
            {
                p0Unit.TakeDamage(p1Unit.attack);
                p1Unit.TakeDamage(p0Unit.attack);
                CheckDeath(0, lane, p0Unit);
                CheckDeath(1, lane, p1Unit);
            }
            else if (p0Unit != null && p1Unit == null)
            {
                players[1].TakeDamage(p0Unit.attack);
                OnPlayerDamaged?.Invoke(1, players[1].hp);
            }
            else if (p1Unit != null && p0Unit == null)
            {
                players[0].TakeDamage(p1Unit.attack);
                OnPlayerDamaged?.Invoke(0, players[0].hp);
            }
        }

        CheckWinCondition();
    }

    private void CheckDeath(int playerId, int lane, Unit unit)
    {
        if (unit.IsDead)
        {
            board[playerId, lane] = null;
            OnUnitDied?.Invoke(playerId, lane);
        }
    }

    // NUEVO: Validación del estado del juego
    private void CheckWinCondition()
    {
        bool p0Dead = players[0].hp <= 0;
        bool p1Dead = players[1].hp <= 0;

        if (p0Dead || p1Dead)
        {
            currentState = GameState.GameOver;

            if (p0Dead && p1Dead) OnGameOver?.Invoke(-1); // Empate
            else if (p0Dead) OnGameOver?.Invoke(1);       // Gana P1
            else if (p1Dead) OnGameOver?.Invoke(0);       // Gana P0
        }
    }
}