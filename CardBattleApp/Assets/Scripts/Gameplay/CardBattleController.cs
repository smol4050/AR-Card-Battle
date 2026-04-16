using UnityEngine;

public class CardBattleController : MonoBehaviour
{
    private ARCardDistanceScanner _distanceScanner;

    // Inicializamos el controlador inyectando el escáner de distancias
    public void Initialize(ARCardDistanceScanner scanner)
    {
        _distanceScanner = scanner;
    }

    /// <summary>
    /// Intenta ejecutar un ataque. Retorna true si el ataque fue exitoso.
    /// </summary>
    public bool TryExecuteAttack(CharacterCombat attacker, CharacterCombat defender)
    {
        // Validamos que ninguno sea nulo y que ambos sigan vivos
        if (attacker == null || defender == null || attacker.IsDead || defender.IsDead)
        {
            return false;
        }

        // Usamos el escáner para ver si están a distancia de combate
        if (_distanceScanner.AreCardsCloseEnough(attacker.transform, defender.transform))
        {
            defender.TakeDamage(attacker.AttackPower);
            return true;
        }

        return false;
    }
}