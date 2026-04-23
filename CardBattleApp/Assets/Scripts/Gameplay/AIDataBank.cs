using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[System.Serializable]
public class AIBoardState
{
    public List<CardID> aiArmy; // La combinación que la IA compró
    public float winWeight;     // Puntuación de éxito (Mayor = Mejor)

    public AIBoardState(List<CardID> army, float initialWeight)
    {
        aiArmy = new List<CardID>(army);
        winWeight = initialWeight;
    }
}

[System.Serializable]
public class AIDatabase
{
    public List<AIBoardState> historicalStates = new List<AIBoardState>();
}

public static class AIDataBank
{
    private static string _savePath = Application.persistentDataPath + "/AIBrain.json";
    private static AIDatabase _currentDB = new AIDatabase();

    public static void LoadBrain()
    {
        if (File.Exists(_savePath))
        {
            string json = File.ReadAllText(_savePath);
            _currentDB = JsonUtility.FromJson<AIDatabase>(json);
            Debug.Log("AI Brain Loaded! Memorized states: " + _currentDB.historicalStates.Count);
        }
        else
        {
            Debug.Log("No previous AI Brain found. Creating new consciousness...");
        }
    }

    public static void SaveBrain()
    {
        string json = JsonUtility.ToJson(_currentDB, true);
        File.WriteAllText(_savePath, json);
    }

    // Busca la mejor composición histórica. Si no hay datos, retorna null.
    public static List<CardID> GetBestHistoricalArmy()
    {
        if (_currentDB.historicalStates.Count == 0) return null;

        // Ordena de mayor a menor peso y toma la mejor
        var bestState = _currentDB.historicalStates.OrderByDescending(s => s.winWeight).First();

        // Si la mejor estrategia tiene peso negativo (es decir, todas son malísimas), forzamos a intentar algo nuevo
        if (bestState.winWeight <= 0) return null;

        return new List<CardID>(bestState.aiArmy);
    }

    // Actualiza la base de datos tras una batalla
    public static void RecordBattleResult(List<CardID> usedArmy, bool didAIWin)
    {
        // 1. Convertimos la lista a un formato ordenado para compararla fácilmente
        var sortedArmy = new List<CardID>(usedArmy);
        sortedArmy.Sort();

        // 2. Buscamos si ya tenemos esta combinación en la memoria
        AIBoardState existingState = null;
        foreach (var state in _currentDB.historicalStates)
        {
            var sortedStateArmy = new List<CardID>(state.aiArmy);
            sortedStateArmy.Sort();

            if (sortedStateArmy.SequenceEqual(sortedArmy))
            {
                existingState = state;
                break;
            }
        }

        // 3. Ajustamos los pesos (Mahoraga Adaptation)
        float weightDelta = didAIWin ? 1.0f : -0.5f; // Gana: Sube 1 punto. Pierde: Baja 0.5 puntos.

        if (existingState != null)
        {
            existingState.winWeight += weightDelta;
            Debug.Log($"AI adapted: Strategy recognized. New weight: {existingState.winWeight}");
        }
        else
        {
            // Nueva estrategia, la añadimos a la base de datos
            _currentDB.historicalStates.Add(new AIBoardState(usedArmy, weightDelta));
            Debug.Log($"AI adapted: New strategy recorded. Weight: {weightDelta}");
        }

        SaveBrain();
    }
}