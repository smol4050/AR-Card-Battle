using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TutorialAIController aiController;

    private int _tutorialStep = 0;

    public void StartInGameTutorial()
    {
        Debug.Log("Tutorial: The match has started! In this game, phases are SIMULTANEOUS.");
        Debug.Log("Tutorial: Both players plan their moves at the same time. Place your Scout Unit in any lane!");

        gameManager.OnUnitSpawned += CheckPlayerPlay;
        gameManager.OnPlayerReadyStatusChanged += CheckReadyStatus;

        aiController.ActivateAI();
    }

    private void CheckPlayerPlay(int playerId, int lane, Unit unit)
    {
        if (playerId == 0 && _tutorialStep == 0)
        {
            Debug.Log($"Tutorial: Excellent. You deployed {unit.cardId}.");
            Debug.Log("Tutorial: Notice that the AI is also planning its move. Once you are done, press 'Ready'!");
            _tutorialStep = 1;
            gameManager.OnUnitSpawned -= CheckPlayerPlay;
        }
    }

    private void CheckReadyStatus(int playerId, bool isReady)
    {
        if (playerId == 0 && isReady && _tutorialStep == 1)
        {
            Debug.Log("Tutorial: You are Ready. Waiting for the opponent...");
            _tutorialStep = 2; // Avanzamos el paso directamente aquí
        }

        // Ahora es un 'if' separado. Evalúa el estado global sin importar quién lo detonó.
        if (_tutorialStep == 2 && gameManager.isPlayerReady[0] && gameManager.isPlayerReady[1])
        {
            Debug.Log("Tutorial: Both are ready! Watch the combat unfold automatically.");
            _tutorialStep = 3;
            gameManager.OnPlayerReadyStatusChanged -= CheckReadyStatus;
        }
    }
}