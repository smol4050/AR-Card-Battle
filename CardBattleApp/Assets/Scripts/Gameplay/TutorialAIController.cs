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
        // En el modelo TFT, la IA evalúa su energía y despliega su unidad en su área dinámica (Ej: Y positivo)
        if (gameManager.currentPhase == RoundPhase.Preparation && gameManager.players[_aiPlayerId].energy >= 1)
        {
            // Despliega la Horda (Instanciará 4 unidades automáticamente gracias al GameManager)
            gameManager.PlayCard(_aiPlayerId, CardID.VoidHorde, new Vector2(0, 2f), 1);
            gameManager.LogMessage(_aiPlayerId, "Void Dominion (AI) deployed Void Horde!");
        }
    }
}