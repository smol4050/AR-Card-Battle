using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public void StartInGameTutorial()
    {
        Debug.Log("Tutorial: TFT Mini-Battle Starting!");

        // Damos energía máxima temporalmente
        gameManager.players[0].energy = 10;
        gameManager.players[1].energy = 10;

        // Jugador 0 invoca unidades Sollar
        gameManager.PlayCard(0, CardID.SollarDuelist, new Vector2(-1f, -1f), 1);
        gameManager.PlayCard(0, CardID.SollarForce, new Vector2(1f, -1f), 1);

        // Jugador 1 (IA) invoca un Comandante del Vacío
        gameManager.PlayCard(1, CardID.VoidCommander, new Vector2(0f, 1f), 1);

        Debug.Log("Tutorial: Units deployed. Forcing Ready state to execute auto-combat...");

        gameManager.SetPlayerReady(0);
        gameManager.SetPlayerReady(1);

        Debug.Log("Tutorial: Simulation Started. You need to call ProcessCombatTick from an Update loop to see the fight.");
    }
}