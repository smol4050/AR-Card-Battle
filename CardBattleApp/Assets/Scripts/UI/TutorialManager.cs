using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public void StartInGameTutorial()
    {
        Debug.Log("Tutorial: TFT Mini-Battle Starting!");

        // Damos energía máxima temporalmente para el test
        gameManager.players[0].energy = 5;
        gameManager.players[1].energy = 5;

        // Jugador 0 invoca múltiples unidades pequeñas
        gameManager.PlayCard(0, hp: 1, atk: 2, cost: 1, cardId: CardID.ScoutUnit);
        gameManager.PlayCard(0, hp: 1, atk: 2, cost: 1, cardId: CardID.ScoutUnit);

        // Jugador 1 (IA) invoca un tanque pesado
        gameManager.PlayCard(1, hp: 5, atk: 1, cost: 3, cardId: CardID.PulseTank);

        Debug.Log("Tutorial: Units deployed. Forcing Ready state to execute auto-combat...");

        // Detonamos la fase forzando la respuesta de los jugadores
        gameManager.SetPlayerReady(0);
        gameManager.SetPlayerReady(1);

        Debug.Log("Tutorial: Mini-Battle executed automatically. Check console logs!");
    }
}