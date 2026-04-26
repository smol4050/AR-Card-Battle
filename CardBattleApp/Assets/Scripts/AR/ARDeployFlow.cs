using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Gestiona el despliegue del jugador 0 (Sollar) en AR.
/// </summary>
public class ARDeployFlow : MonoBehaviour
{
    [Header("Referencias de sistema")]
    public GameManager gameManager;
    public ARCardScanner scanner;
    public TutorialAIController aiController;

    [Header("UI — Confirmación de carta")]
    public GameObject confirmPanel;
    public TextMeshProUGUI cardNameText;

    [Header("UI — Estado")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI logText;
    public Button readyButton;

    [Header("UI — Energía del jugador")]
    public TextMeshProUGUI playerEnergyText;

    [Header("Raycast")]
    public LayerMask slotLayerMask = ~0;

    [Header("Cooldown de escaneo (segundos)")]
    public float scanCooldown = 2f;

    // ─── EVENTOS PÚBLICOS PARA TutorialFlowController ─────────────────────────
    public event Action OnBoardReady;      // paso 1: tablero detectado
    public event Action OnCardConfirmed;   // paso 2: carta escaneada y confirmada
    public event Action OnUnitPlaced;      // paso 3: unidad colocada en slot
    public event Action OnPlayerReady;     // ready pulsado → batalla

    // ─── Estado interno ───────────────────────────────────────────────────────
    private CardID _pendingCard = CardID.None;
    private bool _isWaitingForSlot = false;
    private bool _battlefieldReady = false;
    private bool _playerDeclaredReady = false;

    private int _lastPlacedSlot = -1;
    private CardID _lastPlacedCardId = CardID.None;

    private Dictionary<CardID, float> _lastScanTime = new Dictionary<CardID, float>();

    private static readonly CardID[] _allowedSollarCards =
    {
        CardID.SollarDuelist, CardID.SollarForce,
        CardID.SollarCommander, CardID.SolarVanguard,
    };

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (gameManager == null) Debug.LogError("[ARDeployFlow] GameManager no asignado.");
        if (scanner == null) Debug.LogError("[ARDeployFlow] ARCardScanner no asignado.");
        if (aiController == null) Debug.LogError("[ARDeployFlow] TutorialAIController no asignado.");

        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (readyButton != null) readyButton.interactable = false;

        SetStatus("Escanea el marcador del tablero para comenzar.");
    }

    private void OnEnable()
    {
        if (scanner != null)
        {
            scanner.OnBattlefieldSpawned += HandleBattlefieldSpawned;
            scanner.OnCardScanned += HandleCardScanned;
        }
        if (gameManager != null) gameManager.OnLogMessage += AppendLog;
    }

    private void OnDisable()
    {
        if (scanner != null)
        {
            scanner.OnBattlefieldSpawned -= HandleBattlefieldSpawned;
            scanner.OnCardScanned -= HandleCardScanned;
        }
        if (gameManager != null) gameManager.OnLogMessage -= AppendLog;
    }

    // ─── GESTIÓN VISUAL DE UI ─────────────────────────────────────────────────
    private void ToggleMainUI(bool isVisible)
    {
        if (statusText != null) statusText.gameObject.SetActive(isVisible);
        if (logText != null) logText.gameObject.SetActive(isVisible);
        if (playerEnergyText != null) playerEnergyText.gameObject.SetActive(isVisible);
        if (readyButton != null) readyButton.gameObject.SetActive(isVisible);
    }

    // ─── TABLERO LISTO ────────────────────────────────────────────────────────
    private void HandleBattlefieldSpawned(BattlefieldReferences board)
    {
        if (_battlefieldReady) return;

        _battlefieldReady = true;
        gameManager.InitializeGame();
        gameManager.players[0].energy = 10;

        if (readyButton != null) readyButton.interactable = true;
        SetStatus("Tablero listo. Escanea tus cartas Sollar.");
        RefreshEnergyUI();

        OnBoardReady?.Invoke();
    }

    // ─── ESCANEO DE CARTA ─────────────────────────────────────────────────────
    private void HandleCardScanned(CardID cardId)
    {
        if (!_battlefieldReady) { SetStatus("Primero escanea el tablero."); return; }
        if (_playerDeclaredReady) return;
        if (!IsSollarCard(cardId))
        {
            SetStatus($"<color=red>{cardId} es del Vacío. Solo cartas Sollar.</color>");
            return;
        }

        float now = Time.time;
        if (_lastScanTime.TryGetValue(cardId, out float last) && now - last < scanCooldown) return;
        _lastScanTime[cardId] = now;

        if (_isWaitingForSlot)
        {
            SetStatus("Toca un slot primero, o cancela la carta actual.");
            return;
        }

        _lastPlacedSlot = -1;
        _lastPlacedCardId = CardID.None;
        _pendingCard = cardId;
        int cost = GetCardCost(cardId);

        if (cardNameText != null) cardNameText.text = $"¿Desplegar {cardId}?\nCosto: {cost} Energía";

        // Mostrar panel de confirmación y ocultar la UI principal
        if (confirmPanel != null) confirmPanel.SetActive(true);
        ToggleMainUI(false);
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

    // ─── CONFIRMACIÓN Y CANCELACIÓN ───────────────────────────────────────────
    public void OnConfirmDeploy()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        _isWaitingForSlot = true;

        // Recuperar la UI principal
        ToggleMainUI(true);
        ToggleSlotHighlights(true);

        SetStatus("Toca una casilla aliada.");
        OnCardConfirmed?.Invoke();
    }

    public void OnCancelDeploy()
    {
        _pendingCard = CardID.None;
        _isWaitingForSlot = false;
        _lastPlacedSlot = -1;
        _lastPlacedCardId = CardID.None;

        if (confirmPanel != null) confirmPanel.SetActive(false);

        // Recuperar la UI principal
        ToggleMainUI(true);
        SetStatus("Escanea una carta para desplegarla.");
    }

    // ─── BOTÓN LISTO ──────────────────────────────────────────────────────────
    public void OnReadyClicked()
    {
        if (_playerDeclaredReady) return;
        if (!_battlefieldReady) { SetStatus("<color=red>Primero escanea el tablero.</color>"); return; }

        _playerDeclaredReady = true;
        if (readyButton != null) readyButton.interactable = false;
        if (confirmPanel != null) confirmPanel.SetActive(false);

        ToggleMainUI(true); // Asegurar que log y status estén visibles para la batalla

        _pendingCard = CardID.None; _isWaitingForSlot = false;
        _lastPlacedSlot = -1; _lastPlacedCardId = CardID.None;

        SetStatus("Esperando despliegue de la IA...");
        AppendLog(-1, "<color=yellow>Jugador 0 listo. Mahoraga analiza...</color>");

        aiController.ExecuteDecisionLogic();
        gameManager.SetPlayerReady(0);
        gameManager.SetPlayerReady(1);

        SetStatus("<color=red>¡GUERRA DECLARADA!</color>");
        OnPlayerReady?.Invoke();
    }

    // ─── RAYCAST ─────────────────────────────────────────────────────────────
    private void Update()
    {
        if (!_isWaitingForSlot || _pendingCard == CardID.None) return;

        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(
            Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1))
        {
            return;
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) TryRaycast(Input.mousePosition);
#else
    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        TryRaycast(Input.GetTouch(0).position);
#endif
    }

    private void TryRaycast(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        bool hit = Physics.Raycast(ray, out RaycastHit info, 100f, slotLayerMask);
        if (!hit) hit = Physics.Raycast(ray, out info, 100f);
        if (!hit) { Debug.Log("[ARDeployFlow] Raycast sin impacto."); return; }

        SlotCollider slot = info.collider.GetComponent<SlotCollider>()
                         ?? info.collider.GetComponentInParent<SlotCollider>();

        if (slot != null) TryPlaceOnSlot(slot);
    }

    // ─── COLOCAR / MOVER ──────────────────────────────────────────────────────
    private void TryPlaceOnSlot(SlotCollider slot)
    {
        if (_pendingCard == CardID.SolarVanguard)
        {
            SetStatus("<color=orange>Solar Vanguard solo en combate.</color>");
            OnCancelDeploy(); return;
        }

        int cost = GetCardCost(_pendingCard);
        bool wasPlaced = _lastPlacedSlot != -1 && _lastPlacedCardId == _pendingCard;

        if (wasPlaced && _lastPlacedSlot == slot.slotIndex)
        {
            SetStatus($"<color=lime>{_pendingCard} ya está aquí. Escanea otra o pulsa Listo.</color>");
            return;
        }

        if (wasPlaced)
        {
            RemoveUnitFromSlot(0, _lastPlacedSlot);
            gameManager.players[0].energy += cost;
        }

        bool occupied = gameManager.activeUnits[0] != null &&
                        gameManager.activeUnits[0].Exists(u => u.slotIndex == slot.slotIndex && !u.IsDead);

        if (occupied)
        {
            SetStatus("<color=red>Slot ocupado por otra unidad.</color>");
            if (wasPlaced) { gameManager.players[0].energy -= cost; TrySpawnAtSlot(_pendingCard, _lastPlacedSlot, cost); }
            return;
        }

        if (TrySpawnAtSlot(_pendingCard, slot.slotIndex, cost))
        {
            _lastPlacedSlot = slot.slotIndex;
            _lastPlacedCardId = _pendingCard;
            RefreshEnergyUI();
            SetStatus($"<color=lime>{_pendingCard} en slot {slot.slotIndex}. Toca otro para mover o pulsa Listo.</color>");

            ToggleSlotHighlights(false);
            OnUnitPlaced?.Invoke();
        }
        else
        {
            int e = gameManager.players[0].energy;
            SetStatus(e < cost
                ? $"<color=red>Energía insuficiente ({e}/{cost}).</color>"
                : "<color=red>No se pudo colocar. Intenta otro slot.</color>");
        }
    }

    private void ToggleSlotHighlights(bool active)
    {
        if (scanner == null || scanner.BoardRefs == null) return;
        foreach (var slot in scanner.BoardRefs.p0FrontSlots)
            slot.GetComponent<SlotHighlighter>()?.SetHighlight(active);
        foreach (var slot in scanner.BoardRefs.p0BackSlots)
            slot.GetComponent<SlotHighlighter>()?.SetHighlight(active);
    }

    private bool TrySpawnAtSlot(CardID cardId, int slotIndex, int cost)
    {
        if (scanner.BoardRefs == null) return false;

        int row = slotIndex < 3 ? 0 : 1;
        Transform t = scanner.BoardRefs.GetSlot(0, row, slotIndex);
        if (t == null) return false;

        BoxCollider col = t.GetComponent<BoxCollider>();
        Vector2 pos = col != null
            ? new Vector2(t.localPosition.x + col.center.x, t.localPosition.z + col.center.z)
            : new Vector2(t.localPosition.x, t.localPosition.z);

        bool ok = gameManager.PlayCard(0, cardId, pos, row, slotIndex, cost);
        if (ok) AppendLog(0, $"<color=lime>{cardId} → slot {slotIndex} (row {row}).</color>");
        return ok;
    }

    private void RemoveUnitFromSlot(int playerId, int slotIndex)
    {
        if (gameManager.activeUnits[playerId] == null) return;
        for (int i = gameManager.activeUnits[playerId].Count - 1; i >= 0; i--)
        {
            Unit u = gameManager.activeUnits[playerId][i];
            if (u.slotIndex != slotIndex || u.IsDead) continue;
            u.currentHp = 0;
            gameManager.OnUnitDied_Internal(playerId, u);
            gameManager.activeUnits[playerId].RemoveAt(i);
            break;
        }
    }

    // ─── HELPERS UI ───────────────────────────────────────────────────────────
    private void SetStatus(string msg) { if (statusText != null) statusText.text = msg; }

    private void AppendLog(int pid, string msg)
    {
        if (logText == null) return;
        string pre = pid switch { 0 => "[Sollar] ", 1 => "[Vacío] ", -1 => "[Sistema] ", _ => "" };
        logText.text += $"\n{pre}{msg}";
        string[] lines = logText.text.Split('\n');
        if (lines.Length > 30) logText.text = string.Join("\n", lines, lines.Length - 30, 30);
    }

    private void RefreshEnergyUI()
    {
        if (playerEnergyText != null && gameManager?.players?[0] != null)
            playerEnergyText.text = $"Energía: {gameManager.players[0].energy}";
    }
}