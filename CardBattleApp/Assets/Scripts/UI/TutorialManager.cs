using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TutorialAIController aiController;

    public void StartInGameTutorial()
    {
        Debug.Log("Tutorial: TFT 6v6 INSTANT BATTLE STARTING!");

        // Otorgamos energía suficiente para comprar ejércitos completos
        gameManager.players[0].energy = 10;
        gameManager.players[1].energy = 10;

        // --- JUGADOR 0: SOLLAR ALLIANCE (6 Unidades) ---
        gameManager.PlayCard(0, CardID.SollarForce, new Vector2(-1f, 1.5f), 1);
        gameManager.PlayCard(0, CardID.SollarForce, new Vector2(1f, 1.5f), 1);

        gameManager.PlayCard(0, CardID.SollarDuelist, new Vector2(-1.5f, 0.5f), 1);
        gameManager.PlayCard(0, CardID.SollarDuelist, new Vector2(0f, 0.5f), 1);
        gameManager.PlayCard(0, CardID.SollarDuelist, new Vector2(1.5f, 0.5f), 1);

        gameManager.PlayCard(0, CardID.SollarCommander, new Vector2(0f, -1.0f), 1);

        Debug.Log("Tutorial: Local army deployed. Forcing AI instant deployment...");

        // --- JUGADOR 1: VOID DOMINION (IA) ---
        // Obligamos a la IA a usar su energía y colocar sus unidades ahora mismo sin esperar corrutinas
        aiController.ExecuteDecisionLogic();

        Debug.Log("Tutorial: AI army deployed. Starting combat instantly!");

        // Detonamos la batalla inmediatamente
        gameManager.SetPlayerReady(0);
        gameManager.SetPlayerReady(1);
    }
}