using UnityEngine;

public class VisualController : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject prefabSolar; // Cubo Azul/Blanco (Jugador 0)
    public GameObject prefabVoid;  // Cubo Morado/Oscuro (Jugador 1 - IA)

    public Transform[] p0Lanes;
    public Transform[] p1Lanes;

    private GameObject[,] _visualCubes = new GameObject[2, 3];

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

    private void HandleUnitSpawned(int playerId, int lane, Unit unitData)
    {
        Transform spawnPos = (playerId == 0) ? p0Lanes[lane] : p1Lanes[lane];

        // Seleccionamos el prefab según el jugador
        GameObject prefabToUse = (playerId == 0) ? prefabSolar : prefabVoid;
        GameObject newCube = Instantiate(prefabToUse, spawnPos.position, spawnPos.rotation);

        _visualCubes[playerId, lane] = newCube;
        Debug.Log($"Visual: Cubo instanciado para jugador {playerId} en carril {lane}.");
    }

    private void HandleUnitDied(int playerId, int lane)
    {
        if (_visualCubes[playerId, lane] != null)
        {
            Destroy(_visualCubes[playerId, lane]);
            _visualCubes[playerId, lane] = null;
            Debug.Log($"Visual: Cubo destruido para jugador {playerId} en carril {lane}.");
        }
    }
}