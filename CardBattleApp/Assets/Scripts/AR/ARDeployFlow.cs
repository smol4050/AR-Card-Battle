using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona el flujo de despliegue del jugador 0 (Sollar) en AR.
///
/// FLUJO:
///  1. Jugador escanea una carta  → aparece panel de confirmación.
///  2. Jugador pulsa Confirmar    → entra en modo "esperando slot".
///  3. Jugador toca un SlotCollider aliado → la unidad se instancia allí.
///     Si ya había una unidad previa de esa carta (pendiente de recolocar),
///     se elimina del slot anterior y se mueve al nuevo.
///     El jugador puede seguir tocando otros slots para cambiarla de lugar.
///  4. Para fijar la unidad y pasar a la siguiente, el jugador escanea
///     otra carta (o pulsa "Listo" para terminar).
///  5. Al pulsar "Listo": la IA despliega y comienza la batalla.
/// </summary>
public class ARDeployFlow : MonoBehaviour
{
    // ─── Referencias de sistema ───────────────────────────────────────────────
    [Header("Referencias de sistema")]
    public GameManager gameManager;
    public ARCardScanner scanner;
    public TutorialAIController aiController;

    // ─── UI — Confirmación de carta ───────────────────────────────────────────
    [Header("UI — Confirmación de carta")]
    public GameObject confirmPanel;
    public TextMeshProUGUI cardNameText;

    // ─── UI — Estado general ──────────────────────────────────────────────────
    [Header("UI — Estado")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI logText;
    public Button readyButton;

    // ─── UI — Energía ─────────────────────────────────────────────────────────
    [Header("UI — Energía del jugador")]
    public TextMeshProUGUI playerEnergyText;

    // ─── Capas de raycast ─────────────────────────────────────────────────────
    [Header("Raycast")]
    [Tooltip("LayerMask que incluya la capa donde están los SlotColliders.")]
    public LayerMask slotLayerMask = ~0;   // por defecto, todo; ajusta en Inspector

    // ─── Estado interno ───────────────────────────────────────────────────────
    private CardID _pendingCard = CardID.None;
    private bool _isWaitingForSlot = false;
    private bool _battlefieldReady = false;
    private bool _playerDeclaredReady = false;

    // Última unidad colocada que aún puede recolocarse (antes de escanear otra carta)
    private int _lastPlacedSlot = -1;
    private CardID _lastPlacedCardId = CardID.None;

    // Cartas permitidas para el jugador 0 (Sollar)
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
        if (gameManager != null) gameManager.OnLogMessage += AppendLog;
    }

    private void OnDisable()
    {
        scanner.OnCardScanned -= HandleCardScanned;
        scanner.OnBattlefieldSpawned -= HandleBattlefieldSpawned;
        if (gameManager != null) gameManager.OnLogMessage -= AppendLog;
    }

    // ─── TABLERO LISTO ────────────────────────────────────────────────────────
    private void HandleBattlefieldSpawned(BattlefieldReferences board)
    {
        _battlefieldReady = true;
        gameManager.InitializeGame();
        gameManager.players[0].energy = 10;

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
        if (_playerDeclaredReady) return;

        // Carta del bando incorrecto
        if (!IsSollarCard(cardId))
        {
            SetStatus($"<color=red>{cardId} es del Vacío. Solo puedes usar cartas Sollar.</color>");
            return;
        }

        // Si hay una carta esperando slot, ignoramos el re-escaneo
        // (el jugador debe tocar un slot primero o cancelar)
        if (_isWaitingForSlot)
        {
            SetStatus("Toca un slot del tablero para colocar la unidad pendiente, o cancela primero.");
            return;
        }

        // Al escanear una carta nueva, la unidad anterior queda fijada en su slot
        _lastPlacedSlot = -1;
        _lastPlacedCardId = CardID.None;

        _pendingCard = cardId;
        int cost = GetCardCost(cardId);

        if (cardNameText != null)
            cardNameText.text = $"¿Desplegar {cardId}?\nCosto: {cost} Energía";

        if (confirmPanel != null) confirmPanel.SetActive(true);
        SetStatus($"Confirma el despliegue de {cardId} (costo: {cost}).");
    }

    private bool IsSollarCard(CardID id)
    {
        foreach (var c in _allowedSollarCards) if (c == id) return true;
        return false;
    }

    private int GetCardCost(CardID id)
    {
        if (id == CardID.SollarForce) return 2;
        if (id == CardID.SolarVanguard) return 3;
        return 1;
    }

    // ─── CONFIRMACIÓN ────────────────────────────────────────────────────────
    public void OnConfirmDeploy()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        _isWaitingForSlot = true;
        SetStatus("Toca una casilla aliada en el tablero para colocar la unidad.");
    }

    public void OnCancelDeploy()
    {
        _pendingCard = CardID.None;
        _isWaitingForSlot = false;
        _lastPlacedSlot = -1;
        _lastPlacedCardId = CardID.None;
        if (confirmPanel != null) confirmPanel.SetActive(false);
        SetStatus("Escanea una carta para desplegarla.");
    }

    // ─── BOTÓN LISTO ─────────────────────────────────────────────────────────
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

        // Cancelar flujo pendiente sin eliminar unidades ya colocadas
        _pendingCard = CardID.None;
        _isWaitingForSlot = false;
        _lastPlacedSlot = -1;
        _lastPlacedCardId = CardID.None;
        if (confirmPanel != null) confirmPanel.SetActive(false);

        SetStatus("Esperando despliegue de la IA...");
        AppendLog(-1, "<color=yellow>Jugador 0 listo. Analizando estrategia de Mahoraga...</color>");

        aiController.ExecuteDecisionLogic();

        gameManager.SetPlayerReady(0);
        gameManager.SetPlayerReady(1);

        SetStatus("<color=red>¡GUERRA DECLARADA! El combate ha comenzado.</color>");
    }

    // ─── DETECCIÓN DE TOQUE EN SLOT ───────────────────────────────────────────
    private void Update()
    {
        if (!_isWaitingForSlot || _pendingCard == CardID.None) return;

#if UNITY_EDITOR
        // En el editor usamos el ratón para pruebas
        if (Input.GetMouseButtonDown(0))
            TryRaycast(Input.mousePosition);
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryRaycast(Input.GetTouch(0).position);
#endif
    }

    private void TryRaycast(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // Intentamos primero con la layer mask específica
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, 100f, slotLayerMask);

        // Fallback: raycast contra todo si no hay layer configurada
        if (!hit) hit = Physics.Raycast(ray, out hitInfo, 100f);

        if (!hit) return;

        SlotCollider slot = hitInfo.collider.GetComponent<SlotCollider>();
        if (slot == null)
        {
            // Buscar en el padre por si el collider está en un hijo
            slot = hitInfo.collider.GetComponentInParent<SlotCollider>();
        }

        if (slot != null) TryPlaceOnSlot(slot);
    }

    // ─── COLOCAR / MOVER UNIDAD EN SLOT ──────────────────────────────────────
    private void TryPlaceOnSlot(SlotCollider slot)
    {
        // SolarVanguard solo en combate
        if (_pendingCard == CardID.SolarVanguard)
        {
            SetStatus("<color=orange>Solar Vanguard se invoca durante el combate, no en preparación.</color>");
            OnCancelDeploy();
            return;
        }

        int cost = GetCardCost(_pendingCard);

        // Si el jugador ya colocó esta unidad antes (mismo escaneo) en otro slot,
        // la quitamos del slot anterior antes de intentar ponerla en el nuevo.
        bool wasPlacedBefore = _lastPlacedSlot != -1 && _lastPlacedCardId == _pendingCard;

        if (wasPlacedBefore && _lastPlacedSlot == slot.slotIndex)
        {
            // Tocó el mismo slot donde ya está → confirmar posición (sin hacer nada)
            SetStatus($"<color=lime>{_pendingCard} ya está en este slot. Escanea otra carta o pulsa Listo.</color>");
            return;
        }

        if (wasPlacedBefore)
        {
            // Eliminar del slot anterior para "moverla"
            RemoveUnitFromSlot(0, _lastPlacedSlot);
            // Devolvemos la energía ya que se va a recolocar
            gameManager.players[0].energy += cost;
        }

        // Verificar que el slot destino esté libre
        bool slotOccupied = gameManager.activeUnits[0] != null &&
                            gameManager.activeUnits[0].Exists(u => u.slotIndex == slot.slotIndex && !u.IsDead);

        if (slotOccupied)
        {
            // Si el slot está ocupado por OTRA unidad diferente, no podemos mover aquí
            SetStatus("<color=red>Ese slot ya está ocupado por otra unidad.</color>");

            // Revertir: volver a colocar en el slot anterior si existía
            if (wasPlacedBefore)
            {
                gameManager.players[0].energy -= cost; // quitamos la energía que devolvimos
                // Intentar recolocar en slot anterior
                TrySpawnAtSlot(_pendingCard, _lastPlacedSlot, cost);
            }
            return;
        }

        // Intentar colocar en el nuevo slot
        bool success = TrySpawnAtSlot(_pendingCard, slot.slotIndex, cost);

        if (success)
        {
            _lastPlacedSlot = slot.slotIndex;
            _lastPlacedCardId = _pendingCard;
            RefreshEnergyUI();
            SetStatus($"<color=lime>{_pendingCard} en slot {slot.slotIndex}. " +
                      $"Toca otro slot para moverla, escanea otra carta, o pulsa Listo.</color>");
        }
        else
        {
            int energy = gameManager.players[0].energy;
            if (energy < cost)
                SetStatus($"<color=red>Energía insuficiente ({energy}/{cost}). Pulsa Listo para comenzar.</color>");
            else
                SetStatus("<color=red>No se pudo colocar la unidad. Intenta otro slot.</color>");
        }
    }

    /// <summary>
    /// Intenta instanciar la carta en el slot dado usando el Transform del tablero AR.
    /// Retorna true si GameManager.PlayCard tuvo éxito.
    /// </summary>
    private bool TrySpawnAtSlot(CardID cardId, int slotIndex, int cost)
    {
        if (scanner.BoardRefs == null)
        {
            Debug.LogError("[ARDeployFlow] BoardRefs es null. El tablero no está registrado.");
            return false;
        }

        int row = slotIndex < 3 ? 0 : 1;
        Transform slotTransform = scanner.BoardRefs.GetSlot(0, row, slotIndex);

        if (slotTransform == null)
        {
            Debug.LogError($"[ARDeployFlow] Transform del slot {slotIndex} es null.");
            return false;
        }

        // Posición lógica basada en el Transform del slot
        BoxCollider col = slotTransform.GetComponent<BoxCollider>();
        Vector2 logicalPos = col != null
            ? new Vector2(slotTransform.localPosition.x + col.center.x,
                          slotTransform.localPosition.z + col.center.z)
            : new Vector2(slotTransform.localPosition.x, slotTransform.localPosition.z);

        bool success = gameManager.PlayCard(0, cardId, logicalPos, row, slotIndex, cost);

        if (success)
            AppendLog(0, $"<color=lime>{cardId} colocada en slot {slotIndex} (row {row}).</color>");

        return success;
    }

    /// <summary>
    /// Elimina físicamente una unidad del equipo 0 en el slotIndex indicado
    /// y dispara OnUnitDied para que VisualController destruya el GO.
    /// </summary>
    private void RemoveUnitFromSlot(int playerId, int slotIndex)
    {
        if (gameManager.activeUnits[playerId] == null) return;

        for (int i = gameManager.activeUnits[playerId].Count - 1; i >= 0; i--)
        {
            Unit u = gameManager.activeUnits[playerId][i];
            if (u.slotIndex == slotIndex && !u.IsDead)
            {
                // Marcamos como muerta para que VisualController la retire
                u.currentHp = 0;
                // Disparamos el evento de muerte visual
                gameManager.OnUnitDied_Internal(playerId, u);
                gameManager.activeUnits[playerId].RemoveAt(i);
                AppendLog(playerId, $"<color=yellow>{u.cardId} retirada del slot {slotIndex} para recolocar.</color>");
                break;
            }
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

        string[] lines = logText.text.Split('\n');
        if (lines.Length > 30)
            logText.text = string.Join("\n", lines, lines.Length - 30, 30);
    }

    private void RefreshEnergyUI()
    {
        if (playerEnergyText != null &&
            gameManager?.players != null &&
            gameManager.players[0] != null)
            playerEnergyText.text = $"Energía: {gameManager.players[0].energy}";
    }
}