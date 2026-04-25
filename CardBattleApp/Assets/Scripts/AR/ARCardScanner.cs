using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARCardScanner : MonoBehaviour
{
    private ARTrackedImageManager _imageManager;

    [Header("Configuración del Tablero")]
    public GameObject battlefieldPrefab;
    private GameObject spawnedBattlefield;

    // Evento para avisarle a la UI (ARDeployFlow) qué carta se escaneó
    public event Action<CardID> OnCardScanned;

    private void Awake() => _imageManager = GetComponent<ARTrackedImageManager>();

    // SOLUCIÓN UNITY 6: Usar la sintaxis de UnityEvent (AddListener/RemoveListener)
    private void OnEnable() => _imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    private void OnDisable() => _imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var trackedImage in args.added)
        {
            ProcessScannedImage(trackedImage);
        }

        foreach (var trackedImage in args.updated)
        {
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                ProcessScannedImage(trackedImage);
        }
    }

    private void ProcessScannedImage(ARTrackedImage trackedImage)
    {
        // Extraemos los datos puros y se los pasamos a la capa de lógica de negocio
        string imageName = trackedImage.referenceImage.name;
        ProcessScannedImageString(imageName, trackedImage.transform.position, trackedImage.transform.rotation);
    }

    /// <summary>
    /// Método desacoplado del hardware AR para permitir Pruebas Unitarias.
    /// </summary>
    public void ProcessScannedImageString(string imageName, Vector3 position, Quaternion rotation)
    {
        if (imageName == "Battlefield_Marker")
        {
            if (spawnedBattlefield == null)
            {
                spawnedBattlefield = Instantiate(battlefieldPrefab, position, rotation);

                // ¡NUEVA LÍNEA! Inyectamos las referencias dinámicas al VisualController
                FindAnyObjectByType<VisualController>().RegisterBattlefield(spawnedBattlefield.GetComponent<BattlefieldReferences>());

                Debug.Log("<color=green>Tablero desplegado con éxito en el marcador.</color>");
            }
            else
            {
                spawnedBattlefield.transform.position = position;
                spawnedBattlefield.transform.rotation = rotation;
            }
            return;
        }

        // 2. Escanear cartas de unidad (Convierte el nombre de la imagen al Enum)
        if (Enum.TryParse(imageName, out CardID scannedCard))
        {
            OnCardScanned?.Invoke(scannedCard);
        }
    }
}