using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    // Para este prototipo local, definimos los stats de una carta base (Ej: Scout Unit)
    private readonly int _baseCardHealth = 1;
    private readonly int _baseCardAttack = 2;
    private readonly int _baseCardCost = 1;
    private readonly CardID _baseCardId = CardID.ScoutUnit;

    /// <summary>
    /// Se llama cuando el jugador local hace clic en un carril (Lane).
    /// </summary>
    public void OnLaneClicked(int laneIndex)
    {
        int localPlayerId = 0;

        // Validamos si estamos en la fase correcta (Preparation)
        if (gameManager.currentPhase != RoundPhase.Preparation)
        {
            Debug.LogWarning("Input: The game is not in the Preparation phase.");
            return;
        }

        // Enviamos la petición pura a nuestra API matemática usando la nueva firma
        bool success = gameManager.PlayCard(
            playerId: localPlayerId,
            laneIndex: laneIndex,
            hp: _baseCardHealth,
            atk: _baseCardAttack,
            cost: _baseCardCost,
            cardId: _baseCardId
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

    /// <summary>
    /// Se llama cuando el jugador presiona el botón "Ready".
    /// </summary>
    public void OnReadyClicked()
    {
        int localPlayerId = 0;
        gameManager.SetPlayerReady(localPlayerId);
    }
}