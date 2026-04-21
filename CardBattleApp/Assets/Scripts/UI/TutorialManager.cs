using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public void StartInGameTutorial()
    {
        Debug.Log("Tutorial: The match has started! It is Player 0's turn.");
        Debug.Log("Tutorial: Try placing a card in any lane.");

        // Nos suscribimos al evento del GameManager para detectar cuándo el jugador hace su primera jugada
        gameManager.OnUnitSpawned += CheckFirstPlay;
    }

    private void CheckFirstPlay(int playerId, int lane, Unit unit)
    {
        if (playerId == 0)
        {
            Debug.Log($"Tutorial: Excellent! You placed a unit with {unit.health} HP in Lane {lane}.");
            Debug.Log("Tutorial: Now press 'End Turn' so the combat resolves.");

            // Nos desuscribimos para no repetir el mensaje
            gameManager.OnUnitSpawned -= CheckFirstPlay;
        }
    }
}