using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private readonly int _baseCardCost = 1;
    private readonly CardID _baseCardId = CardID.SollarDuelist;

    [Header("Target Placement")]
    public Transform targetSlot;
    public int targetRow = 1;
    public int targetSlotIndex = 4;

    public void OnDeployCardClicked()
    {
        int localPlayerId = 0;
        int currentEnergy = gameManager.players[localPlayerId].energy;

        if (!GameplayRules.CanPlayCard(gameManager.currentPhase, currentEnergy, _baseCardCost))
        {
            Debug.LogWarning("Input: Cannot play card. Wrong phase or not enough energy.");
            return;
        }

        Vector2 spawnPos = new Vector2(0, -2f);

        if (targetSlot != null)
        {
            // Extraemos la posición exacta considerando el BoxCollider si existe
            BoxCollider col = targetSlot.GetComponent<BoxCollider>();
            if (col != null)
            {
                spawnPos = new Vector2(targetSlot.localPosition.x + col.center.x, targetSlot.localPosition.z + col.center.z);
            }
            else
            {
                spawnPos = new Vector2(targetSlot.localPosition.x, targetSlot.localPosition.z);
            }
        }

        bool success = gameManager.PlayCard(localPlayerId, _baseCardId, spawnPos, targetRow, targetSlotIndex, _baseCardCost);

        if (success) Debug.Log($"Input: {_baseCardId} deployed to slot {targetSlotIndex}!");
    }

    public void OnReadyClicked()
    {
        gameManager.SetPlayerReady(0);
    }
}