using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
public class NetworkGameManager : MonoBehaviourPunCallbacks
{
    [Header("UI - Lobby")]
    [Tooltip("El panel que contiene el InputField y los botones de Host/Join")]
    public GameObject lobbyPanel;
    public TMP_InputField roomIdInput;
    public TextMeshProUGUI statusText;
    public Button createButton;
    public Button joinButton;

    [Header("UI - Flujos de Juego (AR)")]
    [Tooltip("El GameObject Padre que contiene el ARDeployFlowP1 y su UI")]
    public GameObject p1SollarLogic;
    [Tooltip("El GameObject Padre que contiene el ARDeployFlowP2 y su UI")]
    public GameObject p2VoidLogic;

    [Header("Referencias de Juego")]
    public GameManager gameManager;

    private int _localPlayerId;

    private void Start()
    {
        // 1. Estado inicial: Mostrar Lobby, ocultar UI de combate
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (p1SollarLogic != null) p1SollarLogic.SetActive(false);
        if (p2VoidLogic != null) p2VoidLogic.SetActive(false);

        createButton.interactable = false;
        joinButton.interactable = false;
        statusText.text = "Conectando a Photon...";

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Autenticando...";
        // Forzamos unirse al lobby general para estabilizar el estado antes de crear salas
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        // Solo habilitamos botones cuando Photon está 100% listo
        statusText.text = "Conectado. Ingresa ID de sala.";
        createButton.interactable = true;
        joinButton.interactable = true;
    }

    public void OnClickCreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;
        string id = roomIdInput.text;
        if (string.IsNullOrEmpty(id)) { statusText.text = "ID Inválido"; return; }

        statusText.text = "Creando sala...";
        createButton.interactable = false;
        joinButton.interactable = false;

        PhotonNetwork.CreateRoom(id, new RoomOptions { MaxPlayers = 2 });
    }

    public void OnClickJoinRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;
        string id = roomIdInput.text;
        if (string.IsNullOrEmpty(id)) { statusText.text = "ID Inválido"; return; }

        statusText.text = "Uniéndose...";
        createButton.interactable = false;
        joinButton.interactable = false;

        PhotonNetwork.JoinRoom(id);
    }

    public override void OnJoinedRoom()
    {
        _localPlayerId = PhotonNetwork.IsMasterClient ? 0 : 1;

        // 2. Ocultar el Lobby completamente
        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        // 3. Activar solo el flujo y UI del jugador correspondiente
        if (_localPlayerId == 0)
        {
            if (p1SollarLogic != null) p1SollarLogic.SetActive(true);
        }
        else
        {
            if (p2VoidLogic != null) p2VoidLogic.SetActive(true);
        }

        // Iniciar la lógica del juego
        gameManager.InitializeGame();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Error al crear: " + message;
        createButton.interactable = true;
        joinButton.interactable = true;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "Error al unirse: " + message;
        createButton.interactable = true;
        joinButton.interactable = true;
    }

    // ─── LÓGICA DE SINCRONIZACIÓN ───
    public void SendPlacement(int cardId, int slotIndex)
    {
        photonView.RPC(nameof(RPC_SyncCardPlacement), RpcTarget.Others, _localPlayerId, cardId, slotIndex);
    }

    public void SendReady()
    {
        photonView.RPC(nameof(RPC_SyncReady), RpcTarget.All, _localPlayerId);
    }

    [PunRPC]
    public void RPC_SyncCardPlacement(int remotePlayerId, int cardIdInt, int slotIndex)
    {
        gameManager.ExecuteRemotePlay(remotePlayerId, (CardID)cardIdInt, slotIndex);
    }

    [PunRPC]
    public void RPC_SyncReady(int playerId)
    {
        gameManager.SetPlayerReady(playerId);
    }
}