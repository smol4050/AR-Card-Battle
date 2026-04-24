using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TutorialAIController aiController;

    [Header("Zonas de Despliegue del Jugador (Arrastra los GameObjects)")]
    [Tooltip("Casillas de la Primera Línea (Frente)")]
    public Transform[] frontSlots = new Transform[3];

    [Tooltip("Casillas de la Retaguardia (Atrás)")]
    public Transform[] backSlots = new Transform[3];

    public void StartInGameTutorial()
    {
        Debug.Log("Tutorial: STRATEGIC FOCUS BATTLE STARTING!");

        gameManager.players[0].energy = 10;
        gameManager.players[1].energy = 10;

        // Validamos que hayas asignado las casillas en el Inspector
        if (frontSlots.Length < 3 || backSlots.Length < 3)
        {
            Debug.LogError("Faltan casillas por asignar en el TutorialManager.");
            return;
        }

        // --- JUGADOR 0: SOLLAR ALLIANCE ---

        // Fila 0 (Frontline) - Slots 0, 1, 2
        gameManager.PlayCard(0, CardID.SollarDuelist, new Vector2(backSlots[2].localPosition.x, backSlots[2].localPosition.z), row: 0, slotIndex: 2, cost: 1);
        gameManager.PlayCard(0, CardID.SollarCommander, new Vector2(backSlots[1].localPosition.x, backSlots[1].localPosition.z), row: 0, slotIndex: 1, cost: 1);
        gameManager.PlayCard(0, CardID.SollarDuelist, new Vector2(backSlots[0].localPosition.x, backSlots[0].localPosition.z), row: 0, slotIndex: 0, cost: 1);

        // Fila 1 (Backline) - Slots 3, 4, 5
        gameManager.PlayCard(0, CardID.SollarForce, new Vector2(frontSlots[2].localPosition.x, frontSlots[2].localPosition.z), row: 1, slotIndex: 5, cost: 1);
        gameManager.PlayCard(0, CardID.SollarForce, new Vector2(frontSlots[0].localPosition.x, frontSlots[0].localPosition.z), row: 1, slotIndex: 3, cost: 1);
        gameManager.PlayCard(0, CardID.SollarForce, new Vector2(frontSlots[0].localPosition.x, frontSlots[0].localPosition.z), row: 1, slotIndex: 4, cost: 1);

        // Despertamos a la IA (Que ahora usará sus propias casillas del Inspector)
        aiController.ExecuteDecisionLogic();

        // Detonamos la batalla
        gameManager.SetPlayerReady(0);
        gameManager.SetPlayerReady(1);
    }
}