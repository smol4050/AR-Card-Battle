using UnityEngine;

/// <summary>
/// Componente para identificar las casillas del tablero en 3D.
/// </summary>
public class SlotCollider : MonoBehaviour
{
    [Tooltip("0 para la fila frontal (Frontline), 1 para la trasera (Backline)")]
    public int row;

    [Tooltip("Índice exacto del slot (0 a 5)")]
    public int slotIndex;
}