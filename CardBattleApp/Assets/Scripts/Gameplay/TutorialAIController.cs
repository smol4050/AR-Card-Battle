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
            StartCoroutine(AITurnRoutine());
        }
    }

    private IEnumerator AITurnRoutine()
    {
        yield return new WaitForSeconds(2f);
        ExecuteDecisionLogic();
        yield return new WaitForSeconds(1f);
        gameManager.SetPlayerReady(_aiPlayerId);
    }

    public void ExecuteDecisionLogic()
    {
        // En el modelo TFT, la IA despliega su unidad en su área dinámica
        if (gameManager.currentPhase == RoundPhase.Preparation && gameManager.players[_aiPlayerId].energy >= 1)
        {
            // Despliega Drone Swarm: Coste 1, ATK 1, HP 2
            gameManager.PlayCard(_aiPlayerId, hp: 2, atk: 1, cost: 1, cardId: CardID.DroneSwarm);
            gameManager.LogMessage(_aiPlayerId, "Void Dominion (AI) deployed Drone Swarm!");
        }
    }
}