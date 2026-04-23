using UnityEngine;
using System.Collections.Generic;

public class VisualController : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject prefabSolar;
    public GameObject prefabVoid;

    // En TFT definimos un área general en lugar de carriles estrictos
    public Transform p0BoardArea;
    public Transform p1BoardArea;

    private Dictionary<Unit, GameObject> _visualCubes = new Dictionary<Unit, GameObject>();

    private void OnEnable()
    {
        gameManager.OnUnitSpawned += HandleUnitSpawned;
        gameManager.OnUnitDied += HandleUnitDied;
    }

    private void OnDisable()
    {
        gameManager.OnUnitSpawned -= HandleUnitSpawned;
        gameManager.OnUnitDied -= HandleUnitDied;
    }

    private void HandleUnitSpawned(int playerId, Unit unitData)
    {
        Transform baseArea = (playerId == 0) ? p0BoardArea : p1BoardArea;

        // Asignamos una posición con un pequeño desplazamiento aleatorio en el área
        Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.0f, 1.0f));
        Vector3 spawnPos = baseArea.position + randomOffset;

        GameObject prefabToUse = (playerId == 0) ? prefabSolar : prefabVoid;
        GameObject newCube = Instantiate(prefabToUse, spawnPos, baseArea.rotation);

        _visualCubes[unitData] = newCube;
        Debug.Log($"Visual: Cubo TFT instanciado para jugador {playerId}.");
    }

    private void HandleUnitDied(int playerId, Unit unit)
    {
        if (_visualCubes.ContainsKey(unit))
        {
            Destroy(_visualCubes[unit]);
            _visualCubes.Remove(unit);
            Debug.Log($"Visual: Cubo destruido para jugador {playerId}.");
        }
    }
}