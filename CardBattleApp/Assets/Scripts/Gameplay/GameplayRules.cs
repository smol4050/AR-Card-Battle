/// <summary>
/// Reglas de juego centralizadas y sin estado.
/// Todas las validaciones de "¿puedo hacer X?" viven aquí.
/// </summary>
public static class GameplayRules
{
    /// <summary>
    /// Valida si un jugador puede desplegar una carta (unidad o nave).
    /// </summary>
    public static bool CanPlayCard(RoundPhase currentPhase, int playerEnergy, int cardCost)
    {
        // Las naves se juegan en combate; las unidades en preparación.
        // La fase exacta la valida el GameManager; aquí solo chequeamos energía.
        if (playerEnergy < cardCost) return false;
        return true;
    }

    /// <summary>
    /// Valida si un slot está libre para una unidad normal (no horde, no nave).
    /// </summary>
    public static bool IsSlotFree(System.Collections.Generic.List<Unit> teamUnits, int slotIndex)
    {
        return !teamUnits.Exists(u => u.slotIndex == slotIndex && !u.IsDead);
    }

    /// <summary>
    /// Valida la regla de Commander único por bando.
    /// </summary>
    public static bool CanDeployCommander(System.Collections.Generic.List<Unit> teamUnits)
    {
        return !teamUnits.Exists(
            u => (u.cardId == CardID.SollarCommander || u.cardId == CardID.VoidCommander) && !u.IsDead);
    }
}