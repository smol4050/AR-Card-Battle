using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[System.Serializable]
public class AISlotDecision
{
    public CardID cardId;
    public int slotIndex; // De 0 a 5
}

[System.Serializable]
public class AIBoardState
{
    public List<AISlotDecision> decisions; // Lista de (Qué, Dónde)
    public float winWeight;

    public AIBoardState(List<AISlotDecision> decisions, float initialWeight)
    {
        this.decisions = new List<AISlotDecision>(decisions);
        this.winWeight = initialWeight;
    }
}

[System.Serializable]
public class AIDatabase
{
    public List<AIBoardState> historicalStates = new List<AIBoardState>();
}

public static class AIDataBank
{
    private static string _savePath = Application.persistentDataPath + "/AIBrain_v2.json";
    private static AIDatabase _currentDB = new AIDatabase();

    public static void LoadBrain()
    {
        if (File.Exists(_savePath))
        {
            string json = File.ReadAllText(_savePath);
            _currentDB = JsonUtility.FromJson<AIDatabase>(json);
        }
    }

    public static List<AISlotDecision> GetBestHistoricalArmy()
    {
        if (_currentDB.historicalStates.Count == 0) return null;
        var best = _currentDB.historicalStates.OrderByDescending(s => s.winWeight).FirstOrDefault();
        return (best != null && best.winWeight > 0) ? best.decisions : null;
    }

    public static void RecordBattleResult(List<AISlotDecision> battleDecisions, bool didAIWin)
    {
        float weightDelta = didAIWin ? 1.0f : -0.5f;

        // Buscamos si esta configuración exacta de posiciones ya existe
        var existing = _currentDB.historicalStates.Find(s => IsSameStrategy(s.decisions, battleDecisions));

        if (existing != null) existing.winWeight += weightDelta;
        else _currentDB.historicalStates.Add(new AIBoardState(battleDecisions, weightDelta));

        File.WriteAllText(_savePath, JsonUtility.ToJson(_currentDB, true));
    }

    private static bool IsSameStrategy(List<AISlotDecision> a, List<AISlotDecision> b)
    {
        if (a.Count != b.Count) return false;
        // Compara si todos los pares (Carta, Slot) coinciden
        return a.All(da => b.Any(db => db.cardId == da.cardId && db.slotIndex == da.slotIndex));
    }
}