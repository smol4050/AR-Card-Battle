using NUnit.Framework;
using UnityEngine;

public class GameManagerTests
{
    [Test]
    public void PlayCard_WithEnoughEnergyAndEmptyLane_SpawnsUnitAndConsumesEnergy()
    {
        // 1. ARRANGE (Preparar)
        // Creamos nuestro entorno aislado
        GameObject go = new GameObject();
        GameManager gm = go.AddComponent<GameManager>();
        gm.InitializeGame();

        // Le damos energía al jugador 0
        gm.players[0].maxEnergy = 3;
        gm.players[0].energy = 3;

        // Bandera para verificar que el evento se disparó
        bool eventFired = false;
        gm.OnUnitSpawned += (playerId, lane, unit) => { eventFired = true; };

        // Parámetros de la carta
        int targetPlayer = 0;
        int targetLane = 1;
        int cardHealth = 5;
        int cardAttack = 2;
        int energyCost = 2;

        // 2. ACT (Actuar)
        // Ejecutamos el método API para colocar la carta
        bool success = gm.PlayCard(targetPlayer, targetLane, cardHealth, cardAttack, energyCost);

        // 3. ASSERT (Comprobar)
        // Verificamos que el método devolvió true
        Assert.IsTrue(success);

        // Verificamos que la energía se redujo (3 - 2 = 1)
        Assert.AreEqual(1, gm.players[0].energy);

        // Verificamos que la unidad está lógicamente en el tablero
        Assert.IsNotNull(gm.board[0, 1]);
        Assert.AreEqual(5, gm.board[0, 1].health);

        // Verificamos que el evento para la capa visual se emitió correctamente
        Assert.IsTrue(eventFired);

        // Limpieza
        Object.DestroyImmediate(go);
    }
}