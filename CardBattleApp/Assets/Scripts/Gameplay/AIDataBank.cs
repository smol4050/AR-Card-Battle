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
    public List<CardID> enemyArmy; // El Draft del rival
    public List<AISlotDecision> aiDecisions; // La respuesta de la IA
    public float winWeight;

    public AIBattleRecord(List<CardID> enemy, List<AISlotDecision> decisions, float weight)
    {
        enemyArmy = new List<CardID>(enemy);
        enemyArmy.Sort(); // Ordenamos para comparar fácilmente
        aiDecisions = new List<AISlotDecision>(decisions);
        winWeight = weight;
    }
}

[System.Serializable]
public class AIDatabase
{
    public List<AIBattleRecord> historicalRecords = new List<AIBattleRecord>();
}

public static class AIDataBank
{
    private static string _savePath = Application.persistentDataPath + "/AIBrain_Superior.json";
    private static AIDatabase _currentDB = new AIDatabase();

    public static void LoadBrain()
    {
        if (File.Exists(_savePath))
        {
            string json = File.ReadAllText(_savePath);
            _currentDB = JsonUtility.FromJson<AIDatabase>(json);
        }
    }

    // El motor de búsqueda que encuentra el counter perfecto
    public static List<AISlotDecision> GetBestCounterStrategy(List<CardID> currentEnemyBoard)
    {
        if (_currentDB.historicalRecords.Count == 0) return null;

        var sortedEnemy = new List<CardID>(currentEnemyBoard);
        sortedEnemy.Sort();

        // 1. Buscar una coincidencia exacta del tablero enemigo con winrate positivo
        var exactMatches = _currentDB.historicalRecords
            .Where(r => r.enemyArmy.SequenceEqual(sortedEnemy) && r.winWeight > 0)
            .OrderByDescending(r => r.winWeight)
            .ToList();

        if (exactMatches.Count > 0)
        {
            Debug.Log("AI Brain: ¡Match exacto encontrado! Ejecutando counter-pick.");
            return exactMatches.First().aiDecisions;
        }

        // 2. Si no hay coincidencia exacta, buscar la estrategia con mayor winrate general
        // (Esto simula que la IA confía en su "Composición Meta" cuando no conoce el matchup)
        var metaStrategy = _currentDB.historicalRecords
            .Where(r => r.winWeight > 0)
            .OrderByDescending(r => r.winWeight)
            .FirstOrDefault();

        if (metaStrategy != null)
        {
            Debug.Log("AI Brain: Matchup desconocido. Ejecutando la mejor composición general (Meta).");
            return metaStrategy.aiDecisions;
        }

        return null; // La IA no tiene idea de qué hacer, tendrá que improvisar (Random)
    }

    public static void RecordBattleResult(List<CardID> enemyArmy, List<AISlotDecision> battleDecisions, bool didAIWin)
    {
        float weightDelta = didAIWin ? 1.0f : -0.5f;
        var sortedEnemy = new List<CardID>(enemyArmy);
        sortedEnemy.Sort();

        // Buscamos si ya existe este cruce específico de (Rival VS IA)
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

        File.WriteAllText(_savePath, JsonUtility.ToJson(_currentDB, true));
    }

    private static bool IsSameStrategy(List<AISlotDecision> a, List<AISlotDecision> b)
    {
        if (a.Count != b.Count) return false;
        return a.All(da => b.Any(db => db.cardId == da.cardId && db.slotIndex == da.slotIndex));
    }
}