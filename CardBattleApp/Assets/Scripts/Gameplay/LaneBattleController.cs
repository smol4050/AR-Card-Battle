using UnityEngine;

// Reemplazamos CardBattleController para adaptarlo a la lógica de Carriles (Lanes) sin AR
public class LaneBattleController : MonoBehaviour
{
    /// <summary>
    /// Resuelve el combate en un carril específico. 
    /// En Photon PUN 2, esto SOLO lo llama el MasterClient al terminar el turno.
    /// </summary>
    public void ResolveLaneCombat(CharacterCombat player1Unit, CharacterCombat player2Unit)
    {
        // Caso 1: Hay unidades de ambos jugadores en el carril (Combate mutuo)
        if (player1Unit != null && player2Unit != null && !player1Unit.IsDead && !player2Unit.IsDead)
        {
            // Ambos se hacen daño simultáneamente
            player1Unit.TakeDamage(player2Unit.AttackPower);
            player2Unit.TakeDamage(player1Unit.AttackPower);
        }
        // Caso 2: Solo hay unidad del Jugador 1 (Ataca directo al jugador 2)
        else if (player1Unit != null && !player1Unit.IsDead && (player2Unit == null || player2Unit.IsDead))
        {
            Debug.Log($"Jugador 2 recibe {player1Unit.AttackPower} de daño directo.");
            // Aquí llamarías a la función para dañar al Player 2 (Ej: PlayerManager.TakeDamage)
        }
        // Caso 3: Solo hay unidad del Jugador 2 (Ataca directo al jugador 1)
        else if (player2Unit != null && !player2Unit.IsDead && (player1Unit == null || player1Unit.IsDead))
        {
            Debug.Log($"Jugador 1 recibe {player2Unit.AttackPower} de daño directo.");
            // Aquí llamarías a la función para dañar al Player 1
        }
    }
}