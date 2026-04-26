using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;
using Unity.Netcode;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARCardScanner1 : MonoBehaviour
{
    [Header("Configuración de Gameplay")]
    public GameObject battlefieldNetworkPrefab;

    public event Action<CardID> OnCardScanned;
    public event Action<BattlefieldReferences> OnBattlefieldSpawned;

    private ARTrackedImageManager _imageManager;
    private bool _isNetcodeActive = false;
    private GameObject _spawnedBoard;
    private BattlefieldReferences _boardRefs;
    public BattlefieldReferences BoardRefs => _boardRefs;

    private void Awake()
    {
        _imageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        // Compatibilidad con AR Foundation 6.0
        if (_imageManager != null)
            _imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        if (_imageManager != null)
            _imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    public void SetNetworkReady()
    {
        _isNetcodeActive = true;
        Debug.Log("<color=cyan>[Scanner] Red activa. Esperando imágenes de gameplay.</color>");
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        if (!_isNetcodeActive) return;

        foreach (var img in args.added) ProcessImage(img);
        foreach (var img in args.updated)
        {
            if (img.trackingState == TrackingState.Tracking)
                ProcessImage(img);
        }
    }

    private void ProcessImage(ARTrackedImage img)
    {
        string imageName = img.referenceImage.name;

        if (imageName == "Battlefield_Marker" && _spawnedBoard == null)
        {
            HandleBattlefieldSpawning(img);
        }
        else if (Enum.TryParse(imageName, out CardID cardId) && cardId != CardID.None)
        {
            OnCardScanned?.Invoke(cardId);
        }
    }

    public void SetBoardRefsManually(BattlefieldReferences refs)
    {
        _boardRefs = refs;
        // Notificar a los flujos de despliegue que el tablero ya existe
        OnBattlefieldSpawned?.Invoke(refs);
    }

    private void HandleBattlefieldSpawning(ARTrackedImage img)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            _spawnedBoard = Instantiate(battlefieldNetworkPrefab, img.transform.position, img.transform.rotation);
            _spawnedBoard.GetComponent<NetworkObject>().Spawn();

            _boardRefs = _spawnedBoard.GetComponent<BattlefieldReferences>();
            OnBattlefieldSpawned?.Invoke(_boardRefs);
            Debug.Log("<color=green>[Net] Tablero spawneado por el Host.</color>");
        }
    }
}