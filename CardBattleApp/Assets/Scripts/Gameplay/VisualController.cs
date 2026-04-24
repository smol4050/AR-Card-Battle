using NUnit.Framework;
using UnityEngine;

public class GameManagerSlotPlacementTests
{
    [Test]
    public void PlayCard_IgnoresDummyPos_AndReliesOnSlotIndex()
    {
        // 1. Arrange
        GameObject go = new GameObject("Core");
        GameManager gm = go.AddComponent<GameManager>();
        gm.InitializeGame();
        gm.players[0].maxEnergy = 5;
        gm.players[0].energy = 5;

        // 2. Act
        // Ejecutamos PlayCard con un dummyPos (0,0) pero con slotIndex = 4
        bool success = gm.PlayCard(0, CardID.SollarDuelist, Vector2.zero, 1, 4, 1);

        // 3. Assert
        Assert.IsTrue(success, "La carta falló al instanciarse.");
        Unit spawnedUnit = gm.activeUnits[0][0];

        // Verificamos que la unidad lógica no intente calcular posiciones matemáticas prematuras
        Assert.AreEqual(Vector2.zero, spawnedUnit.logicalPosition, "La unidad intentó procesar matemáticamente una posición prematura.");
        Assert.AreEqual(4, spawnedUnit.slotIndex, "La unidad no retuvo el slotIndex crítico para el VisualController.");

        Object.DestroyImmediate(go);
    }
}