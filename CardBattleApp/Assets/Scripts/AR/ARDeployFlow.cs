using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona el flujo de despliegue del jugador 0 (Sollar) en AR:
///  1. Espera a que el tablero esté listo.
///  2. Permite escanear y colocar cartas Sollar en los slots.
///  3. Cuando el jugador pulsa "Listo", la IA despliega su bando y comienza la batalla.
/// </summary>
public class ARDeployFlow : MonoBehaviour
{
    [Header("Referencias de sistema")]
    public GameManager gameManager;
    public ARCardScanner scanner;
    public TutorialAIController aiController;

    // ─── UI — Panel de confirmación de carta ──────────────────────────────────
    [Header("UI — Confirmación de carta")]
    public GameObject confirmPanel;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI energyText;

    // ─── UI — Estado general ──────────────────────────────────────────────────
    [Header("UI — Estado")]
    public TextMeshProUGUI statusText;        // "Escanea el tablero", "Despliega tus tropas", etc.
    public TextMeshProUGUI logText;           // Logs del GameManager en tiempo real
    public Button readyButton;       // Botón "¡Listo! / Iniciar batalla"

    [Header("UI — Energía del jugador")]
    public TextMeshProUGUI playerEnergyText;  // Muestra energía actual / máxima

    // ─── Estado interno ───────────────────────────────────────────────────────
    private CardID _pendingCard = CardID.None;
    private bool _isWaitingForSlot = false;
    private bool _battlefieldReady = false;
    private bool _playerDeclaredReady = false;

    // Cartas que el jugador 0 (Sollar) puede usar — Void excluido explícitamente
    private static readonly CardID[] _allowedSollarCards =
    {
        CardID.SollarDuelist,
        CardID.SollarForce,
        CardID.SollarCommander,
        CardID.SolarVanguard,
    };

    // ─── INICIALIZACIÓN ───────────────────────────────────────────────────────
    private void Awake()
    {
        // Validaciones de Inspector
        if (gameManager == null) Debug.LogError("[ARDeployFlow] GameManager no asignado.");
        if (scanner == null) Debug.LogError("[ARDeployFlow] ARCardScanner no asignado.");
        if (aiController == null) Debug.LogError("[ARDeployFlow] TutorialAIController no asignado.");
        if (readyButton == null) Debug.LogWarning("[ARDeployFlow] ReadyButton no asignado.");

        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (readyButton != null) readyButton.interactable = false;

        SetStatus("Escanea el marcador del tablero para comenzar.");
    }

    private void OnEnable()
    {
        scanner.OnCardScanned += HandleCardScanned;
        scanner.OnBattlefieldSpawned += HandleBattlefieldSpawned;

        if (gameManager != null)
            gameManager.OnLogMessage += AppendLog;
    }

    private void OnDisable()
    {
        scanner.OnCardScanned -= HandleCardScanned;
        scanner.OnBattlefieldSpawned -= HandleBattlefieldSpawned;

        if (gameManager != null)
            gameManager.OnLogMessage -= AppendLog;
    }

    // ─── TABLERO ──────────────────────────────────────────────────────────────
    private void HandleBattlefieldSpawned(BattlefieldReferences board)
    {
        _battlefieldReady = true;

        // Inicializar la partida una sola vez
        gameManager.InitializeGame();

        // Dar energía inicial al jugador 0
        gameManager.players[0].energy = 10;

        // Activar botón "Listo"
        if (readyButton != null) readyButton.interactable = true;

        SetStatus("Tablero listo. Escanea tus cartas Sollar y colócalas en el tablero.");
        RefreshEnergyUI();

        Debug.Log("[ARDeployFlow] Tablero registrado. Fase de despliegue activa.");
    }

    // ─── ESCANEO DE CARTA ─────────────────────────────────────────────────────
    private void HandleCardScanned(CardID cardId)
    {
        if (!_battlefieldReady)
        {
            SetStatus("Primero escanea el tablero.");
            return;
        }

        if (_playerDeclaredReady)
        {
            // El jugador ya está listo — ignorar escaneos
            return;
        }

        // Filtramos cartas Void: el jugador 0 es Sollar
        if (!IsSollarCard(cardId))
        {
            SetStatus($"<color=red>{cardId} pertenece al Vacío. Solo puedes usar cartas Sollar.</color>");
            AppendLog(-1, $"Intento de escanear {cardId} (Void): bloqueado.");
            return;
        }

        // Evitar doble escaneo mientras hay una carta pendiente
        if (_pendingCard != CardID.None || _isWaitingForSlot) return;

        _pendingCard = cardId;
        int cost = GetCardCost(cardId);

        if (cardNameText != null)
            cardNameText.text = $"¿Desplegar {cardId}?\nCosto: {cost} Energía";

        if (confirmPanel != null) confirmPanel.SetActive(true);
        SetStatus($"Confirma el despliegue de {cardId}.");
    }

    private bool IsSollarCard(CardID id)
    {
        foreach (var allowed in _allowedSollarCards)
            if (allowed == id) return true;
        return false;
    }

    private int GetCardCost(CardID id)
    {
        if (id == CardID.SollarForce) return 2;
        if (id == CardID.SolarVanguard) return 3;
        return 1;
    }

    // ─── BOTONES DE CONFIRMACIÓN ─────────────────────────────────────────────
    /// <summary>El jugador confirma que quiere colocar la carta escaneada.</summary>
    public void OnConfirmDeploy()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        _isWaitingForSlot = true;
        SetStatus("Toca una casilla aliada en el tablero para colocar la unidad.");
    }

    /// <summary>El jugador cancela el despliegue de la carta escaneada.</summary>
    public void OnCancelDeploy()
    {
        _pendingCard = CardID.None;
        _isWaitingForSlot = false;
        if (confirmPanel != null) confirmPanel.SetActive(false);
        SetStatus("Escanea una carta para desplegarla.");
    }

    // ─── BOTÓN LISTO ─────────────────────────────────────────────────────────
    /// <summary>
    /// El jugador declara que terminó de desplegar.
    /// La IA despliega su bando y ambos jugadores declaran "listo".
    /// </summary>
    public void OnReadyClicked()
    {
        if (_playerDeclaredReady) return;
        if (!_battlefieldReady)
        {
            SetStatus("<color=red>Primero escanea el tablero.</color>");
            return;
        }

        _playerDeclaredReady = true;
        if (readyButton != null) readyButton.interactable = false;

        // Cancelar cualquier flujo de carta pendiente
        OnCancelDeploy();

        SetStatus("Esperando despliegue de la IA...");
        AppendLog(-1, "<color=yellow>Jugador 0 listo. Analizando estrategia de Mahoraga...</color>");

        // La IA despliega su ejército (jugador 1)
        aiController.ExecuteDecisionLogic();

        // Ambos bandos listos → comienza la batalla
        gameManager.SetPlayerReady(0);
        gameManager.SetPlayerReady(1);

        SetStatus("<color=red>¡GUERRA DECLARADA! El combate ha comenzado.</color>");
    }

    // ─── TOQUE EN SLOT (selección de casilla) ─────────────────────────────────
    private void Update()
    {
        if (!_isWaitingForSlot) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        Ray ray = Camera.main.ScreenPointToRay(touch.position);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        SlotCollider slot = hit.collider.GetComponent<SlotCollider>();
        if (slot != null) TrySpawnCard(slot);
    }

    private void TrySpawnCard(SlotCollider slot)
    {
        if (_pendingCard == CardID.None) return;

        int cost = GetCardCost(_pendingCard);

        // SolarVanguard solo se puede usar en combate (el GameManager lo valida)
        // pero avisamos al jugador antes para mejor UX
        if (_pendingCard == CardID.SolarVanguard)
        {
            SetStatus("<color=orange>Solar Vanguard se invoca durante el combate, no en preparación.</color>");
            OnCancelDeploy();
            return;
        }

        Vector2 logicalPos = new Vector2(slot.transform.position.x, slot.transform.position.z);
        bool success = gameManager.PlayCard(0, _pendingCard, logicalPos, slot.row, slot.slotIndex, cost);

        if (success)
        {
            AppendLog(0, $"<color=lime>{_pendingCard} colocado en slot {slot.slotIndex}.</color>");
            _pendingCard = CardID.None;
            _isWaitingForSlot = false;
            RefreshEnergyUI();
            SetStatus($"Unidad desplegada. Energía restante: {gameManager.players[0].energy}. Escanea otra carta o pulsa Listo.");
        }
        else
        {
            int currentEnergy = gameManager.players[0].energy;
            if (currentEnergy < cost)
                SetStatus($"<color=red>Energía insuficiente ({currentEnergy}/{cost}). Pulsa Listo para comenzar.</color>");
            else
                SetStatus("<color=red>Movimiento inválido. El slot puede estar ocupado.</color>");

            AppendLog(0, $"<color=red>No se pudo colocar {_pendingCard} en slot {slot.slotIndex}.</color>");
        }
    }

    // ─── HELPERS DE UI ────────────────────────────────────────────────────────
    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private void AppendLog(int playerId, string msg)
    {
        if (logText == null) return;

        string prefix = playerId switch
        {
            0 => "[Sollar] ",
            1 => "[Vacío] ",
            -1 => "[Sistema] ",
            _ => ""
        };

        logText.text += $"\n{prefix}{msg}";

        // Evitar que el log crezca sin límite (mantener los últimos ~30 mensajes)
        string[] lines = logText.text.Split('\n');
        if (lines.Length > 30)
            logText.text = string.Join("\n", lines, lines.Length - 30, 30);
    }

    private void RefreshEnergyUI()
    {
        if (playerEnergyText != null && gameManager?.players != null && gameManager.players[0] != null)
            playerEnergyText.text = $"Energía: {gameManager.players[0].energy}";
    }
}