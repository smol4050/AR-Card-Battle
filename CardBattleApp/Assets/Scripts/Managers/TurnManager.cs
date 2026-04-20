using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private GameManager _gameManager;
    public int currentTurnPlayerId = 0;
    public float turnTimer = 30f;
    private bool _isTimerRunning = false;

    public event Action<int> OnTurnChanged;
    public event Action<float> OnTimerUpdated;

    public void Initialize(GameManager gm)
    {
        _gameManager = gm;
        StartTurn(0);
    }

    private void Update()
    {
        if (!_isTimerRunning) return;

        turnTimer -= Time.deltaTime;
        OnTimerUpdated?.Invoke(turnTimer);

        if (turnTimer <= 0)
        {
            EndTurn();
        }
    }

    // Método API: Alguien (input local o red) pide terminar el turno
    public void EndTurn()
    {
        _isTimerRunning = false;

        // El turno termina, hay combate
        _gameManager.ResolveCombat();

        int nextPlayer = (currentTurnPlayerId == 0) ? 1 : 0;
        StartTurn(nextPlayer);
    }

    private void StartTurn(int playerId)
    {
        currentTurnPlayerId = playerId;
        turnTimer = 30f;
        _isTimerRunning = true;

        _gameManager.players[playerId].StartTurn();
        OnTurnChanged?.Invoke(playerId);
    }
}