using UnityEngine;

/// <summary>
/// Contenedor de datos que se ancla al Prefab del tablero instanciado dinámicamente.
/// </summary>
public class BattlefieldReferences : MonoBehaviour
{
    [Header("Áreas Base")]
    public Transform p0BoardArea;
    public Transform p1BoardArea;

    [Header("Slots P0")]
    public Transform[] p0FrontSlots = new Transform[3];
    public Transform[] p0BackSlots = new Transform[3];

    [Header("Slots P1")]
    public Transform[] p1FrontSlots = new Transform[3];
    public Transform[] p1BackSlots = new Transform[3];

    [Header("Posiciones de spawn de nave")]
    public Transform p0ShipSpawnPoint;
    public Transform p1ShipSpawnPoint;

    [Header("Naves - Setup P0")]
    public Transform p0ShipEntryPoint;
    public Transform p0ShipBoardPoint;
    public Transform p0ShipConvergencePoint;
    public Transform[] p0ShipPatrolPoints;

    [Header("Naves - Setup P1")]
    public Transform p1ShipEntryPoint;
    public Transform p1ShipBoardPoint;
    public Transform p1ShipConvergencePoint;
    public Transform[] p1ShipPatrolPoints;
}