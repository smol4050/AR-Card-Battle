public static class GameplayRules
{
    /// <summary>
    /// Valida si un jugador puede colocar una carta (cubo) en el tablero.
    /// </summary>
    public static bool CanPlayCard(int requestingPlayerIndex, int currentTurnPlayerIndex, int playerEnergy, CharacterCombat existingUnitInLane)
    {
        // 1. ¿Es el turno del jugador?
        if (requestingPlayerIndex != currentTurnPlayerIndex)
        {
            return false;
        }

        // 2. ¿Tiene al menos 1 de energía?
        if (playerEnergy < 1)
        {
            return false;
        }

        // 3. ¿El carril está libre?
        if (existingUnitInLane != null)
        {
            return false;
        }

        return true;
    }
}