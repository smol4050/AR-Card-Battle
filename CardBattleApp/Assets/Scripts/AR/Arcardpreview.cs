using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

/// <summary>
/// Muestra un modelo 3D flotando sobre cada carta física reconocida por AR.
/// — Un modelo por carta rastreada, anclado a la imagen.
/// — Se destruye cuando la imagen pierde el tracking.
/// — Animación de entrada (scale pop) y salida (fade + scale).
/// 
/// Setup en Unity:
///   1. Este componente va en el MISMO GameObject que ARTrackedImageManager.
///   2. Rellena el array cardPreviews con un CardPreviewEntry por cada carta.
///   3. Cada entry: cardId (enum) + prefab del modelo 3D que quieres mostrar.
///   4. offsetY: cuántos metros sube el modelo sobre la carta (prueba con 0.05–0.15).
///   5. previewScale: escala base del modelo (prueba con 0.05 para modelos normales).
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ARCardPreview : MonoBehaviour
{
    // ─── Datos de configuración ───────────────────────────────────────────────
    [System.Serializable]
    public class CardPreviewEntry
    {
        [Tooltip("Valor del enum CardID que corresponde a esta carta.")]
        public CardID cardId;

        [Tooltip("Prefab del modelo 3D a mostrar sobre la carta.")]
        public GameObject previewPrefab;
    }

    [Header("Asigna un entry por cada carta que quieras previsualizar")]
    public CardPreviewEntry[] cardPreviews;

    [Header("Parámetros de posición")]
    [Tooltip("Metros que sube el modelo sobre el plano de la carta.")]
    public float offsetY = 0.08f;

    [Tooltip("Escala base del modelo instanciado.")]
    public float previewScale = 0.06f;

    [Tooltip("Rotación extra sobre el eje Y (grados). Útil si el modelo mira en la dirección incorrecta.")]
    public float rotationOffsetY = 0f;

    [Header("Animación")]
    [Tooltip("Duración del pop de entrada en segundos.")]
    public float popDuration = 0.25f;

    [Tooltip("Duración del fade de salida en segundos.")]
    public float fadeDuration = 0.20f;

    [Tooltip("Si true, el modelo rota lentamente sobre su eje Y (showcase).")]
    public bool autoRotate = true;

    [Tooltip("Grados por segundo de rotación automática.")]
    public float autoRotateSpeed = 45f;

    // ─── Estado interno ───────────────────────────────────────────────────────
    private ARTrackedImageManager _manager;

    // trackableId → instancia del modelo
    private Dictionary<TrackableId, PreviewInstance> _activeInstances
        = new Dictionary<TrackableId, PreviewInstance>();

    // Lookup rápido cardId → prefab
    private Dictionary<CardID, GameObject> _prefabLookup
        = new Dictionary<CardID, GameObject>();

    private class PreviewInstance
    {
        public GameObject go;
        public CardID cardId;
        public float animTimer;
        public AnimState state;
        public Vector3 targetScale;

        public enum AnimState { PopIn, Idle, FadeOut }
    }

    // ─── INIT ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _manager = GetComponent<ARTrackedImageManager>();

        if (cardPreviews == null || cardPreviews.Length == 0)
        {
            Debug.LogWarning("[ARCardPreview] No hay entries en cardPreviews. " +
                             "Asigna al menos uno en el Inspector.");
            return;
        }

        foreach (var entry in cardPreviews)
        {
            if (entry.previewPrefab == null)
            {
                Debug.LogWarning($"[ARCardPreview] Entry {entry.cardId} no tiene prefab asignado.");
                continue;
            }
            _prefabLookup[entry.cardId] = entry.previewPrefab;
        }
    }

    private void OnEnable()
    {
        if (_manager != null)
            _manager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        if (_manager != null)
            _manager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    // ─── AR EVENTS ────────────────────────────────────────────────────────────
    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var img in args.added)
            HandleAdded(img);

        foreach (var img in args.updated)
            HandleUpdated(img);

        foreach (var img in args.removed)
            HandleRemoved(img.Key);
    }

    private void HandleAdded(ARTrackedImage img)
    {
        string imageName = img.referenceImage.name;

        // El tablero no genera preview 3D
        if (imageName == "Battlefield_Marker") return;

        if (!System.Enum.TryParse(imageName, out CardID cardId) || cardId == CardID.None) return;
        if (!_prefabLookup.TryGetValue(cardId, out GameObject prefab)) return;
        if (_activeInstances.ContainsKey(img.trackableId)) return;

        SpawnPreview(img, cardId, prefab);
    }

    private void HandleUpdated(ARTrackedImage img)
    {
        if (!_activeInstances.TryGetValue(img.trackableId, out PreviewInstance inst)) return;

        bool tracking = img.trackingState == TrackingState.Tracking;

        if (tracking)
        {
            // Actualizar posición y rotación para que siga la carta en tiempo real
            inst.go.transform.position = img.transform.position + Vector3.up * offsetY;
            inst.go.transform.rotation = img.transform.rotation
                                       * Quaternion.Euler(0, rotationOffsetY, 0);
            inst.go.SetActive(true);
        }
        else
        {
            // Imagen perdida — ocultar sin destruir (puede volver)
            inst.go.SetActive(false);
        }
    }

    private void HandleRemoved(TrackableId id)
    {
        if (!_activeInstances.TryGetValue(id, out PreviewInstance inst)) return;
        BeginFadeOut(inst);
        _activeInstances.Remove(id);
    }

    // ─── SPAWN ────────────────────────────────────────────────────────────────
    private void SpawnPreview(ARTrackedImage img, CardID cardId, GameObject prefab)
    {
        Vector3 spawnPos = img.transform.position + Vector3.up * offsetY;
        Quaternion spawnRot = img.transform.rotation * Quaternion.Euler(0, rotationOffsetY, 0);

        GameObject go = Instantiate(prefab, spawnPos, spawnRot);
        go.transform.localScale = Vector3.zero;  // empieza invisible para el pop

        var inst = new PreviewInstance
        {
            go = go,
            cardId = cardId,
            animTimer = 0f,
            state = PreviewInstance.AnimState.PopIn,
            targetScale = Vector3.one * previewScale,
        };

        _activeInstances[img.trackableId] = inst;

        Debug.Log($"[ARCardPreview] Preview spawneado para <color=yellow>{cardId}</color>");
    }

    // ─── UPDATE — animaciones y auto-rotate ───────────────────────────────────
    private void Update()
    {
        //List<PreviewInstance> toRemove = null;

        foreach (var kv in _activeInstances)
        {
            var inst = kv.Value;
            if (inst.go == null) continue;

            switch (inst.state)
            {
                case PreviewInstance.AnimState.PopIn:
                    inst.animTimer += Time.deltaTime;
                    float popT = Mathf.Clamp01(inst.animTimer / popDuration);
                    // Ease out cubic con leve overshoot (bounce)
                    float popScale = EaseOutBack(popT);
                    inst.go.transform.localScale = inst.targetScale * popScale;
                    if (popT >= 1f) inst.state = PreviewInstance.AnimState.Idle;
                    break;

                case PreviewInstance.AnimState.Idle:
                    if (autoRotate)
                        inst.go.transform.Rotate(Vector3.up, autoRotateSpeed * Time.deltaTime, Space.Self);
                    break;
            }
        }
    }

    // ─── FADE OUT (cuando la imagen se pierde definitivamente) ────────────────
    private void BeginFadeOut(PreviewInstance inst)
    {
        if (inst.go == null) return;
        StartCoroutine(FadeOutCoroutine(inst.go));
    }

    private System.Collections.IEnumerator FadeOutCoroutine(GameObject go)
    {
        Vector3 startScale = go.transform.localScale;
        float elapsed = 0f;

        while (elapsed < fadeDuration && go != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            go.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ─── EASING ───────────────────────────────────────────────────────────────
    /// <summary>Ease-out con leve overshoot para un pop natural.</summary>
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}