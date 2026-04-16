using UnityEngine;

public class ARCardDistanceScanner : MonoBehaviour
{
    // Distancia mínima requerida para que empiece la batalla (en metros de Unity)
    public float BattleTriggerDistance { get; private set; } = 0.5f;

    /// <summary>
    /// Calcula la distancia entre dos transformaciones (las dos cartas físicas detectadas)
    /// </summary>
    public float CalculateDistance(Transform cardA, Transform cardB)
    {
        if (cardA == null || cardB == null)
        {
            return -1f; // Retorna -1 si falta alguna carta, indicando error o ausencia
        }

        return Vector3.Distance(cardA.position, cardB.position);
    }

    /// <summary>
    /// Verifica si la distancia es lo suficientemente corta para iniciar el combate
    /// </summary>
    public bool AreCardsCloseEnough(Transform cardA, Transform cardB)
    {
        float distance = CalculateDistance(cardA, cardB);

        // Si la distancia es válida (no es -1) y es menor o igual al límite
        if (distance >= 0f && distance <= BattleTriggerDistance)
        {
            return true;
        }

        return false;
    }
}