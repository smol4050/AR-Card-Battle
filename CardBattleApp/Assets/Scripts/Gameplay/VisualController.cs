using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Collections.Generic;

public class VisualController : MonoBehaviour
{
    public GameManager gameManager;

    [Header("Sollar Prefabs — Unidades")]
    public GameObject prefabSollarDuelist;
    public GameObject prefabSollarForce;      // SOL-9
    public GameObject prefabSollarCommander;

    [Header("Void Prefabs — Unidades")]
    public GameObject prefabVoidHorde;
    public GameObject prefabVoidCommander;
    public GameObject prefabVoidHeavyShooter;

    [Header("Naves - Setup P0")]
    public Transform p0ShipEntryPoint;       // Punto X donde aparece
    public Transform p0ShipBoardPoint;       // Punto donde se posiciona para la batalla
    public Transform p0ShipConvergencePoint; // Punto donde se une la energía
    public Transform[] p0ShipPatrolPoints;   // Puntos por donde se mueve en combate

    [Header("Naves - Setup P1")]
    public Transform p1ShipEntryPoint;
    public Transform p1ShipBoardPoint;
    public Transform p1ShipConvergencePoint;
    public Transform[] p1ShipPatrolPoints;

    [Header("Ship Prefabs")]
    public GameObject prefabSolarVanguard;    // nave Sollar (posee múltiples shootPoints)
    public GameObject prefabAbyssReaper;      // nave Void

    [Header("VFX Prefabs")]
    public GameObject prefabVoidBullet;
    public GameObject prefabSollarBullet;
    public GameObject prefabAbyssPulse;       // VFX del pulso de la nave Void
    public GameObject prefabSolarBlast;       // VFX del impacto de la nave Sollar
    public GameObject prefabStaggerFX;
    public GameObject prefabBurnFX;

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
    [Tooltip("Punto donde aparece la nave del jugador 0")]
    public Transform p0ShipSpawnPoint;
    [Tooltip("Punto donde aparece la nave del jugador 1")]
    public Transform p1ShipSpawnPoint;

    [Header("Configuración")]
    public float deathDestroyDelay = 2.5f;
    public float hitParticlesDuration = 0.6f;

    // Parámetros de Animator (deben coincidir con los AnimatorControllers de los prefabs)
    private static readonly int ANIM_VELOCIDAD = Animator.StringToHash("Velocidad");
    private static readonly int ANIM_ATACAR = Animator.StringToHash("Atacar");
    private static readonly int ANIM_NUM_ATAQUE = Animator.StringToHash("NumAtaque");
    private static readonly int ANIM_MORIR = Animator.StringToHash("Morir");

    private Dictionary<Unit, GameObject> _visualUnits = new Dictionary<Unit, GameObject>();
    private Dictionary<ShipInstance, GameObject> _visualShips = new Dictionary<ShipInstance, GameObject>();

    // ─── SUSCRIPCIÓN ──────────────────────────────────────────────────────────
    private void OnEnable()
    {
        gameManager.OnUnitSpawned += HandleUnitSpawned;
        gameManager.OnUnitDied += HandleUnitDied;
        gameManager.OnUnitSkillCast += HandleUnitSkillCast;
        gameManager.OnAttackAnimationRequested += HandleAttackAnimationRequested;
        gameManager.OnSOL9ProjectileRequested += HandleSOL9ProjectileRequested;
        gameManager.OnShipSpawned += HandleShipSpawned;
        gameManager.OnShipExpired += HandleShipExpired;
        gameManager.OnShipPulseFired += HandleShipPulseFired;
    }

    private void OnDisable()
    {
        if (gameManager == null) return;
        gameManager.OnUnitSpawned -= HandleUnitSpawned;
        gameManager.OnUnitDied -= HandleUnitDied;
        gameManager.OnUnitSkillCast -= HandleUnitSkillCast;
        gameManager.OnAttackAnimationRequested -= HandleAttackAnimationRequested;
        gameManager.OnSOL9ProjectileRequested -= HandleSOL9ProjectileRequested;
        gameManager.OnShipSpawned -= HandleShipSpawned;
        gameManager.OnShipExpired -= HandleShipExpired;
        gameManager.OnShipPulseFired -= HandleShipPulseFired;
    }

    // ─── SPAWN DE UNIDAD ──────────────────────────────────────────────────────
    private void HandleUnitSpawned(int playerId, Unit unitData)
    {
        GameObject prefab = GetUnitPrefab(unitData.cardId);
        if (prefab == null) return;

        Vector3 worldPos = GetSlotWorldPos(playerId, unitData.row, unitData.slotIndex);
        Transform boardArea = (playerId == 0) ? p0BoardArea : p1BoardArea;

        GameObject go = Instantiate(prefab, worldPos, boardArea.rotation);

        UnitWorldUI ui = go.GetComponent<UnitWorldUI>();
        if (ui != null) ui.Initialize(unitData);

        Vector3 offset = worldPos - boardArea.position;
        unitData.logicalPosition = new Vector2(offset.x, offset.z);

        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            // Seguridad: Apagamos el agente, movemos el objeto y lo volvemos a prender
            // Esto fuerza a Unity a recalcular el anclaje al NavMesh correctamente.
            agent.enabled = false;
            go.transform.position = worldPos;
            agent.enabled = true;

            agent.speed = unitData.FinalSpeed * 3f;
            agent.stoppingDistance = 0.1f;
            agent.autoBraking = true;
        }

        Animator anim = go.GetComponent<Animator>();
        if (anim != null) anim.SetFloat(ANIM_VELOCIDAD, 0f);

        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
            if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        _visualUnits[unitData] = go;
    }

    private Vector3 GetSlotWorldPos(int playerId, int row, int slotIndex)
    {
        if (playerId == 0)
            return (row == 0) ? p0FrontSlots[slotIndex].position : p0BackSlots[slotIndex - 3].position;
        else
            return (row == 0) ? p1FrontSlots[slotIndex].position : p1BackSlots[slotIndex - 3].position;
    }

    private GameObject GetUnitPrefab(CardID id)
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
        if (!_visualUnits.TryGetValue(unit, out GameObject go)) return;
        _visualUnits.Remove(unit);
        if (go == null) return;

        // Desactivar NavMeshAgent para que no interfiera con la animación de muerte
        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        Animator anim = go.GetComponent<Animator>();
        if (anim != null) { anim.SetFloat(ANIM_VELOCIDAD, 0f); anim.SetTrigger(ANIM_MORIR); }

        StartCoroutine(DelayedDestroy(go, deathDestroyDelay));
    }

    private IEnumerator DelayedDestroy(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null) Destroy(go);
    }

    // ─── SPAWN Y EXPIRACIÓN DE NAVE ───────────────────────────────────────────
    private void HandleShipSpawned(int ownerId, ShipInstance ship)
    {
        GameObject prefab = (ship.cardId == CardID.SolarVanguard) ? prefabSolarVanguard : prefabAbyssReaper;
        if (prefab == null) return;

        Transform entryPoint = (ownerId == 0) ? p0ShipEntryPoint : p1ShipEntryPoint;
        Transform boardPoint = (ownerId == 0) ? p0ShipBoardPoint : p1ShipBoardPoint;
        Transform[] patrolPoints = (ownerId == 0) ? p0ShipPatrolPoints : p1ShipPatrolPoints;

        if (entryPoint == null || boardPoint == null) return;

        GameObject go = Instantiate(prefab, entryPoint.position, entryPoint.rotation);
        _visualShips[ship] = go;

        // Iniciar secuencia completa: Entrar -> Patrullar
        StartCoroutine(AnimateShipEntryAndPatrol(go, entryPoint.position, boardPoint.position, patrolPoints));
    }

    private void HandleShipExpired(int ownerId, ShipInstance ship)
    {
        if (!_visualShips.TryGetValue(ship, out GameObject go)) return;
        _visualShips.Remove(ship);
        if (go != null) StartCoroutine(AnimateShipExit(go));
    }

    private IEnumerator AnimateShipEntryAndPatrol(GameObject shipGo, Vector3 startPos, Vector3 boardPos, Transform[] patrolPoints)
    {
        // 1. Animación de entrada
        float elapsed = 0f, dur = 1.5f;
        while (elapsed < dur && shipGo != null)
        {
            elapsed += Time.deltaTime;
            // Usar una curva de interpolación suave (Ease-Out)
            float t = 1f - Mathf.Pow(1f - (elapsed / dur), 3f);
            shipGo.transform.position = Vector3.Lerp(startPos, boardPos, t);
            yield return null;
        }

        // 2. Patrullaje en combate
        if (patrolPoints == null || patrolPoints.Length == 0) yield break;

        int currentPatrolIndex = 0;
        while (shipGo != null)
        {
            Vector3 targetPatrol = patrolPoints[currentPatrolIndex].position;
            // Moverse hacia el punto de patrullaje lentamente
            shipGo.transform.position = Vector3.MoveTowards(shipGo.transform.position, targetPatrol, 1.5f * Time.deltaTime);

            if (Vector3.Distance(shipGo.transform.position, targetPatrol) < 0.1f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
            yield return null;
        }
    }

    private IEnumerator AnimateShipExit(GameObject go)
    {
        if (go == null) yield break;
        Vector3 startPos = go.transform.position;
        Vector3 endPos = startPos + Vector3.up * 8f;
        float elapsed = 0f, dur = 0.6f;
        while (elapsed < dur && go != null)
        {
            elapsed += Time.deltaTime;
            go.transform.position = Vector3.Lerp(startPos, endPos, elapsed / dur);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ─── PULSO DE NAVE ────────────────────────────────────────────────────────
    private void HandleShipPulseFired(int ownerId, ShipInstance ship, int pulseIndex, List<Unit> targetsHit)
    {
        if (!_visualShips.TryGetValue(ship, out GameObject shipGo) || shipGo == null) return;

        Transform[] shootPoints = GetShipShootPoints(shipGo);
        Transform convergencePoint = (ownerId == 0) ? p0ShipConvergencePoint : p1ShipConvergencePoint;

        if (ship.cardId == CardID.SolarVanguard)
            StartCoroutine(FireConvergingBarrage(shootPoints, convergencePoint, targetsHit, prefabSollarBullet, prefabSolarBlast));
        else
            StartCoroutine(FireConvergingBarrage(shootPoints, convergencePoint, targetsHit, prefabVoidBullet, prefabAbyssPulse));
    }

    private IEnumerator FireConvergingBarrage(Transform[] shootPoints, Transform convergencePoint, List<Unit> targets, GameObject projectilePrefab, GameObject blastPrefab)
    {
        if (convergencePoint == null || projectilePrefab == null) yield break;

        // Fase 1: Energía desde los puntos de disparo de la nave hasta el punto de convergencia
        List<GameObject> chargeParticles = new List<GameObject>();
        foreach (Transform sp in shootPoints)
        {
            GameObject charge = Instantiate(projectilePrefab, sp.position, Quaternion.identity);
            charge.transform.LookAt(convergencePoint);
            chargeParticles.Add(charge);
        }

        float chargeTime = 0f, chargeDuration = 0.3f;
        while (chargeTime < chargeDuration)
        {
            chargeTime += Time.deltaTime;
            for (int i = 0; i < chargeParticles.Count; i++)
            {
                if (chargeParticles[i] != null)
                {
                    chargeParticles[i].transform.position = Vector3.Lerp(shootPoints[i % shootPoints.Length].position, convergencePoint.position, chargeTime / chargeDuration);
                }
            }
            yield return null;
        }

        // Limpiar partículas de carga
        foreach (var p in chargeParticles) if (p != null) Destroy(p);

        // Opcional: Pequeña explosión en el punto de convergencia antes de disparar
        if (blastPrefab != null) Instantiate(blastPrefab, convergencePoint.position, Quaternion.identity);

        // Fase 2: Disparar desde el punto de convergencia hacia los objetivos
        foreach (Unit target in targets)
        {
            if (_visualUnits.TryGetValue(target, out GameObject defObj) && defObj != null)
            {
                StartCoroutine(AnimateProjectile(convergencePoint.position, defObj.transform.position + Vector3.up, projectilePrefab, blastPrefab));
            }
            yield return new WaitForSeconds(0.05f); // Micro-retraso para un efecto ametralladora
        }
    }

    private IEnumerator AnimateProjectile(Vector3 start, Vector3 end, GameObject bulletPrefab, GameObject impactPrefab)
    {
        GameObject bullet = Instantiate(bulletPrefab, start, Quaternion.identity);
        bullet.transform.LookAt(end);

        float t = 0f, dur = 0.15f;
        while (t < dur && bullet != null)
        {
            t += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(start, end, t / dur);
            yield return null;
        }
        if (bullet != null) Destroy(bullet);
        if (impactPrefab != null) Instantiate(impactPrefab, end, Quaternion.identity);
    }

    // Recoge todos los hijos del prefab de nave con el tag "ShootPoint"
    // El prefab debe tener esos objetos etiquetados correctamente.
    private Transform[] GetShipShootPoints(GameObject shipGo)
    {
        List<Transform> points = new List<Transform>();
        foreach (Transform child in shipGo.GetComponentsInChildren<Transform>(true))
            if (child.CompareTag("ShootPoint"))
                points.Add(child);

        // Si no hay ninguno etiquetado, usamos el root como fallback
        if (points.Count == 0) points.Add(shipGo.transform);
        return points.ToArray();
    }

    // Solar Vanguard: dispara un proyectil desde cada shootPoint hacia los objetivos,
    // repartiendo los targets entre los puntos disponibles.
    private IEnumerator FireSolarVanguardBarrage(
        GameObject shipGo, Transform[] shootPoints, List<Unit> targets)
    {
        if (targets.Count == 0) yield break;

        for (int i = 0; i < targets.Count; i++)
        {
            Unit target = targets[i];
            Transform from = shootPoints[i % shootPoints.Length];

            if (!_visualUnits.TryGetValue(target, out GameObject defObj) || defObj == null) continue;

            GameObject bulletPrefab = prefabSollarBullet != null ? prefabSollarBullet : prefabVoidBullet;
            if (bulletPrefab == null) continue;

            // Lanzamos cada proyectil en paralelo (sin yield entre ellos)
            StartCoroutine(AnimateShipProjectile(from.position,
                defObj.transform.position + Vector3.up,
                bulletPrefab, prefabSolarBlast));

            // Pequeño escalonamiento visual (no lógico — el daño ya se aplicó)
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Abyss Reaper: onda expansiva desde la posición de la nave
    private IEnumerator FireAbyssReaperPulse(GameObject shipGo, List<Unit> targets)
    {
        if (prefabAbyssPulse != null)
            Instantiate(prefabAbyssPulse, shipGo.transform.position, Quaternion.identity);

        // Efecto de impacto en cada unidad afectada
        foreach (Unit target in targets)
        {
            if (_visualUnits.TryGetValue(target, out GameObject defObj) && defObj != null)
                PlayHitParticles(defObj);
        }
        yield break;
    }

    private IEnumerator AnimateShipProjectile(
        Vector3 start, Vector3 end, GameObject bulletPrefab, GameObject impactPrefab)
    {
        GameObject bullet = Instantiate(bulletPrefab, start, Quaternion.identity);
        bullet.transform.LookAt(end);

        float t = 0f, dur = 0.12f;
        while (t < dur && bullet != null)
        {
            t += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(start, end, t / dur);
            yield return null;
        }
        if (bullet != null) Destroy(bullet);

        if (impactPrefab != null)
            Instantiate(impactPrefab, end, Quaternion.identity);
    }

    // ─── ATAQUE BÁSICO ────────────────────────────────────────────────────────
    private void HandleAttackAnimationRequested(Unit attacker, Unit target, Action onImpact)
    {
        if (!_visualUnits.TryGetValue(attacker, out GameObject attObj) || attObj == null) return;
        if (!_visualUnits.TryGetValue(target, out GameObject defObj) || defObj == null) return;

        // Orientar hacia el objetivo
        Vector3 targetPos = defObj.transform.position;
        attObj.transform.LookAt(new Vector3(targetPos.x, attObj.transform.position.y, targetPos.z));

        UnitWorldUI ui = attObj.GetComponent<UnitWorldUI>();
        bool isRanged = ui != null && ui.shootPoint != null;
        float distance = Vector3.Distance(attObj.transform.position, targetPos);

        if (isRanged)
        {
            if (distance > 4.5f)
                StartCoroutine(NavMeshWalkToRangeAndShoot(attacker, attObj, defObj, 4.5f, onImpact));
            else
                StartCoroutine(AnimateProjectileWithImpact(attObj, defObj, onImpact));
        }
        else
        {
            StartCoroutine(NavMeshMeleeWalkAndStrike(attacker, attObj, defObj, onImpact));
        }
    }

    // ─── MOVIMIENTO MELEE CON NAVMESH ─────────────────────────────────────────
    private IEnumerator NavMeshMeleeWalkAndStrike(
         Unit attackerData, GameObject attObj, GameObject defObj, Action onImpact)
    {
        if (attObj == null || defObj == null) yield break;

        NavMeshAgent agent = attObj.GetComponent<NavMeshAgent>();
        Animator anim = attObj.GetComponent<Animator>();

        // Si no hay agente o falló al anclarse al NavMesh, usamos el fallback
        if (agent == null || !agent.isOnNavMesh)
        {
            yield return StartCoroutine(FallbackMeleeWalk(attObj, defObj, onImpact));
            yield break;
        }

        Vector3 enemyPos = defObj.transform.position;
        Vector3 combatPos = enemyPos + (attObj.transform.position - enemyPos).normalized * 1.2f;
        combatPos.y = attObj.transform.position.y;

        agent.isStopped = false;
        agent.SetDestination(combatPos);
        anim?.SetFloat(ANIM_VELOCIDAD, 1f);

        // Esperar un frame para que el agente calcule el path y evite el error de GetRemainingDistance
        yield return null;

        // Añadida la validación agent.isOnNavMesh dentro del bucle
        while (attObj != null && agent != null && agent.isOnNavMesh &&
               !agent.pathPending && agent.remainingDistance > agent.stoppingDistance + 0.05f)
        {
            agent.speed = attackerData.FinalSpeed * 3f;
            yield return null;
        }

        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        anim?.SetFloat(ANIM_VELOCIDAD, 0f);

        if (attObj == null || defObj == null) yield break;

        int variacion = UnityEngine.Random.Range(0, 2);
        anim?.SetInteger(ANIM_NUM_ATAQUE, variacion);
        anim?.SetTrigger(ANIM_ATACAR);

        yield return new WaitForSeconds(0.2f);
        if (attObj == null || defObj == null) yield break;

        Vector3 strikePos = combatPos + (enemyPos - combatPos).normalized * 0.4f;
        float t = 0f;
        while (t < 0.08f && attObj != null)
        {
            t += Time.deltaTime;
            attObj.transform.position = Vector3.Lerp(combatPos, strikePos, t / 0.08f);
            yield return null;
        }

        onImpact?.Invoke();
        PlayHitParticles(defObj);

        if (attObj != null) attObj.transform.position = combatPos;
    }

    // ─── MOVIMIENTO RANGED CON NAVMESH (Actualizado) ──────────────────────────
    private IEnumerator NavMeshWalkToRangeAndShoot(
        Unit attackerData, GameObject attObj, GameObject defObj, float range, Action onImpact)
    {
        if (attObj == null || defObj == null) yield break;

        NavMeshAgent agent = attObj.GetComponent<NavMeshAgent>();
        Animator anim = attObj.GetComponent<Animator>();

        if (agent == null || !agent.isOnNavMesh)
        {
            yield return StartCoroutine(FallbackRangedWalk(attObj, defObj, range, onImpact));
            yield break;
        }

        Vector3 enemyPos = defObj.transform.position;
        Vector3 combatPos = enemyPos + (attObj.transform.position - enemyPos).normalized * range;
        combatPos.y = attObj.transform.position.y;

        agent.isStopped = false;
        agent.SetDestination(combatPos);
        anim?.SetFloat(ANIM_VELOCIDAD, 1f);

        // Esperar un frame
        yield return null;

        while (attObj != null && agent != null && agent.isOnNavMesh &&
               !agent.pathPending && agent.remainingDistance > agent.stoppingDistance + 0.05f)
        {
            agent.speed = attackerData.FinalSpeed * 3f;
            yield return null;
        }

        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        anim?.SetFloat(ANIM_VELOCIDAD, 0f);

        if (attObj == null || defObj == null) yield break;
        yield return StartCoroutine(AnimateProjectileWithImpact(attObj, defObj, onImpact));
    }

    // ─── PROYECTIL RANGED ─────────────────────────────────────────────────────
    private IEnumerator AnimateProjectileWithImpact(
        GameObject attObj, GameObject defObj, Action onImpact)
    {
        if (attObj == null || defObj == null) { onImpact?.Invoke(); yield break; }

        Animator anim = attObj.GetComponent<Animator>();
        anim?.SetFloat(ANIM_VELOCIDAD, 0f);
        anim?.SetTrigger(ANIM_ATACAR);

        UnitWorldUI ui = attObj.GetComponent<UnitWorldUI>();
        Vector3 startPos = (ui != null && ui.shootPoint != null)
                           ? ui.shootPoint.position
                           : attObj.transform.position + Vector3.up * 0.8f;
        Vector3 endPos = defObj.transform.position + Vector3.up;

        GameObject bullet = prefabVoidBullet != null
            ? Instantiate(prefabVoidBullet, startPos, Quaternion.identity) : null;
        if (bullet != null) bullet.transform.LookAt(endPos);

        float t = 0f, dur = 0.15f;
        while (t < dur && bullet != null)
        {
            t += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(startPos, endPos, t / dur);
            yield return null;
        }
        if (bullet != null) Destroy(bullet);

        onImpact?.Invoke();
        if (defObj != null) PlayHitParticles(defObj);
    }

    // ─── SOL-9 DISRUPTION BARRAGE ─────────────────────────────────────────────
    private void HandleSOL9ProjectileRequested(Unit caster, Unit target, int projIndex, Action onImpact)
    {
        if (!_visualUnits.TryGetValue(caster, out GameObject casterObj) || casterObj == null) return;
        if (!_visualUnits.TryGetValue(target, out GameObject targetObj) || targetObj == null) return;

        StartCoroutine(FireSOL9Projectile(casterObj, targetObj, target, projIndex * 0.12f, projIndex, onImpact));
    }

    private IEnumerator FireSOL9Projectile(
        GameObject casterObj, GameObject targetObj, Unit targetData,
        float delay, int projIndex, Action onImpact)
    {
        yield return new WaitForSeconds(delay);
        if (casterObj == null || targetObj == null) yield break;

        if (projIndex == 0)
            casterObj.GetComponent<Animator>()?.SetTrigger(ANIM_ATACAR);

        UnitWorldUI ui = casterObj.GetComponent<UnitWorldUI>();
        Vector3 startPos = (ui != null && ui.shootPoint != null)
                           ? ui.shootPoint.position
                           : casterObj.transform.position + Vector3.up * 0.8f;

        Vector3 endPos = targetObj.transform.position + Vector3.up * 0.9f
                       + new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 0,
                                     UnityEngine.Random.Range(-0.15f, 0.15f));

        GameObject bulletPrefab = prefabSollarBullet != null ? prefabSollarBullet : prefabVoidBullet;
        if (bulletPrefab == null) { onImpact?.Invoke(); yield break; }

        GameObject bullet = Instantiate(bulletPrefab, startPos, Quaternion.identity);
        bullet.transform.LookAt(endPos);

        float t = 0f, dur = 0.10f;
        while (t < dur && bullet != null)
        {
            t += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(startPos, endPos, t / dur);
            yield return null;
        }
        if (bullet != null) Destroy(bullet);

        // ── IMPACTO ───────────────────────────────────────────────────────────
        onImpact?.Invoke();

        if (targetObj != null)
        {
            PlayHitParticles(targetObj);
            // Stagger FX en el segundo proyectil (cuando se activa)
            if (projIndex == 1 && prefabStaggerFX != null && targetData.stunTimer > 0)
                Instantiate(prefabStaggerFX, targetObj.transform.position + Vector3.up, Quaternion.identity);
        }
    }

    // ─── SKILL CAST ───────────────────────────────────────────────────────────
    private void HandleUnitSkillCast(Unit caster, CardID skillId)
    {
        if (!_visualUnits.TryGetValue(caster, out GameObject casterObj) || casterObj == null) return;

        Animator anim = casterObj.GetComponent<Animator>();
        anim?.SetTrigger(ANIM_ATACAR);

        switch (skillId)
        {
            case CardID.SollarDuelist: StartCoroutine(AnimateSpin(casterObj.transform)); break;
            case CardID.VoidHeavyShooter: StartCoroutine(AnimateShake(casterObj.transform)); break;
            default: StartCoroutine(AnimatePulse(casterObj.transform)); break;
        }
    }

    // ─── FALLBACKS SIN NAVMESH ────────────────────────────────────────────────
    private IEnumerator FallbackMeleeWalk(GameObject attObj, GameObject defObj, Action onImpact)
    {
        Vector3 enemyPos = defObj.transform.position;
        Vector3 combatPos = enemyPos + (attObj.transform.position - enemyPos).normalized * 1.2f;
        combatPos.y = attObj.transform.position.y;

        while (attObj != null && Vector3.Distance(attObj.transform.position, combatPos) > 0.1f)
        {
            attObj.transform.position = Vector3.MoveTowards(attObj.transform.position, combatPos, 8f * Time.deltaTime);
            yield return null;
        }
        if (attObj == null || defObj == null) yield break;

        attObj.GetComponent<Animator>()?.SetTrigger(ANIM_ATACAR);
        yield return new WaitForSeconds(0.2f);

        Vector3 strikePos = combatPos + (enemyPos - combatPos).normalized * 0.4f;
        float t = 0f;
        while (t < 0.08f && attObj != null)
        {
            t += Time.deltaTime;
            attObj.transform.position = Vector3.Lerp(combatPos, strikePos, t / 0.08f);
            yield return null;
        }
        onImpact?.Invoke();
        PlayHitParticles(defObj);
        if (attObj != null) attObj.transform.position = combatPos;
    }

    private IEnumerator FallbackRangedWalk(
        GameObject attObj, GameObject defObj, float range, Action onImpact)
    {
        Vector3 enemyPos = defObj.transform.position;
        Vector3 combatPos = enemyPos + (attObj.transform.position - enemyPos).normalized * range;
        combatPos.y = attObj.transform.position.y;

        while (attObj != null && Vector3.Distance(attObj.transform.position, combatPos) > 0.1f)
        {
            attObj.transform.position = Vector3.MoveTowards(attObj.transform.position, combatPos, 8f * Time.deltaTime);
            yield return null;
        }
        if (attObj == null || defObj == null) yield break;
        yield return StartCoroutine(AnimateProjectileWithImpact(attObj, defObj, onImpact));
    }

    // ─── PARTÍCULAS DE GOLPE ──────────────────────────────────────────────────
    private void PlayHitParticles(GameObject targetObj)
    {
        if (targetObj == null) return;
        foreach (ParticleSystem ps in targetObj.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.CompareTag("WeaponVFX")) continue;
            ps.Play();
            StartCoroutine(StopParticlesAfterDelay(ps, hitParticlesDuration));
        }
    }

    private IEnumerator StopParticlesAfterDelay(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // ─── ANIMACIONES DE SOPORTE ───────────────────────────────────────────────
    private IEnumerator AnimateSpin(Transform t)
    {
        Vector3 startRot = t.eulerAngles;
        float timer = 0f;
        while (timer < 0.4f && t != null)
        {
            timer += Time.deltaTime;
            t.eulerAngles = new Vector3(startRot.x, startRot.y + Mathf.Lerp(0, 360, timer / 0.4f), startRot.z);
            yield return null;
        }
    }

    private IEnumerator AnimatePulse(Transform t)
    {
        Vector3 orig = t.localScale;
        float timer = 0f;
        while (timer < 0.2f && t != null) { timer += Time.deltaTime; t.localScale = Vector3.Lerp(orig, orig * 1.5f, timer / 0.2f); yield return null; }
        timer = 0f;
        while (timer < 0.2f && t != null) { timer += Time.deltaTime; t.localScale = Vector3.Lerp(orig * 1.5f, orig, timer / 0.2f); yield return null; }
    }

    private IEnumerator AnimateShake(Transform t)
    {
        Vector3 orig = t.position;
        float timer = 0f;
        while (timer < 0.5f && t != null)
        {
            timer += Time.deltaTime;
            t.position = orig + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0, UnityEngine.Random.Range(-0.2f, 0.2f));
            yield return null;
        }
        if (t != null) t.position = orig;
    }
}