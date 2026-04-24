using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TutorialAIController : MonoBehaviour
{
    public enum AIDifficulty { Beginner, Normal, Superior }

    [SerializeField] private GameManager gameManager;
    public AIDifficulty currentDifficulty = AIDifficulty.Superior;

    [Header("Configuración de Facción de la IA")]
    [Tooltip("Añade aquí las cartas que esta IA tiene permitido comprar.")]
    public List<CardID> availableCards; // Permite que la IA juegue Sollar o Void

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
        int energy = gameManager.players[1].energy;

        // 1. ESCANEAR AL ENEMIGO (Revisar el tablero del jugador 0)
        foreach (Unit enemyUnit in gameManager.activeUnits[0])
        {
            _enemyDraftSeen.Add(enemyUnit.cardId);
        }

        // 2. BUSCAR EL COUNTER PERFECTO
        List<AISlotDecision> bestStrategy = null;
        if (currentDifficulty == AIDifficulty.Superior)
        {
            bestStrategy = AIDataBank.GetBestCounterStrategy(_enemyDraftSeen);
        }

        // 3. EJECUTAR ESTRATEGIA (Si existe y tiene recursos)
        if (bestStrategy != null)
        {
            foreach (var decision in bestStrategy)
            {
                if (energy <= 0) break;
                // Verificamos que la carta esté en su mazo permitido actual
                if (availableCards.Contains(decision.cardId) && PlaceUnit(decision.cardId, decision.slotIndex))
                {
                    energy--;
                }
            }
        }

        // 4. IMPROVISACIÓN (Rellenar espacios si sobra energía, el DataBank falló o es Principiante)
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
        if (availableCards == null || availableCards.Count == 0) return CardID.VoidHorde; // Fallback extremo

        // Elige una carta aleatoria de su mazo permitido
        int randomIndex = Random.Range(0, availableCards.Count);
        return availableCards[randomIndex];
    }

    private int FindBestSlotForRole(CardID card)
    {
        bool isFrontline = card == CardID.VoidHorde || card == CardID.SollarDuelist || card == CardID.SollarForce;
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
        bool win = gameManager.players[0].hp <= 0 || gameManager.activeUnits[0].Count == 0;

        // Guardamos el historial vinculando lo que vimos enfrente con lo que respondimos
        AIDataBank.RecordBattleResult(_enemyDraftSeen, _currentDecisions, win);
    }
}