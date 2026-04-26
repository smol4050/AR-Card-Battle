using Niantic.Lightship.SharedAR.Colocalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(SharedSpaceFactory))]
public class NetworkColocalizationManager : MonoBehaviour, IColocalizationRoom
{
    [Header("Lightship — Imagen Ancla")]
    [SerializeField] private Texture2D targetTexture;
    [SerializeField] private float targetWidthPhysicalSize = 0.15f;

    [Header("UI de Conexión")]
    [SerializeField] private GameObject connectionUI;
    [SerializeField] private TMP_InputField pinInputField;
    [SerializeField] private TextMeshProUGUI pinText;

    [Header("Referencia al Scanner")]
    [SerializeField] private ARCardScanner1 mainCardScanner;

    private SharedSpaceFactory _sharedSpaceFactory;
    private SharedSpaceManager _sharedSpaceManager;
    private bool _startAsHost;

    private void OnEnable()
    {
        _sharedSpaceFactory = GetComponent<SharedSpaceFactory>();
        _sharedSpaceFactory.OnSharedSpaceTracking += SharedSpaceStartTracking;
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

    private void OnManagerStateChanged(SharedSpaceManager.SharedSpaceManagerStateChangeEventArgs args)
    {
        string status = args.Tracking ? "Alineado ✓" : "Buscando imagen ancla...";
        Debug.Log($"[LIGHTSHIP] Estado: {status}");

        if (pinText != null && pinText.gameObject.activeSelf)
        {
            string roomName = (pinInputField != null && !string.IsNullOrEmpty(pinInputField.text)) ? pinInputField.text : "0000";
            pinText.text = $"SALA: {roomName}\nEstado: {status}";
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

        if (targetTexture == null)
        {
            Debug.LogError("[NetworkColocalizationManager] No se asignó targetTexture.");
            return;
        }

        var imageTrackingOptions = ISharedSpaceTrackingOptions.CreateImageTrackingOptions(
            targetTexture, targetWidthPhysicalSize);

        _sharedSpaceFactory.CreateRoom(imageTrackingOptions, roomName);

        if (connectionUI != null) connectionUI.SetActive(false);
        if (pinText != null) pinText.gameObject.SetActive(true);
    }

    public void SharedSpaceStartTracking()
    {
        if (pinText != null) pinText.text = "¡Mundo Sincronizado!";

        // Iniciamos Netcode
        if (_startAsHost) NetworkManager.Singleton.StartHost();
        else NetworkManager.Singleton.StartClient();

        // Activamos el scanner de cartas solo cuando la red está lista
        if (mainCardScanner != null)
        {
            mainCardScanner.SetNetworkReady();
            Debug.Log("<color=green>[Network] Colocalización exitosa. Scanner habilitado.</color>");
        }

        Invoke(nameof(HideStatusText), 2f);
    }

    private void HideStatusText()
    {
        if (pinText != null) pinText.gameObject.SetActive(false);
    }

    public void StartHost() => NetworkManager.Singleton?.StartHost();
    public void StartClient() => NetworkManager.Singleton?.StartClient();
}