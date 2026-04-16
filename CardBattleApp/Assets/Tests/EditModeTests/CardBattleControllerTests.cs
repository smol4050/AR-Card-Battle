using NUnit.Framework;
using UnityEngine;

public class CardBattleControllerTests
{
    [Test]
    public void TryExecuteAttack_WhenCardsAreClose_ReducesDefenderHealth()
    {
        // Arrange (Preparar)
        // 1. Instanciamos los sistemas
        GameObject controllerObj = new GameObject("Controller");
        CardBattleController battleController = controllerObj.AddComponent<CardBattleController>();

        GameObject scannerObj = new GameObject("Scanner");
        ARCardDistanceScanner scanner = scannerObj.AddComponent<ARCardDistanceScanner>();
        battleController.Initialize(scanner);

        // 2. Creamos y configuramos a los peleadores (muy cerca uno del otro)
        GameObject attackerObj = new GameObject("Attacker");
        attackerObj.transform.position = Vector3.zero;
        CharacterCombat attacker = attackerObj.AddComponent<CharacterCombat>();
        attacker.InitializeStats(100, 25); // Ataca con 25 de daño

        GameObject defenderObj = new GameObject("Defender");
        defenderObj.transform.position = new Vector3(0.2f, 0, 0); // Distancia de 0.2m (¡Están cerca!)
        CharacterCombat defender = defenderObj.AddComponent<CharacterCombat>();
        defender.InitializeStats(100, 10); // Empieza con 100 de vida

        // Act (Actuar)
        bool attackSuccess = battleController.TryExecuteAttack(attacker, defender);

        // Assert (Comprobar)
        Assert.IsTrue(attackSuccess);
        Assert.AreEqual(75, defender.CurrentHealth); // 100 - 25 = 75
    }

    [Test]
    public void TryExecuteAttack_WhenCardsAreFar_DoesNotReduceHealth()
    {
        // Arrange (Preparar)
        GameObject controllerObj = new GameObject("Controller");
        CardBattleController battleController = controllerObj.AddComponent<CardBattleController>();

        GameObject scannerObj = new GameObject("Scanner");
        ARCardDistanceScanner scanner = scannerObj.AddComponent<ARCardDistanceScanner>();
        battleController.Initialize(scanner);

        // Creamos a los peleadores pero los alejamos
        GameObject attackerObj = new GameObject("Attacker");
        attackerObj.transform.position = Vector3.zero;
        CharacterCombat attacker = attackerObj.AddComponent<CharacterCombat>();
        attacker.InitializeStats(100, 25);

        GameObject defenderObj = new GameObject("Defender");
        defenderObj.transform.position = new Vector3(3.0f, 0, 0); // Distancia de 3 metros (¡Están lejos!)
        CharacterCombat defender = defenderObj.AddComponent<CharacterCombat>();
        defender.InitializeStats(100, 10);

        // Act (Actuar)
        bool attackSuccess = battleController.TryExecuteAttack(attacker, defender);

        // Assert (Comprobar)
        Assert.IsFalse(attackSuccess);
        Assert.AreEqual(100, defender.CurrentHealth); // La vida debe seguir intacta
    }
}