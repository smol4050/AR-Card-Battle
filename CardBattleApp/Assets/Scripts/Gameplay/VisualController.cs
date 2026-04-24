using UnityEngine;
using System;
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
    public GameObject prefabSollarBullet;
    public GameObject prefabForceImpact;
    public GameObject prefabStaggerFX;

    [Header("Áreas Base")]
    public Transform p0BoardArea;
    public Transform p1BoardArea;

    [Header("Slots P0")]
    public Transform[] p0FrontSlots = new Transform[3];
    public Transform[] p0BackSlots = new Transform[3];

    [Header("Slots P1")]
    public Transform[] p1FrontSlots = new Transform[3];
    public Transform[] p1BackSlots = new Transform[3];

    // ── Parámetros del Animator (deben coincidir exactamente con los del controller) ──
    // JovenController  → Velocidad (Float), Atacar (Trigger), Morir (Trigger)
    // TropasController → Velocidad (Float), Atacar (Trigger), Morir (Trigger)
    private static readonly int ANIM_VELOCIDAD = Animator.StringToHash("Velocidad");
    private static readonly int ANIM_ATACAR = Animator.StringToHash("Atacar");
    private static readonly int ANIM_NUM_ATAQUE = Animator.StringToHash("NumAtaque");
    private static readonly int ANIM_MORIR = Animator.StringToHash("Morir");

    // Cuánto tiempo esperar antes de destruir el GameObject tras morir
    // (debe ser >= duración de la animación de muerte)
    [Header("Configuración de Muerte")]
    [Tooltip("Segundos que tarda en destruirse el personaje tras morir (ajusta según la animación)")]
    public float deathDestroyDelay = 2.5f;

    // Duración en segundos del efecto de partículas al golpear
    [Header("Partículas de Golpe")]
    [Tooltip("Cuántos segundos dura el sistema de partículas al impactar")]
    public float hitParticlesDuration = 0.6f;

    private Dictionary<Unit, GameObject> _visualUnits = new Dictionary<Unit, GameObject>();

    // ─── SUSCRIPCIÓN ──────────────────────────────────────────────────────────
    private void OnEnable()
    {
        gameManager.OnUnitSpawned += HandleUnitSpawned;
        gameManager.OnUnitDied += HandleUnitDied;
        gameManager.OnUnitSkillCast += HandleUnitSkillCast;
        gameManager.OnVisualEffectRequested += HandleVisualEffectRequested;
        gameManager.OnAttackAnimationRequested += HandleAttackAnimationRequested;
        gameManager.OnSOL9ProjectileRequested += HandleSOL9ProjectileRequested;
    }

    private void OnDisable()
    {
        if (gameManager == null) return;
        gameManager.OnUnitSpawned -= HandleUnitSpawned;
        gameManager.OnUnitDied -= HandleUnitDied;
        gameManager.OnUnitSkillCast -= HandleUnitSkillCast;
        gameManager.OnVisualEffectRequested -= HandleVisualEffectRequested;
        gameManager.OnAttackAnimationRequested -= HandleAttackAnimationRequested;
        gameManager.OnSOL9ProjectileRequested -= HandleSOL9ProjectileRequested;
    }

    // ─── SPAWN ────────────────────────────────────────────────────────────────
    private void HandleUnitSpawned(int playerId, Unit unitData)
    {
        GameObject prefabToUse = GetPrefabByCardId(unitData.cardId);
        if (prefabToUse == null) return;

        Vector3 exactWorldPos;
        if (playerId == 0)
        {
            Transform slot = (unitData.row == 0)
                ? p0FrontSlots[unitData.slotIndex]
                : p0BackSlots[unitData.slotIndex - 3];
            exactWorldPos = slot.position;
        }
        else
        {
            Transform slot = (unitData.row == 0)
                ? p1FrontSlots[unitData.slotIndex]
                : p1BackSlots[unitData.slotIndex - 3];
            exactWorldPos = slot.position;
        }

        Transform baseRotArea = (playerId == 0) ? p0BoardArea : p1BoardArea;
        GameObject newUnit = Instantiate(prefabToUse, exactWorldPos, baseRotArea.rotation);

        UnitWorldUI ui = newUnit.GetComponent<UnitWorldUI>();
        if (ui != null) ui.Initialize(unitData);

        _visualUnits[unitData] = newUnit;
        Vector3 offset = exactWorldPos - baseRotArea.position;
        unitData.logicalPosition = new Vector2(offset.x, offset.z);

        // Asegurar que el personaje empiece en idle
        Animator anim = newUnit.GetComponent<Animator>();
        if (anim != null) anim.SetFloat(ANIM_VELOCIDAD, 0f);

        // Asegurar que el sistema de partículas empiece apagado
        ParticleSystem ps = newUnit.GetComponentInChildren<ParticleSystem>();
        if (ps != null && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

    // ─── MUERTE ───────────────────────────────────────────────────────────────
    private void HandleUnitDied(int playerId, Unit unit)
    {
        if (!_visualUnits.TryGetValue(unit, out GameObject obj)) return;
        if (obj == null) { _visualUnits.Remove(unit); return; }

        // Primero animar, DESPUÉS destruir con delay
        Animator anim = obj.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetFloat(ANIM_VELOCIDAD, 0f);
            anim.SetTrigger(ANIM_MORIR);
        }

        _visualUnits.Remove(unit);
        StartCoroutine(DelayedDestroy(obj, deathDestroyDelay));
    }

    private IEnumerator DelayedDestroy(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) Destroy(obj);
    }

    // ─── EFECTO VISUAL GENÉRICO ───────────────────────────────────────────────
    private void HandleVisualEffectRequested(Unit caster, Unit target, string effectName)
    {
        if (target != null && _visualUnits.TryGetValue(target, out GameObject targetObj))
        {
            if (targetObj == null) return;
            if (effectName == "ForceHit" && prefabForceImpact != null)
                Instantiate(prefabForceImpact, targetObj.transform.position + Vector3.up, Quaternion.identity);
        }
    }

    // ─── ATAQUE BÁSICO (RANGED Y MELEE) ──────────────────────────────────────
    // Recibe el callback onImpact del GameManager y lo ejecuta cuando la
    // bala llega al objetivo o cuando el puño conecta en la animación melee.
    private void HandleAttackAnimationRequested(Unit attacker, Unit target, Action onImpact)
    {
        if (!_visualUnits.TryGetValue(attacker, out GameObject attObj) || attObj == null) return;
        if (!_visualUnits.TryGetValue(target, out GameObject defObj) || defObj == null) return;

        // Mirar al objetivo
        Vector3 targetPos = defObj.transform.position;
        attObj.transform.LookAt(new Vector3(targetPos.x, attObj.transform.position.y, targetPos.z));

        UnitWorldUI ui = attObj.GetComponent<UnitWorldUI>();
        bool isRanged = ui != null && ui.shootPoint != null;
        float distance = Vector3.Distance(attObj.transform.position, targetPos);
        float maxRangedDist = 4.5f;

        if (isRanged)
        {
            if (distance > maxRangedDist)
                StartCoroutine(AnimateWalkToRangeAndShoot(attObj, defObj, maxRangedDist, onImpact));
            else
                StartCoroutine(AnimateProjectileWithImpact(attObj, defObj, onImpact));
        }
        else
        {
            StartCoroutine(AnimateMeleeWalkAndStrike(attObj, defObj, onImpact));
        }
    }

    // ─── SOL-9 DISRUPTION BARRAGE ─────────────────────────────────────────────
    // Cada proyectil lleva su propio callback: el daño ocurre al llegar.
    private void HandleSOL9ProjectileRequested(Unit caster, Unit target, int projectileIndex, Action onImpact)
    {
        if (!_visualUnits.TryGetValue(caster, out GameObject casterObj) || casterObj == null) return;
        if (!_visualUnits.TryGetValue(target, out GameObject targetObj) || targetObj == null) return;

        float delay = projectileIndex * 0.12f;
        StartCoroutine(FireSOL9Projectile(casterObj, targetObj, target, delay, projectileIndex, onImpact));
    }

    private IEnumerator FireSOL9Projectile(
        GameObject casterObj, GameObject targetObj, Unit targetData,
        float initialDelay, int projectileIndex, Action onImpact)
    {
        yield return new WaitForSeconds(initialDelay);

        if (casterObj == null || targetObj == null) yield break;

        // Animación de disparo en el caster (solo en el primer proyectil para no spam)
        if (projectileIndex == 0)
        {
            Animator casterAnim = casterObj.GetComponent<Animator>();
            casterAnim?.SetTrigger(ANIM_ATACAR);
        }


        UnitWorldUI ui = casterObj.GetComponent<UnitWorldUI>();
        Vector3 spawnPos = (ui != null && ui.shootPoint != null)
                           ? ui.shootPoint.position
                           : casterObj.transform.position + Vector3.up * 0.8f;

        Vector3 targetPos = targetObj.transform.position + Vector3.up * 0.9f;
        targetPos += new Vector3(
            UnityEngine.Random.Range(-0.15f, 0.15f), 0,
            UnityEngine.Random.Range(-0.15f, 0.15f));

        GameObject bulletPrefab = prefabSollarBullet != null ? prefabSollarBullet : prefabVoidBullet;
        if (bulletPrefab == null)
        {
            // Sin prefab aún: aplicamos el daño igualmente
            onImpact?.Invoke();
            yield break;
        }

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.transform.LookAt(targetPos);

        float travelTime = 0.10f;
        float t = 0f;
        while (t < travelTime && bullet != null)
        {
            t += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(spawnPos, targetPos, t / travelTime);
            yield return null;
        }

        if (bullet != null) Destroy(bullet);

        // ── IMPACTO: aplicar daño ahora que la bala llegó ─────────────────────
        onImpact?.Invoke();

        if (targetObj != null)
        {
            // Partículas en el objetivo al recibir el golpe
            PlayHitParticles(casterObj);

            if (prefabForceImpact != null)
                Instantiate(prefabForceImpact, targetObj.transform.position + Vector3.up * 0.9f, Quaternion.identity);

            // Stagger FX en el segundo impacto (cuando projectileHitCount llega a 2)
            if (projectileIndex == 1 && prefabStaggerFX != null && targetData.stunTimer > 0)
                Instantiate(prefabStaggerFX, targetObj.transform.position + Vector3.up, Quaternion.identity);
        }
    }

    // ─── ANIMACIÓN RANGED: caminar hasta rango y disparar ────────────────────
    private IEnumerator AnimateWalkToRangeAndShoot(
        GameObject attObj, GameObject defObj, float range, Action onImpact)
    {
        if (attObj == null || defObj == null) yield break;

        Animator anim = attObj.GetComponent<Animator>();

        Vector3 enemyPos = defObj.transform.position;
        Vector3 combatPos = enemyPos + (attObj.transform.position - enemyPos).normalized * range;
        combatPos.y = attObj.transform.position.y;

        // Caminar
        anim?.SetFloat(ANIM_VELOCIDAD, 1f);

        while (attObj != null && Vector3.Distance(attObj.transform.position, combatPos) > 0.1f)
        {
            attObj.transform.position = Vector3.MoveTowards(
                attObj.transform.position, combatPos, 8f * Time.deltaTime);
            yield return null;
        }
        anim?.SetFloat(ANIM_VELOCIDAD, 0f);

        if (attObj == null || defObj == null) yield break;

        yield return StartCoroutine(AnimateProjectileWithImpact(attObj, defObj, onImpact));
    }

    // ─── PROYECTIL RANGED BÁSICO ──────────────────────────────────────────────
    // El daño se aplica cuando el proyectil llega al destino.
    private IEnumerator AnimateProjectileWithImpact(
        GameObject attObj, GameObject defObj, Action onImpact)
    {
        if (attObj == null || defObj == null)
        {
            onImpact?.Invoke();
            yield break;
        }

        // Disparar animación de ataque
        Animator anim = attObj.GetComponent<Animator>();
        anim?.SetFloat(ANIM_VELOCIDAD, 0f);
        anim?.SetTrigger(ANIM_ATACAR);

        UnitWorldUI ui = attObj.GetComponent<UnitWorldUI>();
        Vector3 startPos = (ui != null && ui.shootPoint != null)
                           ? ui.shootPoint.position
                           : attObj.transform.position + Vector3.up * 0.8f;

        Vector3 endPos = defObj.transform.position + Vector3.up;

        GameObject bulletPrefab = prefabVoidBullet;
        if (bulletPrefab == null)
        {
            onImpact?.Invoke();
            yield break;
        }

        GameObject bullet = Instantiate(bulletPrefab, startPos, Quaternion.identity);
        bullet.transform.LookAt(endPos);

        float travelTime = 0.15f;
        float t = 0f;
        while (t < travelTime && bullet != null)
        {
            t += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(startPos, endPos, t / travelTime);
            yield return null;
        }

        if (bullet != null) Destroy(bullet);

        // ── IMPACTO ───────────────────────────────────────────────────────────
        onImpact?.Invoke();

        if (defObj != null)
        {
            // Partículas en el objetivo
            PlayHitParticles(defObj);

            if (prefabForceImpact != null)
                Instantiate(prefabForceImpact, defObj.transform.position + Vector3.up, Quaternion.identity);
        }
    }

    // ─── ANIMACIÓN MELEE: acercarse y golpear ─────────────────────────────────
    // El daño se aplica en el fotograma del "strike" (extensión del brazo).
    private IEnumerator AnimateMeleeWalkAndStrike(
        GameObject attObj, GameObject defObj, Action onImpact)
    {
        if (attObj == null || defObj == null) yield break;
        Animator anim = attObj.GetComponent<Animator>();

        Vector3 enemyPos = defObj.transform.position;
        Vector3 combatPos = enemyPos + (attObj.transform.position - enemyPos).normalized * 1.2f;
        combatPos.y = attObj.transform.position.y;

        // Caminar hacia el objetivo
        anim?.SetFloat(ANIM_VELOCIDAD, 1f);
        while (attObj != null && Vector3.Distance(attObj.transform.position, combatPos) > 0.1f)
        {
            attObj.transform.position = Vector3.MoveTowards(
                attObj.transform.position, combatPos, 8f * Time.deltaTime);
            yield return null;
        }
        anim?.SetFloat(ANIM_VELOCIDAD, 0f);

        if (attObj == null || defObj == null) yield break;

        // Disparar animación de ataque (justo antes del lunge)
        int variacion = UnityEngine.Random.Range(0, 2); // El 2 es exclusivo, devuelve 0 o 1
        anim.SetInteger(ANIM_NUM_ATAQUE, variacion);
        anim?.SetTrigger(ANIM_ATACAR);

        // Pequeño delay para que la animación se vea antes del impacto
        yield return new WaitForSeconds(0.2f);

        if (attObj == null || defObj == null) yield break;

        // Extender el golpe hacia el objetivo
        Vector3 strikePos = combatPos + (enemyPos - combatPos).normalized * 0.4f;
        float t = 0f;
        while (t < 0.08f && attObj != null)
        {
            t += Time.deltaTime;
            attObj.transform.position = Vector3.Lerp(combatPos, strikePos, t / 0.08f);
            yield return null;
        }

        // ── IMPACTO: el puño llegó al objetivo ────────────────────────────────
        onImpact?.Invoke();

        if (defObj != null)
        {
            // Partículas en el objetivo
            PlayHitParticles(defObj);

            if (prefabForceImpact != null)
                Instantiate(prefabForceImpact, defObj.transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        // Retirar el brazo
        if (attObj != null) attObj.transform.position = combatPos;
    }






    // ─── PARTÍCULAS DE GOLPE ──────────────────────────────────────────────────
    /// <summary>
    /// Activa el sistema de partículas del personaje objetivo y lo apaga
    /// automáticamente después de <see cref="hitParticlesDuration"/> segundos.
    /// El ParticleSystem debe estar en el prefab del personaje (puede ser un hijo).
    /// </summary>
    private void PlayHitParticles(GameObject targetObj)
    {
        ParticleSystem[] allPS = targetObj.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in allPS)
        {
            if (ps.CompareTag("WeaponVFX")) continue;

            ps.Play();
            // ¡Activa esta línea para obligar a las partículas a detenerse!
            StartCoroutine(StopParticlesAfterDelay(ps, hitParticlesDuration));
        }
    }

    private IEnumerator StopParticlesAfterDelay(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }







    // ─── SKILL CAST (animaciones del caster) ──────────────────────────────────
    private void HandleUnitSkillCast(Unit caster, CardID skillId)
    {
        if (!_visualUnits.TryGetValue(caster, out GameObject casterObj) || casterObj == null) return;

        // Siempre disparar la animación de ataque en el skill
        Animator anim = casterObj.GetComponent<Animator>();
        anim?.SetTrigger(ANIM_ATACAR);

        switch (skillId)
        {
            case CardID.SollarDuelist: StartCoroutine(AnimateSpin(casterObj.transform)); break;
            case CardID.VoidHeavyShooter: StartCoroutine(AnimateShake(casterObj.transform)); break;
            case CardID.SollarForce: StartCoroutine(AnimatePulse(casterObj.transform)); break;
            default: StartCoroutine(AnimatePulse(casterObj.transform)); break;
        }
    }

    // ─── ANIMACIONES DE SOPORTE ───────────────────────────────────────────────
    private IEnumerator AnimateSpin(Transform t)
    {
        Vector3 startRot = t.eulerAngles;
        float timer = 0f;
        while (timer < 0.4f && t != null)
        {
            timer += Time.deltaTime;
            t.eulerAngles = new Vector3(startRot.x,
                startRot.y + Mathf.Lerp(0, 360, timer / 0.4f), startRot.z);
            yield return null;
        }
    }

    private IEnumerator AnimatePulse(Transform t)
    {
        Vector3 orig = t.localScale;
        float timer = 0f;
        while (timer < 0.2f && t != null)
        {
            timer += Time.deltaTime;
            t.localScale = Vector3.Lerp(orig, orig * 1.5f, timer / 0.2f);
            yield return null;
        }
        timer = 0f;
        while (timer < 0.2f && t != null)
        {
            timer += Time.deltaTime;
            t.localScale = Vector3.Lerp(orig * 1.5f, orig, timer / 0.2f);
            yield return null;
        }
    }

    private IEnumerator AnimateShake(Transform t)
    {
        Vector3 orig = t.position;
        float timer = 0f;
        while (timer < 0.5f && t != null)
        {
            timer += Time.deltaTime;
            t.position = orig + new Vector3(
                UnityEngine.Random.Range(-0.2f, 0.2f), 0,
                UnityEngine.Random.Range(-0.2f, 0.2f));
            yield return null;
        }
        if (t != null) t.position = orig;
    }
}