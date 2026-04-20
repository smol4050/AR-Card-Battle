using UnityEngine;

public class VisualController : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject cubePrefab;
    public Transform[] p0Lanes;
    public Transform[] p1Lanes;

    private GameObject[,] _visualCubes = new GameObject[2, 3];

    private void OnEnable()
    {
        // Nos suscribimos a los eventos de lógica
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
        GameObject newCube = Instantiate(cubePrefab, spawnPos.position, spawnPos.rotation);

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