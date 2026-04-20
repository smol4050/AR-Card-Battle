using NUnit.Framework;
using UnityEngine;

public class LaneManagerTests
{
    [Test]
    public void TryPlaceCard_WhenLaneIsEmpty_PlacesCardAndReturnsTrue()
    {
        // 1. ARRANGE (Preparar)
        GameObject managerObj = new GameObject("LaneManager");
        LaneManager laneManager = managerObj.AddComponent<LaneManager>();

        laneManager.player1LanePositions = new Transform[3];
        for (int i = 0; i < 3; i++) laneManager.player1LanePositions[i] = new GameObject($"P1_Pos_{i}").transform;

        GameObject unitObj = new GameObject("TestUnit");
        CharacterCombat testUnit = unitObj.AddComponent<CharacterCombat>();
        testUnit.InitializeStats(10, 2);

        // 2. ACT (Actuar)
        bool success = laneManager.TryPlaceCard(0, 1, testUnit);

        // 3. ASSERT (Comprobar)
        Assert.IsTrue(success);
        Assert.AreEqual(testUnit, laneManager.GetUnit(0, 1));

        Object.DestroyImmediate(managerObj);
        Object.DestroyImmediate(unitObj);
    }

    [Test]
    public void TryPlaceCard_WhenLaneIsOccupied_ReturnsFalseAndDoesNotReplace()
    {
        // 1. ARRANGE (Preparar)
        GameObject managerObj = new GameObject("LaneManager");
        LaneManager laneManager = managerObj.AddComponent<LaneManager>();

        laneManager.player1LanePositions = new Transform[3];
        for (int i = 0; i < 3; i++) laneManager.player1LanePositions[i] = new GameObject($"P1_Pos_{i}").transform;

        GameObject unitObj1 = new GameObject("FirstUnit");
        CharacterCombat firstUnit = unitObj1.AddComponent<CharacterCombat>();
        laneManager.TryPlaceCard(0, 0, firstUnit);

        GameObject unitObj2 = new GameObject("SecondUnit");
        CharacterCombat secondUnit = unitObj2.AddComponent<CharacterCombat>();

        // 2. ACT (Actuar)
        bool success = laneManager.TryPlaceCard(0, 0, secondUnit);

        // 3. ASSERT (Comprobar)
        Assert.IsFalse(success);
        Assert.AreEqual(firstUnit, laneManager.GetUnit(0, 0));

        Object.DestroyImmediate(managerObj);
        Object.DestroyImmediate(unitObj1);
        Object.DestroyImmediate(unitObj2);
    }

    [Test]
    public void CleanUpDeadUnits_RemovesDeadUnitsFromGrid()
    {
        // 1. ARRANGE (Preparar)
        GameObject managerObj = new GameObject("LaneManager");
        LaneManager laneManager = managerObj.AddComponent<LaneManager>();

        laneManager.player1LanePositions = new Transform[3];
        for (int i = 0; i < 3; i++) laneManager.player1LanePositions[i] = new GameObject($"P1_Pos_{i}").transform;

        GameObject unitObj = new GameObject("UnitToDie");
        CharacterCombat unit = unitObj.AddComponent<CharacterCombat>();
        unit.InitializeStats(10, 2);

        laneManager.TryPlaceCard(0, 2, unit); // Colocamos en el jugador 0, carril 2

        // Reducimos la vida a 0 para que su propiedad IsDead sea true
        unit.TakeDamage(100);

        // 2. ACT (Actuar)
        laneManager.CleanUpDeadUnits();

        // 3. ASSERT (Comprobar)
        // Solo verificamos que el LaneManager limpió su matriz lógica correctamente.
        // No asertamos sobre el GameObject porque la destrucción de memoria es tarea de Unity.
        Assert.IsNull(laneManager.GetUnit(0, 2));

        Object.DestroyImmediate(managerObj);
    }
}