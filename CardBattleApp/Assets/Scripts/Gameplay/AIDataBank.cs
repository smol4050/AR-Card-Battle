using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[System.Serializable]
public class AISlotDecision
{
    public CardID cardId;
    public int slotIndex;
}

[System.Serializable]
public class AIBattleRecord
{
    public List<CardID> enemyArmy;
    public List<AISlotDecision> aiDecisions;
    public float winWeight;

    public AIBattleRecord(List<CardID> enemy, List<AISlotDecision> decisions, float weight)
    {
        enemyArmy = enemy != null ? new List<CardID>(enemy) : new List<CardID>();
        enemyArmy.Sort();
        aiDecisions = decisions != null ? new List<AISlotDecision>(decisions) : new List<AISlotDecision>();
        winWeight = weight;
    }

    // Calcula qué tan similar es este record al tablero actual del jugador
    public float GetSimilarity(List<CardID> currentEnemy)
    {
        if (enemyArmy.Count == 0) return 0;
        float matches = 0;
        List<CardID> tempEnemy = new List<CardID>(currentEnemy);
        foreach (var card in enemyArmy)
        {
            if (tempEnemy.Contains(card))
            {
                matches++;
                tempEnemy.Remove(card);
            }
        }
        return matches / Mathf.Max(enemyArmy.Count, currentEnemy.Count);
    }
}

[System.Serializable]
public class AIDatabase
{
    public List<AIBattleRecord> historicalRecords = new List<AIBattleRecord>();
}

public static class AIDataBank
{
    private static string _savePath = Application.persistentDataPath + "/AIBrain_Mahoraga_v2.json";
    private static AIDatabase _currentDB = new AIDatabase();

    public static void LoadBrain()
    {
        if (File.Exists(_savePath))
        {
            try
            {
                string json = File.ReadAllText(_savePath);
                _currentDB = JsonUtility.FromJson<AIDatabase>(json);
                if (_currentDB == null) _currentDB = new AIDatabase();
            }
            catch
            {
                _currentDB = new AIDatabase();
            }
        }
    }

    public static List<AISlotDecision> GetBestCounterStrategy(List<CardID> currentEnemyBoard)
    {
        if (_currentDB.historicalRecords.Count == 0) return null;

        var sortedEnemy = new List<CardID>(currentEnemyBoard);
        sortedEnemy.Sort();

        // 1. BUSQUEDA DE ADAPTACIÓN EXACTA (Mahoraga ya conoce este fenómeno)
        var exactMatches = _currentDB.historicalRecords
            .Where(r => r.enemyArmy.SequenceEqual(sortedEnemy) && r.winWeight > 0)
            .OrderByDescending(r => r.winWeight)
            .ToList();

        if (exactMatches.Count > 0)
        {
            Debug.Log("<color=cyan>Mahoraga Brain: Fenómeno adaptado. Ejecutando counter-pick exacto.</color>");
            return exactMatches.First().aiDecisions;
        }

        // 2. BUSQUEDA POR SIMILITUD (Mahoraga está empezando a adaptarse)
        var similarMatch = _currentDB.historicalRecords
            .Where(r => r.winWeight > 0.5f) // Solo confiar en victorias sólidas
            .OrderByDescending(r => r.GetSimilarity(sortedEnemy))
            .ThenByDescending(r => r.winWeight)
            .FirstOrDefault();

        if (similarMatch != null && similarMatch.GetSimilarity(sortedEnemy) > 0.6f)
        {
            Debug.Log($"<color=orange>Mahoraga Brain: Tablero similar detectado ({similarMatch.GetSimilarity(sortedEnemy):P}). Adaptando estrategia...</color>");
            return similarMatch.aiDecisions;
        }

        // 3. META ESTRATEGIA (Estrategia general de mayor éxito)
        var metaStrategy = _currentDB.historicalRecords
            .Where(r => r.winWeight > 2)
            .OrderByDescending(r => r.winWeight)
            .FirstOrDefault();

        if (metaStrategy != null)
        {
            Debug.Log("<color=yellow>Mahoraga Brain: Usando técnica maestra (Meta de alto Winrate).</color>");
            return metaStrategy.aiDecisions;
        }

        return null;
    }

    public static void RecordBattleResult(List<CardID> enemyArmy, List<AISlotDecision> battleDecisions, bool didAIWin)
    {
        float weightDelta = didAIWin ? 1.0f : -0.7f; // Castigamos más la derrota para forzar el cambio de estrategia
        var sortedEnemy = new List<CardID>(enemyArmy);
        sortedEnemy.Sort();

        var existingRecord = _currentDB.historicalRecords.Find(r =>
            r.enemyArmy.SequenceEqual(sortedEnemy) &&
            IsSameStrategy(r.aiDecisions, battleDecisions)
        );

        if (existingRecord != null)
        {
            existingRecord.winWeight += weightDelta;
        }
        else
        {
            _currentDB.historicalRecords.Add(new AIBattleRecord(sortedEnemy, battleDecisions, weightDelta));
        }

        // Persistencia
        string json = JsonUtility.ToJson(_currentDB, true);
        File.WriteAllText(_savePath, json);
    }

    private static bool IsSameStrategy(List<AISlotDecision> a, List<AISlotDecision> b)
    {
        if (a.Count != b.Count) return false;
        return a.All(da => b.Any(db => db.cardId == da.cardId && db.slotIndex == da.slotIndex));
    }
}