using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TutorialAIController aiController;

    [Header("Zonas de Despliegue del Jugador")]
    [Tooltip("Frontline — row 0, slots 0-2")]
    public Transform[] frontSlots = new Transform[3];
    [Tooltip("Backline  — row 1, slots 3-5")]
    public Transform[] backSlots = new Transform[3];

    public void StartInGameTutorial()
    {
        Debug.Log("Tutorial: STRATEGIC FOCUS BATTLE STARTING!");

        gameManager.players[0].energy = 10;
        gameManager.players[1].energy = 10;

        if (frontSlots.Length < 3 || backSlots.Length < 3)
        {
            Debug.LogError("Faltan casillas por asignar en el TutorialManager.");
            return;
        }

        // ── JUGADOR 0: SOLLAR ALLIANCE ────────────────────────────────────────
        // Frontline: 2× SollarDuelist + SollarCommander
        gameManager.PlayCard(0, CardID.SollarDuelist,
            SlotPos(frontSlots[0]), row: 0, slotIndex: 0, cost: 1);
        gameManager.PlayCard(0, CardID.SollarCommander,
            SlotPos(frontSlots[1]), row: 0, slotIndex: 1, cost: 1);
        gameManager.PlayCard(0, CardID.SollarDuelist,
            SlotPos(frontSlots[2]), row: 0, slotIndex: 2, cost: 1);

        // Backline: 3× SOL-9 (cost:2 según diseño)
        gameManager.PlayCard(0, CardID.SollarForce,
            SlotPos(backSlots[0]), row: 1, slotIndex: 3, cost: 2);
        gameManager.PlayCard(0, CardID.SollarForce,
            SlotPos(backSlots[1]), row: 1, slotIndex: 4, cost: 2);
        gameManager.PlayCard(0, CardID.SollarForce,
            SlotPos(backSlots[2]), row: 1, slotIndex: 5, cost: 2);

        // ── IA (Jugador 1) ─────────────────────────────────────────────────────
        aiController.ExecuteDecisionLogic();

        // ── Detonar la batalla ─────────────────────────────────────────────────
        gameManager.SetPlayerReady(0);
        gameManager.SetPlayerReady(1);
    }

    private UnityEngine.Vector2 SlotPos(Transform t)
        => new UnityEngine.Vector2(t.localPosition.x, t.localPosition.z);
}