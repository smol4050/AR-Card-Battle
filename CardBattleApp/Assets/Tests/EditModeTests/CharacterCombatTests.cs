using NUnit.Framework;
using UnityEngine;

public class CharacterCombatTests
{
    [Test]
    public void TakeDamage_ReducesCurrentHealth_Correctly()
    {
        // Arrange (Preparar)
        GameObject go = new GameObject();
        CharacterCombat combatStats = go.AddComponent<CharacterCombat>();
        combatStats.InitializeStats(100, 20); // 100 de vida, 20 de ataque

        // Act (Actuar)
        combatStats.TakeDamage(30); // Recibe 30 de daño

        // Assert (Comprobar)
        Assert.AreEqual(70, combatStats.CurrentHealth);
        Assert.IsFalse(combatStats.IsDead);
    }

    [Test]
    public void TakeDamage_WhenDamageExceedsHealth_SetsIsDeadToTrue()
    {
        // Arrange (Preparar)
        GameObject go = new GameObject();
        CharacterCombat combatStats = go.AddComponent<CharacterCombat>();
        combatStats.InitializeStats(50, 10); // 50 de vida

        // Act (Actuar)
        combatStats.TakeDamage(60); // Recibe un daño mortal de 60

        // Assert (Comprobar)
        Assert.AreEqual(0, combatStats.CurrentHealth);
        Assert.IsTrue(combatStats.IsDead);
    }
}