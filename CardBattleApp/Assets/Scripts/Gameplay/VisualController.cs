using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VisualController : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject prefabSolar;
    public GameObject prefabVoid;

    public Transform p0BoardArea;
    public Transform p1BoardArea;

    private Dictionary<Unit, GameObject> _visualCubes = new Dictionary<Unit, GameObject>();

    private void OnEnable()
    {
        gameManager.OnUnitSpawned += HandleUnitSpawned;
        gameManager.OnUnitDied += HandleUnitDied;
        gameManager.OnUnitAttacked += HandleUnitAttacked;
    }

    private void OnDisable()
    {
        gameManager.OnUnitSpawned -= HandleUnitSpawned;
        gameManager.OnUnitDied -= HandleUnitDied;
        gameManager.OnUnitAttacked -= HandleUnitAttacked;
    }

    private void HandleUnitSpawned(int playerId, Unit unitData)
    {
        Transform baseArea = (playerId == 0) ? p0BoardArea : p1BoardArea;

        // Espaciamos los cubos en la zona de juego de forma determinista usando su posición lógica
        Vector3 spawnPos = baseArea.position + new Vector3(unitData.logicalPosition.x, 0, unitData.logicalPosition.y);

        GameObject prefabToUse = (playerId == 0) ? prefabSolar : prefabVoid;
        GameObject newCube = Instantiate(prefabToUse, spawnPos, baseArea.rotation);

        _visualCubes[unitData] = newCube;
    }

    private void HandleUnitDied(int playerId, Unit unit)
    {
        if (_visualCubes.TryGetValue(unit, out GameObject cubeToDestroy))
        {
            Destroy(cubeToDestroy);
            _visualCubes.Remove(unit);
            Debug.Log($"Visual: Cubo destruido físicamente en la escena.");
        }
    }

    // Actualizado a FLOAT para encajar con el nuevo motor
    private void HandleUnitAttacked(Unit attacker, Unit defender, float damageReal)
    {
        if (_visualCubes.TryGetValue(attacker, out GameObject attackerObj) &&
            _visualCubes.TryGetValue(defender, out GameObject defenderObj))
        {
            StartCoroutine(AnimateAttackBump(attackerObj.transform, defenderObj.transform.position));
        }
    }

    private IEnumerator AnimateAttackBump(Transform attackerTransform, Vector3 targetPosition)
    {
        // Guardamos la posición inicial para que el cubo sepa a dónde regresar
        Vector3 originalPosition = attackerTransform.position;
        Vector3 attackPosition = Vector3.Lerp(originalPosition, targetPosition, 0.4f);

        float attackSpeed = 0.08f;
        float returnSpeed = 0.12f;
        float timer = 0;

        // Embiste
        while (timer < attackSpeed)
        {
            timer += Time.deltaTime;
            attackerTransform.position = Vector3.Lerp(originalPosition, attackPosition, timer / attackSpeed);
            yield return null;
        }

        timer = 0;

        // Regresa
        while (timer < returnSpeed)
        {
            timer += Time.deltaTime;
            attackerTransform.position = Vector3.Lerp(attackPosition, originalPosition, timer / returnSpeed);
            yield return null;
        }

        attackerTransform.position = originalPosition;
    }
}