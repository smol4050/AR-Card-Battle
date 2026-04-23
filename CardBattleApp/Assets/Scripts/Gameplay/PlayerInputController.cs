using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    // Para este prototipo local, definimos el coste y el ID de la carta que lanzará el botón
    private readonly int _baseCardCost = 1;
    private readonly CardID _baseCardId = CardID.SollarDuelist;

    /// <summary>
    /// Se llama cuando el jugador local presiona el botón de desplegar carta.
    /// </summary>
    public void OnDeployCardClicked()
    {
        int localPlayerId = 0;
        int currentEnergy = gameManager.players[localPlayerId].energy;

        // Utilizamos nuestra regla estática aislada para validar la jugada
        if (!GameplayRules.CanPlayCard(gameManager.currentPhase, currentEnergy, _baseCardCost))
        {
            Debug.LogWarning("Input: Cannot play card. Wrong phase or not enough energy.");
            return;
        }

        // Enviamos la petición usando la nueva firma (playerId, cardId, spawnPos, cost)
        // Usamos una posición lógica (Ej: Y negativo para el lado del jugador)
        bool success = gameManager.PlayCard(
            localPlayerId,
            _baseCardId,
            new Vector2(0, -2f),
            _baseCardCost
        );

        if (success)
        {
            Debug.Log($"Input: {_baseCardId} successfully deployed to the TFT board!");
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