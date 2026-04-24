using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TutorialAIController : MonoBehaviour
{
    public enum AIDifficulty { Beginner, Normal, Superior }

    [SerializeField] private GameManager gameManager;
    public AIDifficulty currentDifficulty = AIDifficulty.Superior;

    [Header("Configuración de Facción de la IA")]
    public List<CardID> availableCards;

    [Header("Zonas de Despliegue")]
    public Transform[] frontSlots = new Transform[3];
    public Transform[] backSlots = new Transform[3];

    private List<AISlotDecision> _currentDecisions = new List<AISlotDecision>();
    private List<CardID> _enemyDraftSeen = new List<CardID>();
    private bool _isActive = false;

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

        // La IA siempre juega con el Jugador 1 (Index 1)
        int energy = gameManager.players[1].energy;

        // 1. ESCANEO DE AMENAZAS (Análisis del fenómeno enemigo)
        foreach (Unit enemyUnit in gameManager.activeUnits[0])
        {
            if (!enemyUnit.IsDead) _enemyDraftSeen.Add(enemyUnit.cardId);
        }

        // 2. ADAPTACIÓN (Búsqueda en el banco de datos de Mahoraga)
        List<AISlotDecision> bestStrategy = null;
        if (currentDifficulty == AIDifficulty.Superior)
        {
            bestStrategy = AIDataBank.GetBestCounterStrategy(_enemyDraftSeen);
        }

        // 3. DESPLIEGUE DE CONTRAMEDIDA
        if (bestStrategy != null)
        {
            foreach (var decision in bestStrategy)
            {
                if (energy <= 0) break;
                if (availableCards.Contains(decision.cardId) && PlaceUnit(decision.cardId, decision.slotIndex))
                {
                    energy--;
                }
            }
        }

        // 4. EVOLUCIÓN E IMPROVISACIÓN (Si sobra energía o no hay datos previos)
        while (energy > 0 && _currentDecisions.Count < 6)
        {
            CardID nextCard = DecideNextCardFromDeck();
            int bestSlot = FindBestSlotForRole(nextCard);

            if (bestSlot != -1 && PlaceUnit(nextCard, bestSlot)) energy--;
            else break;
        }

        gameManager.SetPlayerReady(1);
    }

    private CardID DecideNextCardFromDeck()
    {
        if (availableCards == null || availableCards.Count == 0) return CardID.VoidHorde;

        bool hasCommander = _currentDecisions.Exists(d => d.cardId == CardID.VoidCommander || d.cardId == CardID.SollarCommander);

        // --- LÓGICA DE SINERGIA REFORZADA ---

        // A. Sinergia de Ejecución (Heavy Shooter + Debuffs)
        bool needsDebuffs = _currentDecisions.Exists(d => d.cardId == CardID.VoidHeavyShooter);
        if (needsDebuffs)
        {
            CardID debuffer = availableCards.FirstOrDefault(c => c == CardID.VoidCommander || c == CardID.SollarForce);
            if (debuffer != CardID.None && Random.value > 0.5f) return debuffer;
        }

        // B. Contramedida de Sustain (Anti-Heal vs Duelists)
        bool enemyHasSustain = _enemyDraftSeen.Contains(CardID.SollarDuelist);
        if (enemyHasSustain && currentDifficulty == AIDifficulty.Superior)
        {
            if (availableCards.Contains(CardID.SollarForce) && Random.value > 0.7f) return CardID.SollarForce;
        }

        // C. Restricción de Comandante (Unicidad)
        List<CardID> safeDeck = new List<CardID>(availableCards);
        if (hasCommander)
        {
            safeDeck.RemoveAll(c => c == CardID.VoidCommander || c == CardID.SollarCommander);
        }

        if (safeDeck.Count == 0) return availableCards[0];
        return safeDeck[Random.Range(0, safeDeck.Count)];
    }

    private int FindBestSlotForRole(CardID card)
    {
        // Frontline: Bruisers y Tanques
        bool isFrontline = card == CardID.VoidHorde ||
                           card == CardID.SollarDuelist ||
                           card == CardID.SollarCommander ||
                           card == CardID.VoidCommander;

        int start = isFrontline ? 0 : 3;
        int end = isFrontline ? 3 : 6;

        for (int i = start; i < end; i++)
        {
            if (!_currentDecisions.Exists(d => d.slotIndex == i)) return i;
        }

        for (int i = 0; i < 6; i++)
        {
            if (!_currentDecisions.Exists(d => d.slotIndex == i)) return i;
        }
        return -1;
    }

    private bool PlaceUnit(CardID card, int slot)
    {
        int row = (slot < 3) ? 0 : 1;
        Transform t = (slot < 3) ? frontSlots[slot] : backSlots[slot - 3];

        Vector2 pos;
        BoxCollider box = t.GetComponent<BoxCollider>();
        if (box != null) pos = new Vector2(t.localPosition.x + box.center.x, t.localPosition.z + box.center.z);
        else pos = new Vector2(t.localPosition.x, t.localPosition.z);

        if (gameManager.PlayCard(1, card, pos, row, slot, 1))
        {
            _currentDecisions.Add(new AISlotDecision { cardId = card, slotIndex = slot });
            return true;
        }
        return false;
    }

    private void EvaluateResult()
    {
        if (_currentDecisions.Count == 0) return;

        // Mahoraga analiza si el resultado fue una victoria para el bando de la IA
        // Se considera victoria si el bando enemigo (Jugador 0) se quedó sin HP o unidades
        bool aiWon = gameManager.players[0].hp <= 0 || gameManager.activeUnits[0].Count == 0;

        AIDataBank.RecordBattleResult(_enemyDraftSeen, _currentDecisions, aiWon);
        Debug.Log($"<color=lime>Mahoraga: Ciclo de aprendizaje completado. Resultado: {(aiWon ? "Victoria" : "Derrota")}.</color>");
    }
}