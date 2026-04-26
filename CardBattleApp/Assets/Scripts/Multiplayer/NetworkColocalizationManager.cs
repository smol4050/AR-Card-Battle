using Niantic.Lightship.SharedAR.Colocalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SharedSpaceFactory))]
public class NetworkColocalizationManager : MonoBehaviour, IColocalizationRoom
{
    [Header("Lightship Target (QR o Imagen Ancla)")]
    [SerializeField] private Texture2D targetImage;
    [SerializeField] private float targetWidthPhysicalSize;

    [Header("UI de Conexión")]
    [SerializeField] private GameObject connectionUI;
    [SerializeField] private TMP_InputField pinInputField;
    [SerializeField] private TextMeshProUGUI pinText;

    [Header("Nuestros Sistemas")]
    [Tooltip("El escáner real del juego que procesa Battlefield y Cartas")]
    [SerializeField] private ARCardScanner mainCardScanner;

    private SharedSpaceFactory _sharedSpaceFactory;
    private bool _startAsHost;

    private void OnEnable()
    {
        _sharedSpaceFactory = GetComponent<SharedSpaceFactory>();
        _sharedSpaceFactory.OnSharedSpaceTracking += SharedSpaceStartTracking;

        // El escáner principal no debe buscar cartas de juego hasta que los mundos estén alineados.
        if (mainCardScanner != null) mainCardScanner.enabled = false;
    }

    private void OnDisable()
    {
        if (_sharedSpaceFactory != null)
            _sharedSpaceFactory.OnSharedSpaceTracking -= SharedSpaceStartTracking;
    }

    private void Start()
    {
        if (connectionUI != null) connectionUI.SetActive(true);
        if (pinText != null) pinText.gameObject.SetActive(false);
    }

    public void StartNetworkAsHostOrClient(bool isHost)
    {
        _startAsHost = isHost;
        CreateRoomOptions();
    }

    public void CreateRoomOptions()
    {
        string roomName = pinInputField != null && !string.IsNullOrEmpty(pinInputField.text)
            ? pinInputField.text
            : "0000";

        var imageTrackingOptions = ISharedSpaceTrackingOptions.CreateImageTrackingOptions(targetImage, targetWidthPhysicalSize);
        _sharedSpaceFactory.CreateRoom(imageTrackingOptions, roomName);

        // Actualización de UI
        if (connectionUI != null) connectionUI.SetActive(false);
        if (pinText != null)
        {
            pinText.gameObject.SetActive(true);
            pinText.text = $"SALA: {roomName}\nBuscando ancla AR...";
        }
    }

    public void SharedSpaceStartTracking()
    {
        if (pinText != null) pinText.text = "¡Mundos alineados! Conectando...";

        if (_startAsHost)
            StartHost();
        else
            StartClient();

        // ── AQUÍ CONECTAMOS CON NUESTRO JUEGO ──
        if (mainCardScanner != null)
        {
            mainCardScanner.enabled = true;
            Debug.Log("<color=lime>[Multiplayer] Colocalización exitosa. ARCardScanner activado.</color>");
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.StartHost();
        Debug.Log("<color=cyan>[Multiplayer] Iniciado como HOST.</color>");
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.StartClient();
        Debug.Log("<color=cyan>[Multiplayer] Iniciado como CLIENTE.</color>");
    }
}