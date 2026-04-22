using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private GameManager _gameManager;
    public float roundTimer = 30f;
    private bool _isTimerRunning = false;

    public event Action<float> OnTimerUpdated;

    public void Initialize(GameManager gm)
    {
        _gameManager = gm;

        // Nos suscribimos al evento para saber cuándo inicia una nueva ronda
        _gameManager.OnPhaseChanged += HandlePhaseChanged;

        // Si al inicializar ya estamos en preparación, arrancamos el reloj
        if (_gameManager.currentPhase == RoundPhase.Preparation)
        {
            StartTimer();
        }
    }

    private void OnDestroy()
    {
        if (_gameManager != null)
        {
            _gameManager.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(RoundPhase newPhase)
    {
        if (newPhase == RoundPhase.Preparation)
        {
            StartTimer();
        }
        else
        {
            // Detenemos el reloj durante el combate y el final de ronda
            _isTimerRunning = false;
        }
    }

    private void StartTimer()
    {
        roundTimer = 30f;
        _isTimerRunning = true;
    }

    private void Update()
    {
        if (!_isTimerRunning) return;

        roundTimer -= Time.deltaTime;
        OnTimerUpdated?.Invoke(roundTimer);

        if (roundTimer <= 0)
        {
            ForceEndPreparationPhase();
        }
    }

    private void ForceEndPreparationPhase()
    {
        _isTimerRunning = false;

        // Si el tiempo se acaba, forzamos el estado "Ready" en los jugadores rezagados.
        // Como el GameManager evalúa si ambos están listos para iniciar el combate,
        // esto disparará el ExecuteCombatPhase() de forma automática y determinista.
        if (!_gameManager.isPlayerReady[0]) _gameManager.SetPlayerReady(0);
        if (!_gameManager.isPlayerReady[1]) _gameManager.SetPlayerReady(1);
    }
}