using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using Photon.Realtime;
using TMPro;
using UnityEngine;

/// <summary>
/// Gestiona la conexión remota y la sincronización de cartas por slots.
/// </summary>
public class RemoteGameManager : MonoBehaviourPunCallbacks
{
    [Header("UI de Red")]
    public TextMeshProUGUI statusText;
    public string roomName = "UAO_Battle_Room";
    public GameManager gameManager;
    [Header("Referencias de Juego")]
    public ARDeployFlowP1 playerFlow; // O el flujo unificado

    private void Awake()
    {
        if (gameManager == null)
            gameManager = Object.FindAnyObjectByType<GameManager>();
    }

    private void Start()
    {
        ConnectToPhoton();
    }

    private void ConnectToPhoton()
    {
        statusText.text = "Conectando a Photon...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Buscando oponente...";
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        statusText.text = $"En Sala: {roomName} | Jugador: {PhotonNetwork.LocalPlayer.ActorNumber}";
        // Aquí habilitamos el flujo de despliegue
        playerFlow.gameObject.SetActive(true);
    }

    /// <summary>
    /// Sincroniza el despliegue de una carta en un slot específico.
    /// </summary>
    [PunRPC]
    public void RPC_SyncCardPlacement(int playerId, int cardIdInt, int slotIndex)
    {
        CardID cardId = (CardID)cardIdInt;
        Debug.Log($"[Sync] Jugador {playerId} puso {cardId} en slot {slotIndex}");

        // El oponente ve la carta aparecer en SU tablero local en el slot correspondiente
        if (PhotonNetwork.LocalPlayer.ActorNumber != playerId)
        {
            // Lógica visual para spawnear la carta en el tablero del oponente
            gameManager.ExecuteRemotePlay(playerId, (CardID)cardIdInt, slotIndex);
        }
    }
}