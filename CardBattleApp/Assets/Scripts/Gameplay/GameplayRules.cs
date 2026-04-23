public static class GameplayRules
{
    /// <summary>
    /// Valida si un jugador tiene permitido desplegar una unidad al tablero TFT.
    /// </summary>
    public static bool CanPlayCard(RoundPhase currentPhase, int playerEnergy, int cardCost)
    {
        // 1. ¿Estamos en la fase correcta?
        if (currentPhase != RoundPhase.Preparation)
        {
            return false;
        }

        // 2. ¿Tiene la energía suficiente?
        if (playerEnergy < cardCost)
        {
            return false;
        }

        return true;
    }
}