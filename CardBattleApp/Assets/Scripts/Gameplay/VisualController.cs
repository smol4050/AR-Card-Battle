using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VisualController : MonoBehaviour
{
    public GameManager gameManager;

    [Header("Sollar Prefabs")]
    public GameObject prefabSollarDuelist;
    public GameObject prefabSollarForce;
    public GameObject prefabSollarCommander;

    [Header("Void Prefabs")]
    public GameObject prefabVoidHorde;
    public GameObject prefabVoidCommander;
    public GameObject prefabVoidHeavyShooter;

    [Header("VFX Prefabs")]
    public GameObject prefabVoidBullet;
    public GameObject prefabForceImpact; // Partículas para la Fuerza

    [Header("Áreas Base (Para UI/Rotación General)")]
    public Transform p0BoardArea;
    public Transform p1BoardArea;

    [Header("Posiciones Reales del Tablero P0 (Local)")]
    public Transform[] p0FrontSlots = new Transform[3];
    public Transform[] p0BackSlots = new Transform[3];

    [Header("Posiciones Reales del Tablero P1 (IA)")]
    public Transform[] p1FrontSlots = new Transform[3];
    public Transform[] p1BackSlots = new Transform[3];

    private Dictionary<Unit, GameObject> _visualUnits = new Dictionary<Unit, GameObject>();

    private void OnEnable()
    {
        gameManager.OnUnitSpawned += HandleUnitSpawned;
        gameManager.OnUnitDied += HandleUnitDied;
        gameManager.OnUnitAttacked += HandleUnitAttacked;
        gameManager.OnUnitSkillCast += HandleUnitSkillCast;
        gameManager.OnVisualEffectRequested += HandleVisualEffectRequested;
    }

    private void OnDisable()
    {
        gameManager.OnUnitSpawned -= HandleUnitSpawned;
        gameManager.OnUnitDied -= HandleUnitDied;
        gameManager.OnUnitAttacked -= HandleUnitAttacked;
        gameManager.OnUnitSkillCast -= HandleUnitSkillCast;
        gameManager.OnVisualEffectRequested -= HandleVisualEffectRequested;
    }

    private void HandleUnitSpawned(int playerId, Unit unitData)
    {
        GameObject prefabToUse = GetPrefabByCardId(unitData.cardId);
        if (prefabToUse == null) return;

        Vector3 exactWorldPos = Vector3.zero;

        if (playerId == 0)
        {
            Transform targetSlot = (unitData.row == 0) ? p0FrontSlots[unitData.slotIndex] : p0BackSlots[unitData.slotIndex - 3];
            BoxCollider box = targetSlot.GetComponent<BoxCollider>();
            exactWorldPos = (box != null) ? targetSlot.TransformPoint(box.center) : targetSlot.position;
        }
        else
        {
            Transform targetSlot = (unitData.row == 0) ? p1FrontSlots[unitData.slotIndex] : p1BackSlots[unitData.slotIndex - 3];
            BoxCollider box = targetSlot.GetComponent<BoxCollider>();
            exactWorldPos = (box != null) ? targetSlot.TransformPoint(box.center) : targetSlot.position;
        }

        Transform baseRotArea = (playerId == 0) ? p0BoardArea : p1BoardArea;
        GameObject newUnit = Instantiate(prefabToUse, exactWorldPos, baseRotArea.rotation);

        UnitWorldUI uiComponent = newUnit.GetComponent<UnitWorldUI>();
        if (uiComponent != null) uiComponent.Initialize(unitData);

        _visualUnits[unitData] = newUnit;

        // Sincronización lógica inicial
        Vector3 offset = exactWorldPos - baseRotArea.position;
        unitData.logicalPosition = new Vector2(offset.x, offset.z);
    }

    private GameObject GetPrefabByCardId(CardID id)
    {
        switch (id)
        {
            case CardID.SollarDuelist: return prefabSollarDuelist;
            case CardID.SollarForce: return prefabSollarForce;
            case CardID.SollarCommander: return prefabSollarCommander;
            case CardID.VoidHorde: return prefabVoidHorde;
            case CardID.VoidCommander: return prefabVoidCommander;
            case CardID.VoidHeavyShooter: return prefabVoidHeavyShooter;
            default: return null;
        }
    }

    private void HandleUnitDied(int playerId, Unit unit)
    {
        if (_visualUnits.TryGetValue(unit, out GameObject objToDestroy))
        {
            Destroy(objToDestroy);
            _visualUnits.Remove(unit);
        }
    }

    private void HandleVisualEffectRequested(Unit caster, Unit target, string effectName)
    {
        if (target != null && _visualUnits.TryGetValue(target, out GameObject targetObj))
        {
            if (effectName == "ForceHit" && prefabForceImpact != null)
            {
                Vector3 impactPos = targetObj.transform.position + Vector3.up;
                Instantiate(prefabForceImpact, impactPos, Quaternion.identity);
            }
        }
    }

    private void HandleUnitAttacked(Unit attacker, Unit defender, float damageReal)
    {
        if (_visualUnits.TryGetValue(attacker, out GameObject attackerObj) &&
            _visualUnits.TryGetValue(defender, out GameObject defenderObj))
        {
            Vector3 targetPos = defenderObj.transform.position;
            Vector3 lookPosition = new Vector3(targetPos.x, attackerObj.transform.position.y, targetPos.z);
            attackerObj.transform.LookAt(lookPosition);

            bool isWeaponRanged = attacker.cardId == CardID.VoidHorde ||
                                  attacker.cardId == CardID.VoidHeavyShooter ||
                                  attacker.cardId == CardID.SollarCommander;

            bool isForceUser = attacker.cardId == CardID.SollarForce;

            float distanceToTarget = Vector3.Distance(attackerObj.transform.position, targetPos);
            float maxRangedDistance = 4.5f;

            if (isWeaponRanged || isForceUser)
            {
                if (distanceToTarget > maxRangedDistance)
                {
                    StartCoroutine(AnimateWalkToRangeAndAttack(attacker, attackerObj, defenderObj, maxRangedDistance, isWeaponRanged));
                }
                else
                {
                    ExecuteRangedVisuals(attackerObj, defenderObj, isWeaponRanged);
                }
            }
            else
            {
                StartCoroutine(AnimateMeleeWalkAndStrike(attacker, attackerObj.transform, defenderObj.transform.position));
            }
        }
    }

    private void HandleUnitSkillCast(Unit caster, CardID skillId)
    {
        if (_visualUnits.TryGetValue(caster, out GameObject casterObj))
        {
            switch (skillId)
            {
                case CardID.SollarDuelist: StartCoroutine(AnimateSpin(casterObj.transform)); break;
                case CardID.VoidHeavyShooter: StartCoroutine(AnimateShake(casterObj.transform)); break;
                case CardID.SollarCommander:
                case CardID.VoidCommander:
                case CardID.SollarForce: StartCoroutine(AnimatePulse(casterObj.transform)); break;
            }
        }
    }

    private void ExecuteRangedVisuals(GameObject attackerObj, GameObject defenderObj, bool shootProjectile)
    {
        if (shootProjectile && prefabVoidBullet != null)
        {
            Vector3 shootStartPos = attackerObj.transform.position + Vector3.up;
            UnitWorldUI uiData = attackerObj.GetComponent<UnitWorldUI>();
            if (uiData != null && uiData.shootPoint != null) shootStartPos = uiData.shootPoint.position;

            Vector3 shootTargetPos = defenderObj.transform.position + Vector3.up;
            StartCoroutine(AnimateProjectile(shootStartPos, shootTargetPos));
        }
        else
        {
            // Sollar Force
            StartCoroutine(AnimatePulse(attackerObj.transform));
        }
    }

    private IEnumerator AnimateWalkToRangeAndAttack(Unit attacker, GameObject attackerObj, GameObject defenderObj, float range, bool shootProjectile)
    {
        Transform attackerTransform = attackerObj.transform;
        Vector3 originalPosition = attackerTransform.position;
        Vector3 enemyPosition = defenderObj.transform.position;

        Vector3 combatPosition = enemyPosition + (originalPosition - enemyPosition).normalized * range;
        combatPosition.y = originalPosition.y;

        float walkSpeed = 8f;

        while (Vector3.Distance(attackerTransform.position, combatPosition) > 0.1f)
        {
            attackerTransform.position = Vector3.MoveTowards(attackerTransform.position, combatPosition, walkSpeed * Time.deltaTime);
            yield return null;
        }

        attackerTransform.position = combatPosition;

        Transform baseArea = (attacker.ownerId == 0) ? p0BoardArea : p1BoardArea;
        Vector3 localOffset = attackerTransform.position - baseArea.position;
        attacker.logicalPosition = new Vector2(localOffset.x, localOffset.z);

        ExecuteRangedVisuals(attackerObj, defenderObj, shootProjectile);
    }

    private IEnumerator AnimateMeleeWalkAndStrike(Unit attacker, Transform attackerTransform, Vector3 enemyPosition)
    {
        Vector3 originalPosition = attackerTransform.position;
        Vector3 combatPosition = enemyPosition + (originalPosition - enemyPosition).normalized * 1.2f;
        combatPosition.y = originalPosition.y;

        float walkSpeed = 8f;

        while (Vector3.Distance(attackerTransform.position, combatPosition) > 0.1f)
        {
            attackerTransform.position = Vector3.MoveTowards(attackerTransform.position, combatPosition, walkSpeed * Time.deltaTime);
            yield return null;
        }

        Vector3 strikePos = combatPosition + (enemyPosition - combatPosition).normalized * 0.4f;
        float bumpTime = 0.08f;
        float timer = 0;

        while (timer < bumpTime)
        {
            timer += Time.deltaTime;
            attackerTransform.position = Vector3.Lerp(combatPosition, strikePos, timer / bumpTime);
            yield return null;
        }

        timer = 0;
        while (timer < bumpTime)
        {
            timer += Time.deltaTime;
            attackerTransform.position = Vector3.Lerp(strikePos, combatPosition, timer / bumpTime);
            yield return null;
        }

        attackerTransform.position = combatPosition;

        Transform baseArea = (attacker.ownerId == 0) ? p0BoardArea : p1BoardArea;
        Vector3 localOffset = attackerTransform.position - baseArea.position;
        attacker.logicalPosition = new Vector2(localOffset.x, localOffset.z);
    }

    private IEnumerator AnimateProjectile(Vector3 startPos, Vector3 targetPos)
    {
        GameObject bullet = Instantiate(prefabVoidBullet, startPos, Quaternion.identity);
        float duration = 0.15f;
        float timer = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(startPos, targetPos, timer / duration);
            yield return null;
        }
        Destroy(bullet);
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