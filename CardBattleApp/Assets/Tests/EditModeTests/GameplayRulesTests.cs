using NUnit.Framework;
using UnityEngine;

public class GameplayRulesTests
{
    [Test]
    public void CanPlayCard_WithTurnEnergyAndEmptyLane_ReturnsTrue()
    {
        // 1. ARRANGE (Preparar)
        int requestingPlayer = 0;
        int currentTurn = 0; // Es el turno del jugador 0
        int energy = 3;      // Tiene energía de sobra
        CharacterCombat existingUnit = null; // El carril está vacío

        // 2. ACT (Actuar)
        bool result = GameplayRules.CanPlayCard(requestingPlayer, currentTurn, energy, existingUnit);

        // 3. ASSERT (Comprobar)
        Assert.IsTrue(result);
    }

    [Test]
    public void CanPlayCard_WhenNotPlayerTurn_ReturnsFalse()
    {
        // 1. ARRANGE (Preparar)
        int requestingPlayer = 1; // El jugador 1 intenta jugar
        int currentTurn = 0;      // Pero es el turno del jugador 0
        int energy = 5;
        CharacterCombat existingUnit = null;

        // 2. ACT (Actuar)
        bool result = GameplayRules.CanPlayCard(requestingPlayer, currentTurn, energy, existingUnit);

        // 3. ASSERT (Comprobar)
        Assert.IsFalse(result);
    }

    [Test]
    public void CanPlayCard_WhenNotEnoughEnergy_ReturnsFalse()
    {
        // 1. ARRANGE (Preparar)
        int requestingPlayer = 0;
        int currentTurn = 0;
        int energy = 0; // Sin energía
        CharacterCombat existingUnit = null;

        // 2. ACT (Actuar)
        bool result = GameplayRules.CanPlayCard(requestingPlayer, currentTurn, energy, existingUnit);

        // 3. ASSERT (Comprobar)
        Assert.IsFalse(result);
    }

    [Test]
    public void CanPlayCard_WhenLaneIsOccupied_ReturnsFalse()
    {
        // 1. ARRANGE (Preparar)
        int requestingPlayer = 0;
        int currentTurn = 0;
        int energy = 2;

        // Simulamos que ya hay una unidad en el carril
        GameObject dummyObj = new GameObject();
        CharacterCombat existingUnit = dummyObj.AddComponent<CharacterCombat>();

        // 2. ACT (Actuar)
        bool result = GameplayRules.CanPlayCard(requestingPlayer, currentTurn, energy, existingUnit);

        // 3. ASSERT (Comprobar)
        Assert.IsFalse(result);

        // Limpieza de memoria
        Object.DestroyImmediate(dummyObj);
    }
}