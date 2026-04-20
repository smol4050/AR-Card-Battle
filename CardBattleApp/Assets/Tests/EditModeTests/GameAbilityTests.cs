using NUnit.Framework;
using UnityEngine;

public class GameAbilityTests
{
    [Test]
    public void ActivateCard_WithValidUnitAndEnergy_IncreasesStatsAndConsumesEnergy()
    {
        // 1. ARRANGE (Preparar)
        // Creamos el entorno del GameManager aislado de la escena
        GameObject go = new GameObject();
        GameManager gm = go.AddComponent<GameManager>();
        gm.InitializeGame();

        // Configuramos al Jugador 0 con energía suficiente
        gm.players[0].maxEnergy = 5;
        gm.players[0].energy = 5;

        // Spawneamos una unidad base directamente usando la API (Costo: 0 para no afectar el test)
        int playerId = 0;
        int targetLane = 1;
        gm.PlayCard(playerId, targetLane, cardHealth: 10, cardAttack: 5, energyCost: 0);

        // Variables para interceptar el evento
        bool wasBuffEventFired = false;
        int buffAppliedToLane = -1;

        gm.OnUnitBuffed += (pId, lane, amount) =>
        {
            wasBuffEventFired = true;
            buffAppliedToLane = lane;
        };

        // 2. ACT (Actuar)
        // El jugador intenta usar la habilidad "Awakening" que cuesta 3 de energía y da +4 de stats
        int abilityCost = 3;
        int awakeningBuffAmount = 4;
        bool success = gm.ActivateCard(playerId, targetLane, abilityCost, awakeningBuffAmount);

        // 3. ASSERT (Comprobar)
        // Verificamos que la acción fue validada como correcta
        Assert.IsTrue(success);

        // Comprobamos que la energía se redujo correctamente (5 - 3 = 2)
        Assert.AreEqual(2, gm.players[0].energy);

        // Verificamos que los stats de la unidad objetivo en el tablero aumentaron (Salud: 10+4, Ataque: 5+4)
        Unit buffedUnit = gm.board[playerId, targetLane];
        Assert.AreEqual(14, buffedUnit.health);
        Assert.AreEqual(9, buffedUnit.attack);

        // Verificamos que el evento se disparó para que el Frontend (VisualController/UI) reaccione
        Assert.IsTrue(wasBuffEventFired);
        Assert.AreEqual(targetLane, buffAppliedToLane);

        // Limpieza de memoria
        Object.DestroyImmediate(go);
    }
}