using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    // Para este prototipo local, definimos los stats de la carta base en inglés
    private readonly int _baseCardHealth = 10;
    private readonly int _baseCardAttack = 5;
    private readonly int _baseCardCost = 1;

    /// <summary>
    /// Se llama cuando el jugador local hace clic en un carril (Lane).
    /// </summary>
    public void OnLaneClicked(int laneIndex)
    {
        // En nuestro entorno local, el jugador humano siempre será el ID 0
        int localPlayerId = 0;

        // Validamos si es nuestro turno usando el estado del GameManager (podríamos consultarlo al TurnManager también)
        if (gameManager.currentState != GameState.Playing)
        {
            Debug.LogWarning("Input: The game is not in a playing state.");
            return;
        }

        // Enviamos la petición pura a nuestra API matemática
        bool success = gameManager.PlayCard(
            playerId: localPlayerId,
            laneIndex: laneIndex,
            cardHealth: _baseCardHealth,
            cardAttack: _baseCardAttack,
            energyCost: _baseCardCost
        );

        if (success)
        {
            Debug.Log($"Input: Card successfully played in lane {laneIndex}!");
        }
        else
        {
            Debug.LogWarning("Input: Failed to play card. Not enough energy or lane occupied.");
        }
    }
}