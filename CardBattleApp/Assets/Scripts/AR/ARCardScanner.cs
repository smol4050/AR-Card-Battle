using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;

/// <summary>
/// Escanea imagenes AR y despacha eventos.
/// - "Battlefield_Marker" instancia el tablero.
/// - Cualquier nombre que coincida con un CardID emite OnCardScanned.
/// - ARCardPreview, si existe en la escena, escucha el mismo ARTrackedImageManager.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ARCardScanner : MonoBehaviour
{
    [Header("Tablero")]
    [Tooltip("Prefab que contiene BattlefieldReferences y todos los slots.")]
    public GameObject battlefieldPrefab;

    [Header("Debug")]
    [Tooltip("Imprime en consola cada imagen detectada.")]
    public bool verboseLog = true;

    public event Action<CardID> OnCardScanned;
    public event Action<BattlefieldReferences> OnBattlefieldSpawned;

    private ARTrackedImageManager _imageManager;
    private GameObject _spawnedBattlefield;
    private BattlefieldReferences _boardRefs;

    public bool IsBattlefieldReady => _boardRefs != null;
    public BattlefieldReferences BoardRefs => _boardRefs;

    private void Awake()
    {
        _imageManager = GetComponent<ARTrackedImageManager>();

        if (_imageManager == null)
        {
            Debug.LogError("[ARCardScanner] ARTrackedImageManager no encontrado. Anadelo al mismo GameObject.");
            return;
        }

        ValidateReferenceLibrary();

        if (battlefieldPrefab == null)
            Debug.LogError("[ARCardScanner] battlefieldPrefab no asignado en el Inspector.");
        else if (battlefieldPrefab.GetComponent<BattlefieldReferences>() == null)
            Debug.LogError("[ARCardScanner] battlefieldPrefab no tiene BattlefieldReferences. Anade el componente al prefab.");
    }

    private void OnEnable()
    {
        if (_imageManager == null)
            _imageManager = GetComponent<ARTrackedImageManager>();

        if (_imageManager != null)
        {
            ValidateReferenceLibrary();
            _imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }
    }

    private void OnDisable()
    {
        if (_imageManager != null)
            _imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void ValidateReferenceLibrary()
    {
        XRReferenceImageLibrary library = _imageManager.referenceLibrary as XRReferenceImageLibrary;

        if (library == null)
        {
            Debug.Log("[ARCardScanner] ARTrackedImageManager no tiene Reference Image Library serializada. Se espera una libreria runtime.");
            return;
        }

        bool hasBattlefieldMarker = false;
        int validGameImages = 0;

        for (int i = 0; i < library.count; i++)
        {
            XRReferenceImage image = library[i];
            string imageName = image.name;

            if (image.specifySize && image.size.x <= 0f)
                Debug.LogWarning($"[ARCardScanner] La imagen '{imageName}' tiene ancho fisico 0.");

            if (image.specifySize && image.size.y <= 0f)
                Debug.LogWarning($"[ARCardScanner] La imagen '{imageName}' tiene alto fisico 0.");

            if (imageName == "Battlefield_Marker")
            {
                hasBattlefieldMarker = true;
                validGameImages++;
            }
            else if (Enum.TryParse(imageName, out CardID cardId) && cardId != CardID.None)
            {
                validGameImages++;
            }
            else
            {
                Debug.LogWarning($"[ARCardScanner] La imagen '{imageName}' no coincide con Battlefield_Marker ni con CardID.");
            }
        }

        if (!hasBattlefieldMarker)
            Debug.LogError("[ARCardScanner] La libreria AR no contiene una imagen llamada exactamente 'Battlefield_Marker'.");

        if (validGameImages == 0)
            Debug.LogError("[ARCardScanner] La libreria AR no tiene imagenes reconocibles para el juego.");
        else if (verboseLog)
            Debug.Log($"[ARCardScanner] Libreria AR cargada: {library.count} imagen(es), {validGameImages} utiles para el juego.");
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var img in args.added)
            ProcessScannedImage(img);

        foreach (var img in args.updated)
            if (img.trackingState == TrackingState.Tracking)
                ProcessScannedImage(img);
    }

    private void ProcessScannedImage(ARTrackedImage trackedImage)
    {
        ProcessScannedImageString(
            trackedImage.referenceImage.name,
            trackedImage.transform.position,
            trackedImage.transform.rotation);
    }

    public void ProcessScannedImageString(string imageName, Vector3 position, Quaternion rotation)
    {
        if (verboseLog)
            Debug.Log($"[ARCardScanner] Detectado: '<color=cyan>{imageName}</color>'");

        if (imageName == "Battlefield_Marker")
        {
            HandleBattlefieldMarker(position, rotation);

            return;
        }

        if (Enum.TryParse(imageName, out CardID scannedCard) && scannedCard != CardID.None)
        {
            if (verboseLog)
                Debug.Log($"[ARCardScanner] Carta valida: <color=yellow>{scannedCard}</color>");
            OnCardScanned?.Invoke(scannedCard);
        }
        else
        {
            Debug.LogWarning($"[ARCardScanner] '{imageName}' no es ni tablero ni carta valida.");
        }
    }

    private void HandleBattlefieldMarker(Vector3 position, Quaternion rotation)
    {
        if (_spawnedBattlefield == null)
        {
            if (battlefieldPrefab == null)
            {
                Debug.LogError("[ARCardScanner] battlefieldPrefab es null.");
                return;
            }

            _spawnedBattlefield = Instantiate(battlefieldPrefab, position, rotation);
            _boardRefs = _spawnedBattlefield.GetComponent<BattlefieldReferences>();

            if (_boardRefs == null)
            {
                Debug.LogError("[ARCardScanner] El prefab instanciado no tiene BattlefieldReferences.");
                return;
            }

            VisualController vc = FindAnyObjectByType<VisualController>();
            if (vc != null)
                vc.RegisterBattlefield(_boardRefs);
            else
                Debug.LogWarning("[ARCardScanner] VisualController no encontrado en la escena.");

            TutorialAIController ai = FindAnyObjectByType<TutorialAIController>();
            if (ai != null)
                ai.RegisterDynamicBoard(_boardRefs);
            else
                Debug.LogWarning("[ARCardScanner] TutorialAIController no encontrado en la escena.");

            OnBattlefieldSpawned?.Invoke(_boardRefs);
            Debug.Log("<color=green>[ARCardScanner] Tablero instanciado y registrado.</color>");
        }
        else
        {
            _spawnedBattlefield.transform.SetPositionAndRotation(position, rotation);
        }
    }

    public void ResetBattlefield()
    {
        if (_spawnedBattlefield != null) Destroy(_spawnedBattlefield);
        _spawnedBattlefield = null;
        _boardRefs = null;
    }
}
