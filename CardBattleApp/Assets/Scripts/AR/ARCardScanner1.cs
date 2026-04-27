using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARCardScanner1 : MonoBehaviour
{
    [Header("Configuración de Gameplay")]
    [Tooltip("El prefab del tablero que contiene BattlefieldReferences")]
    public GameObject battlefieldPrefab;

    [Tooltip("Multiplicador de tamaño (1.0 = normal, 0.1 = diez veces más pequeño)")]
    public float boardScale = 0.1f;

    public event Action<CardID> OnCardScanned;
    public event Action<BattlefieldReferences> OnBattlefieldSpawned;

    private ARTrackedImageManager _imageManager;
    private GameObject _spawnedBoard;
    private BattlefieldReferences _boardRefs;

    public BattlefieldReferences BoardRefs => _boardRefs;
    public bool IsBattlefieldReady => _boardRefs != null;

    private void Awake()
    {
        _imageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        if (_imageManager != null)
            _imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        if (_imageManager != null)
            _imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
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

        // Si detecta el tablero y no existe uno previamente
        if (imageName == "Battlefield_Marker" && _spawnedBoard == null)
        {
            HandleBattlefieldSpawning(img.transform.position, img.transform.rotation);
        }
        else if (Enum.TryParse(imageName, out CardID cardId) && cardId != CardID.None)
        {
            OnCardScanned?.Invoke(cardId);
        }
    }

    private void HandleBattlefieldSpawning(Vector3 position, Quaternion rotation)
    {
        if (battlefieldPrefab == null)
        {
            Debug.LogError("[ARCardScanner1] Prefab del tablero no asignado.");
            return;
        }

        // Instanciación puramente local
        _spawnedBoard = Instantiate(battlefieldPrefab, position, rotation);

        //_spawnedBoard.transform.localScale = Vector3.one * boardScale;

        _boardRefs = _spawnedBoard.GetComponent<BattlefieldReferences>();

        // Conexión crítica: El VisualController necesita saber dónde está el tablero
        VisualController vc = FindAnyObjectByType<VisualController>();
        if (vc != null)
        {
            vc.RegisterBattlefield(_boardRefs);
        }

        OnBattlefieldSpawned?.Invoke(_boardRefs);
        Debug.Log("<color=green>[AR] Tablero instanciado localmente para el multijugador.</color>");
    }

    // Usado por ARBoardPlacer si el usuario decide colocar el tablero manualmente
    public void SetBoardRefsManually(BattlefieldReferences refs)
    {
        _boardRefs = refs;
        OnBattlefieldSpawned?.Invoke(refs);
    }
}