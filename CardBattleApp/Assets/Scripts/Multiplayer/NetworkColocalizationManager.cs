using Niantic.Lightship.SharedAR.Colocalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(SharedSpaceFactory))]
public class NetworkColocalizationManager : MonoBehaviour, IColocalizationRoom
{
    [Header("Lightship Target (QR o Imagen Ancla)")]
    [SerializeField] private Texture2D targetTexture;
    [SerializeField] private float targetWidthPhysicalSize = 0.15f;

    [Header("UI de Conexión")]
    [SerializeField] private GameObject connectionUI;
    [SerializeField] private TMP_InputField pinInputField;
    [SerializeField] private TextMeshProUGUI pinText;

    [Header("Nuestros Sistemas")]
    [SerializeField] private ARCardScanner mainCardScanner;
    [SerializeField] private ARTrackedImageManager trackedImageManager;

    private SharedSpaceFactory _sharedSpaceFactory;
    private SharedSpaceManager _sharedSpaceManager;
    private bool _startAsHost;

    private void OnEnable()
    {
        _sharedSpaceFactory = GetComponent<SharedSpaceFactory>();
        _sharedSpaceFactory.OnSharedSpaceTracking += SharedSpaceStartTracking;
        if (mainCardScanner != null) mainCardScanner.enabled = false;
    }

    private void OnDisable()
    {
        if (_sharedSpaceFactory != null)
            _sharedSpaceFactory.OnSharedSpaceTracking -= SharedSpaceStartTracking;

        if (_sharedSpaceManager != null)
            _sharedSpaceManager.sharedSpaceManagerStateChanged -= OnManagerStateChanged;
    }

    private void Start()
    {
        if (connectionUI != null) connectionUI.SetActive(true);

        _sharedSpaceManager = FindAnyObjectByType<SharedSpaceManager>();
        if (_sharedSpaceManager != null)
            _sharedSpaceManager.sharedSpaceManagerStateChanged += OnManagerStateChanged;
    }

    // ─── FIX: Eliminada la propiedad .Connected que no existe en ARDK 3.x ───
    private void OnManagerStateChanged(SharedSpaceManager.SharedSpaceManagerStateChangeEventArgs args)
    {
        // En ARDK 3.x, 'Tracking' es el indicador de que la colocalización funcionó
        string trackingStatus = args.Tracking ? "Alineado" : "Buscando QR...";

        Debug.Log($"[LIGHTSHIP] Estado de Tracking: {trackingStatus}");

        if (pinText != null && pinText.gameObject.activeSelf)
        {
            pinText.text = $"SALA: {pinInputField.text}\nEstado: {trackingStatus}";
        }
    }

    public void StartNetworkAsHostOrClient(bool isHost)
    {
        if (pinText != null) pinText.text = "Iniciando Colocalización...";
        _startAsHost = isHost;
        CreateRoomOptions();
    }

    public void CreateRoomOptions()
    {
        string roomName = (pinInputField != null && !string.IsNullOrEmpty(pinInputField.text))
            ? pinInputField.text : "0000";

        if (targetTexture == null) return;

        var imageTrackingOptions = ISharedSpaceTrackingOptions.CreateImageTrackingOptions(
            targetTexture, targetWidthPhysicalSize);

        _sharedSpaceFactory.CreateRoom(imageTrackingOptions, roomName);

        if (connectionUI != null) connectionUI.SetActive(false);
        if (pinText != null) pinText.gameObject.SetActive(true);
    }

    public void SharedSpaceStartTracking()
    {
        if (pinText != null) pinText.text = "¡Mundo Sincronizado!";

        if (_startAsHost) StartHost();
        else StartClient();

        if (mainCardScanner != null)
        {
            mainCardScanner.enabled = true;
            Invoke(nameof(HideStatusText), 2f);
        }
    }

    private void HideStatusText() => pinText.gameObject.SetActive(false);
    public void StartHost() => NetworkManager.Singleton?.StartHost();
    public void StartClient() => NetworkManager.Singleton?.StartClient();
}