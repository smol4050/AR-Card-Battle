using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialAIController : MonoBehaviour
{
    public enum AIDifficulty { Beginner, Normal, Superior }

    [SerializeField] private GameManager gameManager;
    public AIDifficulty currentDifficulty = AIDifficulty.Superior;

    private readonly int _aiPlayerId = 1;
    private bool _isActive = false;

    private readonly int _maxUnits = 6;
    private readonly float _boardRadiusX = 2f;
    private readonly float _boardRadiusY = 1.5f;

    // Memoria a corto plazo de lo que compró en esta ronda
    private List<CardID> _armyPurchasedThisRound = new List<CardID>();

    public void ActivateAI()
    {
        _isActive = true;
        AIDataBank.LoadBrain(); // Carga la memoria persistente

        gameManager.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(RoundPhase phase)
    {
        if (!_isActive) return;

        if (phase == RoundPhase.Preparation)
        {
            _armyPurchasedThisRound.Clear();
            ExecuteDecisionLogic();
            gameManager.SetPlayerReady(_aiPlayerId);
        }
        else if (phase == RoundPhase.End)
        {
            EvaluateBattleResult();
        }
    }

    public void ExecuteDecisionLogic()
    {
        int currentEnergy = gameManager.players[_aiPlayerId].energy;
        List<CardID> targetArmy = null;

        // Si es Superior o Normal, consulta la memoria histórica
        if (currentDifficulty != AIDifficulty.Beginner)
        {
            targetArmy = AIDataBank.GetBestHistoricalArmy();
        }

        // Si encontró una estrategia ganadora en memoria, intenta ejecutarla (Modo Mahoraga)
        if (targetArmy != null && targetArmy.Count > 0)
        {
            Debug.Log("AI using memory: Executing historical winning strategy!");
            foreach (CardID card in targetArmy)
            {
                if (currentEnergy <= 0) break;

                if (AttemptPurchase(card, out int cost))
                {
                    currentEnergy -= cost;
                }
            }
        }

        // Si sobró energía (o es Principiante, o no tenía memoria), rellena aleatoriamente
        while (currentEnergy > 0 && GetCurrentUnitCount() < _maxUnits)
        {
            CardID randomCard = DecideNextRandomCard();

            if (AttemptPurchase(randomCard, out int cost))
            {
                currentEnergy -= cost;
            }
            else
            {
                break; // Fallback de seguridad
            }
        }
    }

    private bool AttemptPurchase(CardID chosenCard, out int cost)
    {
        cost = 1; // Para el prototipo todas cuestan 1

        // Validación de Límite de Tablero
        int projectedCount = GetCurrentUnitCount() + ((chosenCard == CardID.VoidHorde) ? 4 : 1);
        if (projectedCount > _maxUnits)
        {
            chosenCard = CardID.VoidHeavyShooter; // Corrección forzosa
        }

        Vector2 randomSpawn = new Vector2(
            Random.Range(-_boardRadiusX, _boardRadiusX),
            Random.Range(0.5f, _boardRadiusY)
        );

        if (gameManager.PlayCard(_aiPlayerId, chosenCard, randomSpawn, cost))
        {
            _armyPurchasedThisRound.Add(chosenCard);
            gameManager.LogMessage(_aiPlayerId, $"AI purchased {chosenCard}");
            return true;
        }
        return false;
    }

    private int GetCurrentUnitCount()
    {
        return gameManager.activeUnits[_aiPlayerId].Count;
    }

    private CardID DecideNextRandomCard()
    {
        bool hasCommander = _armyPurchasedThisRound.Contains(CardID.VoidCommander);

        if (!hasCommander && Random.value > 0.6f) return CardID.VoidCommander;
        return (Random.value > 0.5f) ? CardID.VoidHorde : CardID.VoidHeavyShooter;
    }

    // --- EVALUACIÓN DE BATALLA (MAHORAGA SYSTEM) ---
    private void EvaluateBattleResult()
    {
        if (_armyPurchasedThisRound.Count == 0) return;

        // Determina si la IA ganó (Si el jugador local tiene 0 hp o 0 unidades)
        bool aiWon = gameManager.players[0].hp <= 0 || gameManager.activeUnits[0].Count == 0;

        Debug.Log($"AI Battle Over. AI Won: {aiWon}. Recording strategy to DataBank...");

        // Envía el reporte al cerebro persistente
        AIDataBank.RecordBattleResult(_armyPurchasedThisRound, aiWon);
    }
}