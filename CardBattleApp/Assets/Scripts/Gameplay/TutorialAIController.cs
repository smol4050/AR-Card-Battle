using UnityEngine;
using System.Collections.Generic;

public class TutorialAIController : MonoBehaviour
{
    public enum AIDifficulty { Beginner, Normal, Superior }

    [SerializeField] private GameManager gameManager;
    public AIDifficulty currentDifficulty = AIDifficulty.Superior;

    [Header("Zonas de Despliegue")]
    public Transform[] frontSlots = new Transform[3];
    public Transform[] backSlots = new Transform[3];

    private List<AISlotDecision> _currentDecisions = new List<AISlotDecision>();
    private bool _isActive = false;

    public void ActivateAI()
    {
        _isActive = true;
        AIDataBank.LoadBrain();
        gameManager.OnPhaseChanged += (phase) => {
            if (phase == RoundPhase.Preparation) ExecuteDecisionLogic();
            if (phase == RoundPhase.End) EvaluateResult();
        };
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnPhaseChanged -= (phase) => { }; // Desuscripción genérica
        }
    }

    public void ExecuteDecisionLogic()
    {
        if (gameManager.currentPhase != RoundPhase.Preparation) return;

        _currentDecisions.Clear();
        int energy = gameManager.players[1].energy;

        var bestStrategy = AIDataBank.GetBestHistoricalArmy();
        if (bestStrategy != null && currentDifficulty != AIDifficulty.Beginner)
        {
            foreach (var decision in bestStrategy)
            {
                if (energy <= 0) break;
                if (PlaceUnit(decision.cardId, decision.slotIndex)) energy--;
            }
        }

        while (energy > 0 && _currentDecisions.Count < 6)
        {
            CardID nextCard = DecideNextCard();
            int bestSlot = FindBestSlotForRole(nextCard);

            if (bestSlot != -1 && PlaceUnit(nextCard, bestSlot)) energy--;
            else break;
        }

        gameManager.SetPlayerReady(1);
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

        // Extracción precisa usando BoxCollider
        Vector2 pos;
        BoxCollider box = t.GetComponent<BoxCollider>();
        if (box != null)
        {
            pos = new Vector2(t.localPosition.x + box.center.x, t.localPosition.z + box.center.z);
        }
        else
        {
            pos = new Vector2(t.localPosition.x, t.localPosition.z);
        }

        if (gameManager.PlayCard(1, card, pos, row, slot, 1))
        {
            _currentDecisions.Add(new AISlotDecision { cardId = card, slotIndex = slot });
            return true;
        }
        return false;
    }

    private CardID DecideNextCard()
    {
        bool hasCmd = _currentDecisions.Exists(d => d.cardId == CardID.VoidCommander);
        if (!hasCmd && Random.value > 0.7f) return CardID.VoidCommander;
        return (Random.value > 0.5f) ? CardID.VoidHorde : CardID.VoidHeavyShooter;
    }

    private void EvaluateResult()
    {
        if (_currentDecisions.Count == 0) return;
        bool win = gameManager.players[0].hp <= 0 || gameManager.activeUnits[0].Count == 0;
        AIDataBank.RecordBattleResult(_currentDecisions, win);
    }
}