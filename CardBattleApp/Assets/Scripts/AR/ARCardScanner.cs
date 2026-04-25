using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARCardScanner : MonoBehaviour
{
    private ARTrackedImageManager _imageManager;
    public ARPlacementManager placementManager; 

    // Evento para avisarle a la UI qué carta se escaneó
    public event Action<CardID> OnCardScanned;

    private void Awake() => _imageManager = GetComponent<ARTrackedImageManager>();

    // ACTUALIZACIÓN UNITY 6: Usar trackablesChanged en lugar de trackedImagesChanged
    private void OnEnable() => _imageManager.trackablesChanged += OnTrackablesChanged;
    private void OnDisable() => _imageManager.trackablesChanged -= OnTrackablesChanged;

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        // Imágenes detectadas por primera vez
        foreach (var trackedImage in args.added)
        {
            ProcessScannedImage(trackedImage);
        }
        
        // Imágenes que el sistema sigue viendo o recuperó el tracking
        foreach (var trackedImage in args.updated)
        {
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                ProcessScannedImage(trackedImage);
        }
    }

    private void ProcessScannedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // 1. ¿Es el marcador del tablero?
        if (imageName == "Battlefield_Marker" && !placementManager.isPlaced)
        {
            placementManager.PlaceBattlefield(trackedImage.transform.position, trackedImage.transform.rotation);
            return;
        }

        // 2. ¿Es una carta de unidad? Mapeamos el string de la imagen al Enum CardID
        if (Enum.TryParse(imageName, out CardID scannedCard))
        {
            OnCardScanned?.Invoke(scannedCard);
        }
    }
}