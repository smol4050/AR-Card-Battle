using NUnit.Framework;
using UnityEngine;

public class LaneBattleControllerTests
{
    [Test]
    public void ResolveLaneCombat_WithTwoUnits_BothTakeDamageCorrectly()
    {
        // 1. ARRANGE (Preparar)
        // Instanciamos el controlador de batalla de carriles
        GameObject controllerObj = new GameObject("LaneController");
        LaneBattleController laneController = controllerObj.AddComponent<LaneBattleController>();

        // Creamos la unidad del Jugador 1
        GameObject p1Obj = new GameObject("P1_Unit");
        CharacterCombat p1Unit = p1Obj.AddComponent<CharacterCombat>();
        p1Unit.InitializeStats(maxHealth: 10, attackPower: 3);

        // Creamos la unidad del Jugador 2
        GameObject p2Obj = new GameObject("P2_Unit");
        CharacterCombat p2Unit = p2Obj.AddComponent<CharacterCombat>();
        p2Unit.InitializeStats(maxHealth: 10, attackPower: 5);

        // 2. ACT (Actuar)
        // Ejecutamos la resolución del combate en ese carril
        laneController.ResolveLaneCombat(p1Unit, p2Unit);

        // 3. ASSERT (Comprobar)
        // La unidad 1 debió recibir 5 de daño (10 - 5 = 5)
        Assert.AreEqual(5, p1Unit.CurrentHealth);

        // La unidad 2 debió recibir 3 de daño (10 - 3 = 7)
        Assert.AreEqual(7, p2Unit.CurrentHealth);

        // Ninguno debería estar muerto aún
        Assert.IsFalse(p1Unit.IsDead);
        Assert.IsFalse(p2Unit.IsDead);

        // Limpieza
        Object.DestroyImmediate(controllerObj);
        Object.DestroyImmediate(p1Obj);
        Object.DestroyImmediate(p2Obj);
    }

    [Test]
    public void ResolveLaneCombat_WhenOneUnitDies_SetsIsDeadToTrue()
    {
        // 1. ARRANGE (Preparar)
        GameObject controllerObj = new GameObject("LaneController");
        LaneBattleController laneController = controllerObj.AddComponent<LaneBattleController>();

        GameObject p1Obj = new GameObject("P1_Unit");
        CharacterCombat p1Unit = p1Obj.AddComponent<CharacterCombat>();
        p1Unit.InitializeStats(maxHealth: 5, attackPower: 10); // Mucho ataque

        GameObject p2Obj = new GameObject("P2_Unit");
        CharacterCombat p2Unit = p2Obj.AddComponent<CharacterCombat>();
        p2Unit.InitializeStats(maxHealth: 8, attackPower: 2); // Poca vida para el ataque de P1

        // 2. ACT (Actuar)
        laneController.ResolveLaneCombat(p1Unit, p2Unit);

        // 3. ASSERT (Comprobar)
        // P1 recibe 2 de daño (5 - 2 = 3)
        Assert.AreEqual(3, p1Unit.CurrentHealth);
        Assert.IsFalse(p1Unit.IsDead);

        // P2 recibe 10 de daño (8 - 10 = -2 -> se ajusta a 0) y muere
        Assert.AreEqual(0, p2Unit.CurrentHealth);
        Assert.IsTrue(p2Unit.IsDead);

        // Limpieza
        Object.DestroyImmediate(controllerObj);
        Object.DestroyImmediate(p1Obj);
        Object.DestroyImmediate(p2Obj);
    }
}