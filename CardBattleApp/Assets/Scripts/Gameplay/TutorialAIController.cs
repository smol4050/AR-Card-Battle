using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// IA del jugador 1 (Void Dominion).
/// En modo AR recibe los slots dinámicamente desde BattlefieldReferences.
/// En modo estático usa los arrays asignados en el Inspector.
/// </summary>
public class TutorialAIController : MonoBehaviour
{
    public enum AIDifficulty { Beginner, Normal, Superior }

    [SerializeField] private GameManager gameManager;
    public AIDifficulty currentDifficulty = AIDifficulty.Superior;

    [Header("Cartas disponibles para la IA (Void)")]
    public List<CardID> availableCards;

    [Header("Slots estáticos (se ignoran si hay tablero AR)")]
    public Transform[] frontSlots = new Transform[3];
    public Transform[] backSlots = new Transform[3];

    // Slots dinámicos (inyectados desde BattlefieldReferences al spawnearse el tablero)
    private Transform[] _dynamicFrontSlots;
    private Transform[] _dynamicBackSlots;
    private bool _usingDynamicBoard;

    private List<AISlotDecision> _currentDecisions = new List<AISlotDecision>();
    private List<CardID> _enemyDraftSeen = new List<CardID>();
    private bool _isActive;

    // ─── INYECCIÓN DE TABLERO DINÁMICO (AR) ──────────────────────────────────
    /// <summary>
    /// Llamado por ARDeployFlow o ARCardScanner cuando el tablero AR se instancia.
    /// Los slots de la IA son los del jugador 1 (p1).
    /// </summary>
    public void RegisterDynamicBoard(BattlefieldReferences board)
    {
        if (board == null)
        {
            Debug.LogError("[TutorialAIController] RegisterDynamicBoard: BattlefieldReferences es null.");
            return;
        }

        _dynamicFrontSlots = board.p1FrontSlots;
        _dynamicBackSlots = board.p1BackSlots;
        _usingDynamicBoard = true;

        Debug.Log("<color=lime>[TutorialAIController] Tablero dinámico AR registrado para la IA.</color>");
    }

    // ─── ACTIVACIÓN ───────────────────────────────────────────────────────────
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

    // ─── DESPLIEGUE ───────────────────────────────────────────────────────────
    public void ExecuteDecisionLogic()
    {
        if (gameManager.currentPhase != RoundPhase.Preparation) return;

        _currentDecisions.Clear();
        _enemyDraftSeen.Clear();

        gameManager.players[1].energy = 10;
        int energy = gameManager.players[1].energy;

        // Escaneo de amenazas
        foreach (Unit u in gameManager.activeUnits[0])
            if (!u.IsDead) _enemyDraftSeen.Add(u.cardId);

        // Adaptación Mahoraga
        List<AISlotDecision> bestStrategy = null;
        if (currentDifficulty == AIDifficulty.Superior)
            bestStrategy = AIDataBank.GetBestCounterStrategy(_enemyDraftSeen);

        if (bestStrategy != null)
        {
            foreach (var decision in bestStrategy)
            {
                if (energy <= 0) break;
                if (availableCards.Contains(decision.cardId) && PlaceUnit(decision.cardId, decision.slotIndex))
                    energy -= GetCost(decision.cardId);
            }
        }

        // Improvisación con failsafe para evitar loop infinito
        int failsafe = 0;
        while (energy > 0 && _currentDecisions.Count < 6 && failsafe < 20)
        {
            failsafe++;
            CardID next = DecideNextCard();
            int cost = GetCost(next);
            if (energy < cost) { failsafe += 5; continue; } // sin energía para esta carta
            int slot = FindBestSlot(next);
            if (slot == -1 || !PlaceUnit(next, slot)) continue;
            energy -= cost;
        }

        gameManager.SetPlayerReady(1);
        Debug.Log($"<color=lime>[Mahoraga] Despliegue completado. {_currentDecisions.Count} unidades en campo.</color>");
    }

    private int GetCost(CardID card)
    {
        if (card == CardID.SollarForce) return 2;
        if (card == CardID.SolarVanguard) return 3;
        if (card == CardID.AbyssReaper) return 3;
        return 1;
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

        List<CardID> deck = new List<CardID>(availableCards);
        // Naves se juegan en combate, no en preparación
        deck.RemoveAll(c => c == CardID.SolarVanguard || c == CardID.AbyssReaper);
        if (hasCommander)
            deck.RemoveAll(c => c == CardID.VoidCommander || c == CardID.SollarCommander);

        if (deck.Count == 0) return CardID.VoidHorde;
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
        // Seleccionar fuente de slots (dinámica AR o estática Inspector)
        Transform[] front = _usingDynamicBoard ? _dynamicFrontSlots : frontSlots;
        Transform[] back = _usingDynamicBoard ? _dynamicBackSlots : backSlots;

        if (front == null || back == null)
        {
            Debug.LogError("[TutorialAIController] Slots no configurados. " +
                           "Asigna slots estáticos en el Inspector o llama RegisterDynamicBoard().");
            return false;
        }

        int row = (slot < 3) ? 0 : 1;
        Transform t = (slot < 3) ? front[slot] : back[slot - 3];

        if (t == null)
        {
            Debug.LogError($"[TutorialAIController] Slot Transform null para slotIndex={slot}.");
            return false;
        }

        BoxCollider box = t.GetComponent<BoxCollider>();
        Vector2 pos = box != null
            ? new Vector2(t.localPosition.x + box.center.x, t.localPosition.z + box.center.z)
            : new Vector2(t.localPosition.x, t.localPosition.z);

        int cost = GetCost(card);
        if (!gameManager.PlayCard(1, card, pos, row, slot, cost)) return false;

        _currentDecisions.Add(new AISlotDecision { cardId = card, slotIndex = slot });
        return true;
    }

    // ─── EVALUACIÓN ───────────────────────────────────────────────────────────
    private void EvaluateResult()
    {
        if (_currentDecisions.Count == 0) return;
        bool aiWon = gameManager.players[0].hp <= 0 || gameManager.activeUnits[0].Count == 0;
        AIDataBank.RecordBattleResult(_enemyDraftSeen, _currentDecisions, aiWon);
        Debug.Log($"<color=lime>[Mahoraga] Resultado: {(aiWon ? "Victoria" : "Derrota")}.</color>");
    }
}