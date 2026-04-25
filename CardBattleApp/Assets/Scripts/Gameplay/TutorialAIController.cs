using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TutorialAIController : MonoBehaviour
{
    public enum AIDifficulty { Beginner, Normal, Superior }

    [SerializeField] private GameManager gameManager;
    public AIDifficulty currentDifficulty = AIDifficulty.Superior;

    [Header("Cartas disponibles para la IA")]
    public List<CardID> availableCards;

    [Header("Slots de despliegue")]
    public Transform[] frontSlots = new Transform[3];
    public Transform[] backSlots = new Transform[3];

    private List<AISlotDecision> _currentDecisions = new List<AISlotDecision>();
    private List<CardID> _enemyDraftSeen = new List<CardID>();
    private bool _isActive;

    public void ActivateAI()
    {
        _isActive = true;
        AIDataBank.LoadBrain();
        gameManager.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (gameManager != null) gameManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (!_isActive) return;
        if (phase == RoundPhase.Preparation) ExecuteDecisionLogic();
        if (phase == RoundPhase.End) EvaluateResult();
    }

    public void ExecuteDecisionLogic()
    {
        if (gameManager.currentPhase != RoundPhase.Preparation) return;

        _currentDecisions.Clear();
        _enemyDraftSeen.Clear();

        // Les damos 10 de energía como solicitaste
        gameManager.players[1].energy = 10;
        int energy = gameManager.players[1].energy;

        foreach (Unit u in gameManager.activeUnits[0])
            if (!u.IsDead) _enemyDraftSeen.Add(u.cardId);

        List<AISlotDecision> bestStrategy = null;
        if (currentDifficulty == AIDifficulty.Superior)
            bestStrategy = AIDataBank.GetBestCounterStrategy(_enemyDraftSeen);

        if (bestStrategy != null)
        {
            foreach (var decision in bestStrategy)
            {
                if (energy <= 0) break;
                if (availableCards.Contains(decision.cardId) && PlaceUnit(decision.cardId, decision.slotIndex))
                    energy--;
            }
        }

        // BUCLE CORREGIDO: Evitamos que haga "break" si falla un slot.
        int failsafe = 0; // Para evitar bucles infinitos si el tablero se llena
        while (energy > 0 && _currentDecisions.Count < 6 && failsafe < 20)
        {
            CardID next = DecideNextCard();
            int slot = FindBestSlot(next);

            if (slot == -1 || !PlaceUnit(next, slot))
            {
                failsafe++;
                continue; // En lugar de break, intentamos con otra carta/slot
            }

            energy -= (next == CardID.SollarForce) ? 2 : 1;
            failsafe++;
        }

        gameManager.SetPlayerReady(1);
    }

    private CardID DecideNextCard()
    {
        if (availableCards == null || availableCards.Count == 0) return CardID.VoidHorde;

        bool hasCommander = _currentDecisions.Exists(
            d => d.cardId == CardID.VoidCommander || d.cardId == CardID.SollarCommander);

        // Sinergia: Heavy Shooter necesita debuffers
        if (_currentDecisions.Exists(d => d.cardId == CardID.VoidHeavyShooter))
        {
            CardID debuffer = availableCards.FirstOrDefault(
                c => c == CardID.VoidCommander || c == CardID.SollarForce);
            if (debuffer != CardID.None && Random.value > 0.5f) return debuffer;
        }

        // Counter: Anti-Heal vs Sustain enemigo
        if (_enemyDraftSeen.Contains(CardID.SollarDuelist) && currentDifficulty == AIDifficulty.Superior)
            if (availableCards.Contains(CardID.SollarForce) && Random.value > 0.7f)
                return CardID.SollarForce;

        // Restricción Commander
        List<CardID> deck = new List<CardID>(availableCards);
        // Excluir naves de la fase de preparación (se juegan en combate)
        deck.RemoveAll(c => c == CardID.SolarVanguard || c == CardID.AbyssReaper);
        if (hasCommander)
            deck.RemoveAll(c => c == CardID.VoidCommander || c == CardID.SollarCommander);

        if (deck.Count == 0) return availableCards[0];
        return deck[Random.Range(0, deck.Count)];
    }

    private int FindBestSlot(CardID card)
    {
        bool isFrontline = card == CardID.VoidHorde
                        || card == CardID.SollarDuelist
                        || card == CardID.SollarCommander
                        || card == CardID.VoidCommander;

        int start = isFrontline ? 0 : 3;
        int end = isFrontline ? 3 : 6;

        for (int i = start; i < end; i++)
            if (!_currentDecisions.Exists(d => d.slotIndex == i)) return i;

        for (int i = 0; i < 6; i++)
            if (!_currentDecisions.Exists(d => d.slotIndex == i)) return i;

        return -1;
    }

    private bool PlaceUnit(CardID card, int slot)
    {
        int row = (slot < 3) ? 0 : 1;
        Transform t = (slot < 3) ? frontSlots[slot] : backSlots[slot - 3];

        BoxCollider box = t.GetComponent<BoxCollider>();
        Vector2 pos = box != null
            ? new Vector2(t.localPosition.x + box.center.x, t.localPosition.z + box.center.z)
            : new Vector2(t.localPosition.x, t.localPosition.z);

        int cost = (card == CardID.SollarForce) ? 2 : 1;

        if (!gameManager.PlayCard(1, card, pos, row, slot, cost)) return false;
        _currentDecisions.Add(new AISlotDecision { cardId = card, slotIndex = slot });
        return true;
    }

    private void EvaluateResult()
    {
        if (_currentDecisions.Count == 0) return;
        bool aiWon = gameManager.players[0].hp <= 0 || gameManager.activeUnits[0].Count == 0;
        AIDataBank.RecordBattleResult(_enemyDraftSeen, _currentDecisions, aiWon);
        Debug.Log($"<color=lime>Mahoraga: {(aiWon ? "Victoria" : "Derrota")}.</color>");
    }
}