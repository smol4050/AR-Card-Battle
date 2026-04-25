using UnityEngine;

/// <summary>
/// Contenedor de todas las referencias del tablero instanciado dinámicamente en AR.
/// Este componente DEBE estar en el prefab del tablero.
/// </summary>
public class BattlefieldReferences : MonoBehaviour
{
    [Header("Áreas base (rotación/orientación)")]
    public Transform p0BoardArea;
    public Transform p1BoardArea;

    [Header("Slots Jugador 0 — Sollar")]
    public Transform[] p0FrontSlots = new Transform[3];
    public Transform[] p0BackSlots = new Transform[3];

    [Header("Slots Jugador 1 — Void")]
    public Transform[] p1FrontSlots = new Transform[3];
    public Transform[] p1BackSlots = new Transform[3];

    [Header("Naves — Jugador 0")]
    public Transform p0ShipEntryPoint;       // Punto de entrada de la nave (fuera del tablero)
    public Transform p0ShipBoardPoint;       // Punto de patrulla principal sobre el tablero
    public Transform p0ShipConvergencePoint; // Punto donde convergen los proyectiles antes de disparar
    public Transform[] p0ShipPatrolPoints;   // Puntos de patrulla adicionales

    [Header("Naves — Jugador 1")]
    public Transform p1ShipEntryPoint;
    public Transform p1ShipBoardPoint;
    public Transform p1ShipConvergencePoint;
    public Transform[] p1ShipPatrolPoints;

    // ─── VALIDACIÓN ───────────────────────────────────────────────────────────
    private void Awake()
    {
        bool valid = true;

        if (p0BoardArea == null) { Debug.LogError("[BattlefieldReferences] p0BoardArea no asignado."); valid = false; }
        if (p1BoardArea == null) { Debug.LogError("[BattlefieldReferences] p1BoardArea no asignado."); valid = false; }

        for (int i = 0; i < 3; i++)
        {
            if (p0FrontSlots.Length <= i || p0FrontSlots[i] == null)
            { Debug.LogError($"[BattlefieldReferences] p0FrontSlots[{i}] no asignado."); valid = false; }
            if (p0BackSlots.Length <= i || p0BackSlots[i] == null)
            { Debug.LogError($"[BattlefieldReferences] p0BackSlots[{i}] no asignado."); valid = false; }
            if (p1FrontSlots.Length <= i || p1FrontSlots[i] == null)
            { Debug.LogError($"[BattlefieldReferences] p1FrontSlots[{i}] no asignado."); valid = false; }
            if (p1BackSlots.Length <= i || p1BackSlots[i] == null)
            { Debug.LogError($"[BattlefieldReferences] p1BackSlots[{i}] no asignado."); valid = false; }
        }

        if (valid)
            Debug.Log("<color=green>[BattlefieldReferences] Todas las referencias validadas correctamente.</color>");
        else
            Debug.LogError("[BattlefieldReferences] Faltan referencias. Revisa el prefab del tablero en el Inspector.");
    }

    /// <summary>Devuelve los slots de frontline del jugador indicado.</summary>
    public Transform[] GetFrontSlots(int playerId) => playerId == 0 ? p0FrontSlots : p1FrontSlots;

    /// <summary>Devuelve los slots de backline del jugador indicado.</summary>
    public Transform[] GetBackSlots(int playerId) => playerId == 0 ? p0BackSlots : p1BackSlots;

    /// <summary>Devuelve el Transform de un slot concreto.</summary>
    public Transform GetSlot(int playerId, int row, int slotIndex)
    {
        Transform[] front = GetFrontSlots(playerId);
        Transform[] back = GetBackSlots(playerId);

        if (row == 0 && slotIndex < front.Length) return front[slotIndex];
        if (row == 1 && slotIndex - 3 >= 0 && slotIndex - 3 < back.Length) return back[slotIndex - 3];

        Debug.LogWarning($"[BattlefieldReferences] Slot inválido: playerId={playerId} row={row} slotIndex={slotIndex}");
        return null;
    }
}