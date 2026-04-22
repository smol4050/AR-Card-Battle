using UnityEngine;
using System.Collections;

public class TutorialAIController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    private readonly int _aiPlayerId = 1;
    private bool _isActive = false;

    public void ActivateAI()
    {
        _isActive = true;
        gameManager.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (!_isActive) return;

        if (phase == RoundPhase.Preparation)
        {
            // Iniciamos la corrutina para darle "tiempo de pensamiento" a la IA
            StartCoroutine(AITurnRoutine());
        }
    }

    private IEnumerator AITurnRoutine()
    {
        // Simulamos 2 segundos de latencia humana
        yield return new WaitForSeconds(2f);

        // Separamos la lógica de decisión en un método síncrono para poder testearlo
        ExecuteDecisionLogic();

        // Esperamos 1 segundo más antes de presionar Ready
        yield return new WaitForSeconds(1f);
        gameManager.SetPlayerReady(_aiPlayerId);
    }

    /// <summary>
    /// Lógica pura de decisión de la IA. Pública para facilitar el Unit Testing.
    /// </summary>
    public void ExecuteDecisionLogic()
    {
        // En el primer turno de preparación (Energía = 1), la IA jugará Drone Swarm en el carril 1
        if (gameManager.currentPhase == RoundPhase.Preparation && gameManager.players[_aiPlayerId].energy >= 1)
        {
            if (gameManager.board[_aiPlayerId, 1] == null) // Si el carril central está vacío
            {
                // Drone Swarm: Coste 1, ATK 1, HP 2
                gameManager.PlayCard(_aiPlayerId, 1, 2, 1, 1, CardID.DroneSwarm);
                //gameManager.OnLogMessage?.Invoke(_aiPlayerId, "Void Dominion (AI) deployed Drone Swarm in Lane 1!");
            }
        }
    }
}