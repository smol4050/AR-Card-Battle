using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TutorialAIController aiController;

    [Header("Zonas de Despliegue del Jugador (Arrastra los GameObjects)")]
    [Tooltip("Casillas de la Primera Línea (Frente) — row 0, slots 0-2")]
    public Transform[] frontSlots = new Transform[3];

    [Tooltip("Casillas de la Retaguardia (Atrás) — row 1, slots 3-5")]
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
        //
        // row 0 = Frontline (slots 0, 1, 2) → se posicionan usando frontSlots
        // row 1 = Backline  (slots 3, 4, 5) → se posicionan usando backSlots
        //
        // Frontline: 2× SollarDuelist + SollarCommander
        gameManager.PlayCard(0, CardID.SollarDuelist,
            new Vector2(frontSlots[0].localPosition.x, frontSlots[0].localPosition.z),
            row: 0, slotIndex: 0, cost: 1);

        gameManager.PlayCard(0, CardID.SollarCommander,
            new Vector2(frontSlots[1].localPosition.x, frontSlots[1].localPosition.z),
            row: 0, slotIndex: 1, cost: 1);

        gameManager.PlayCard(0, CardID.SollarDuelist,
            new Vector2(frontSlots[2].localPosition.x, frontSlots[2].localPosition.z),
            row: 0, slotIndex: 2, cost: 1);

        // Backline: 3× SOL-9 (SollarForce) — cost:2 cada una según diseño
        gameManager.PlayCard(0, CardID.SollarForce,
            new Vector2(backSlots[0].localPosition.x, backSlots[0].localPosition.z),
            row: 1, slotIndex: 3, cost: 2);

        gameManager.PlayCard(0, CardID.SollarForce,
            new Vector2(backSlots[1].localPosition.x, backSlots[1].localPosition.z),
            row: 1, slotIndex: 4, cost: 2);

        gameManager.PlayCard(0, CardID.SollarForce,
            new Vector2(backSlots[2].localPosition.x, backSlots[2].localPosition.z),
            row: 1, slotIndex: 5, cost: 2);

        // ── IA (Jugador 1) ─────────────────────────────────────────────────────
        aiController.ExecuteDecisionLogic();

        // ── Detonar la batalla ─────────────────────────────────────────────────
        gameManager.SetPlayerReady(0);
        gameManager.SetPlayerReady(1);
    }
}