using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VisualController : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject prefabSolar;
    public GameObject prefabVoid;
    public GameObject prefabVoidBullet;

    public Transform p0BoardArea;
    public Transform p1BoardArea;

    private Dictionary<Unit, GameObject> _visualCubes = new Dictionary<Unit, GameObject>();

    private void OnEnable()
    {
        gameManager.OnUnitSpawned += HandleUnitSpawned;
        gameManager.OnUnitDied += HandleUnitDied;
        gameManager.OnUnitAttacked += HandleUnitAttacked;
        gameManager.OnUnitSkillCast += HandleUnitSkillCast;
    }

    private void OnDisable()
    {
        gameManager.OnUnitSpawned -= HandleUnitSpawned;
        gameManager.OnUnitDied -= HandleUnitDied;
        gameManager.OnUnitAttacked -= HandleUnitAttacked;
        gameManager.OnUnitSkillCast -= HandleUnitSkillCast;
    }

    private void HandleUnitSpawned(int playerId, Unit unitData)
    {
        Transform baseArea = (playerId == 0) ? p0BoardArea : p1BoardArea;
        Vector3 spawnPos = baseArea.position + new Vector3(unitData.logicalPosition.x, 0, unitData.logicalPosition.y);

        GameObject prefabToUse = (playerId == 0) ? prefabSolar : prefabVoid;
        GameObject newCube = Instantiate(prefabToUse, spawnPos, baseArea.rotation);

        UnitWorldUI uiComponent = newCube.GetComponent<UnitWorldUI>();
        if (uiComponent != null)
        {
            uiComponent.Initialize(unitData);
        }

        _visualCubes[unitData] = newCube;
    }

    private void HandleUnitDied(int playerId, Unit unit)
    {
        if (_visualCubes.TryGetValue(unit, out GameObject cubeToDestroy))
        {
            Destroy(cubeToDestroy);
            _visualCubes.Remove(unit);
        }
    }

    private void HandleUnitAttacked(Unit attacker, Unit defender, float damageReal)
    {
        if (_visualCubes.TryGetValue(attacker, out GameObject attackerObj) &&
            _visualCubes.TryGetValue(defender, out GameObject defenderObj))
        {
            // Verificamos rango usando solo nuestras cartas base
            bool isRanged = attacker.cardId == CardID.VoidHorde ||
                            attacker.cardId == CardID.VoidHeavyShooter ||
                            attacker.cardId == CardID.SollarCommander;

            if (isRanged && prefabVoidBullet != null)
            {
                StartCoroutine(AnimateProjectile(attackerObj.transform.position, defenderObj.transform.position));
            }
            else
            {
                StartCoroutine(AnimateAttackBump(attackerObj.transform, defenderObj.transform.position));
            }
        }
    }

    private void HandleUnitSkillCast(Unit caster, CardID skillId)
    {
        if (_visualCubes.TryGetValue(caster, out GameObject casterObj))
        {
            switch (skillId)
            {
                case CardID.SollarDuelist:
                    StartCoroutine(AnimateSpin(casterObj.transform));
                    break;
                case CardID.VoidHeavyShooter:
                    StartCoroutine(AnimateShake(casterObj.transform));
                    break;
                case CardID.SollarCommander:
                case CardID.VoidCommander:
                case CardID.SollarForce:
                    StartCoroutine(AnimatePulse(casterObj.transform));
                    break;
            }
        }
    }

    // --- ANIMACIONES VISUALES ---

    private IEnumerator AnimateProjectile(Vector3 startPos, Vector3 targetPos)
    {
        GameObject bullet = Instantiate(prefabVoidBullet, startPos, Quaternion.identity);
        float duration = 0.15f;
        float timer = 0;

        startPos.y += 0.5f;
        targetPos.y += 0.5f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(startPos, targetPos, timer / duration);
            yield return null;
        }

        Destroy(bullet);
    }

    private IEnumerator AnimateAttackBump(Transform attackerTransform, Vector3 targetPosition)
    {
        Vector3 originalPosition = attackerTransform.position;
        Vector3 attackPosition = Vector3.Lerp(originalPosition, targetPosition, 0.4f);

        float attackSpeed = 0.08f;
        float returnSpeed = 0.12f;
        float timer = 0;

        while (timer < attackSpeed)
        {
            timer += Time.deltaTime;
            attackerTransform.position = Vector3.Lerp(originalPosition, attackPosition, timer / attackSpeed);
            yield return null;
        }
        timer = 0;
        while (timer < returnSpeed)
        {
            timer += Time.deltaTime;
            attackerTransform.position = Vector3.Lerp(attackPosition, originalPosition, timer / returnSpeed);
            yield return null;
        }
        attackerTransform.position = originalPosition;
    }

    private IEnumerator AnimateSpin(Transform t)
    {
        float duration = 0.4f;
        float timer = 0;
        Vector3 startRot = t.eulerAngles;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float yRot = Mathf.Lerp(0, 360, timer / duration);
            t.eulerAngles = new Vector3(startRot.x, startRot.y + yRot, startRot.z);
            yield return null;
        }
        t.eulerAngles = startRot;
    }

    private IEnumerator AnimatePulse(Transform t)
    {
        Vector3 origScale = t.localScale;
        Vector3 bigScale = origScale * 1.5f;
        float halfDuration = 0.2f;

        float timer = 0;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            t.localScale = Vector3.Lerp(origScale, bigScale, timer / halfDuration);
            yield return null;
        }
        timer = 0;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            t.localScale = Vector3.Lerp(bigScale, origScale, timer / halfDuration);
            yield return null;
        }
        t.localScale = origScale;
    }

    private IEnumerator AnimateShake(Transform t)
    {
        Vector3 origPos = t.position;
        float duration = 0.5f;
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float offsetX = Random.Range(-0.2f, 0.2f);
            float offsetZ = Random.Range(-0.2f, 0.2f);
            t.position = origPos + new Vector3(offsetX, 0, offsetZ);
            yield return null;
        }
        t.position = origPos;
    }
}