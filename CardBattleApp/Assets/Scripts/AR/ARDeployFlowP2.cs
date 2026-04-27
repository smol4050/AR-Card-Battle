using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class ARDeployFlowP2 : MonoBehaviour
{
    [Header("Referencias")]
    public GameManager gameManager;
    public ARCardScanner1 scanner;

    [Header("UI")]
    public GameObject confirmPanel;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI statusText;
    public Button readyButton;
    public TextMeshProUGUI playerEnergyText;

    [Header("Ajustes")]
    public LayerMask slotLayerMask = ~0;
    public float scanCooldown = 2f;

    private CardID _pendingCard = CardID.None;
    private bool _isWaitingForSlot = false;
    private bool _battlefieldReady = false;
    private bool _playerDeclaredReady = false;
    private Dictionary<CardID, float> _lastScanTime = new Dictionary<CardID, float>();

    private static readonly CardID[] _allowedVoidCards = {
        CardID.VoidHorde, CardID.VoidCommander, CardID.VoidHeavyShooter, CardID.AbyssReaper
    };

    private void Awake()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (readyButton != null) readyButton.interactable = false;
        SetStatus("Esperando tablero...");
    }

    private void OnEnable()
    {
        if (scanner != null)
        {
            scanner.OnBattlefieldSpawned += HandleBoard;
            scanner.OnCardScanned += HandleCard;
        }
    }

    private void OnDisable()
    {
        if (scanner != null)
        {
            scanner.OnBattlefieldSpawned -= HandleBoard;
            scanner.OnCardScanned -= HandleCard;
        }
    }

    private void HandleBoard(BattlefieldReferences board)
    {
        if (_battlefieldReady) return;
        _battlefieldReady = true;
        gameManager.players[1].energy = 10;
        readyButton.interactable = true;
        SetStatus("Tablero detectado. Escanea cartas Void.");
        RefreshEnergy();
    }

    private void HandleCard(CardID id)
    {
        if (!_battlefieldReady || _playerDeclaredReady || !IsVoid(id)) return;

        float now = Time.time;
        if (_lastScanTime.TryGetValue(id, out float last) && now - last < scanCooldown) return;
        _lastScanTime[id] = now;

        _pendingCard = id;
        if (cardNameText != null) cardNameText.text = $"¿Desplegar {id}?\nCosto: {GetCost(id)}";
        confirmPanel.SetActive(true);
    }

    public void OnConfirm()
    {
        confirmPanel.SetActive(false);
        _isWaitingForSlot = true;
        SetStatus("Toca una casilla Void.");
    }

    public void OnReady()
    {
        _playerDeclaredReady = true;
        if (readyButton != null) readyButton.interactable = false;

        // EL CAMBIO CRÍTICO: Avisamos a la red usando el sistema de Photon
        NetworkGameManager netManager = FindAnyObjectByType<NetworkGameManager>();
        if (netManager != null)
        {
            // El RPC_SyncReady del NetworkGameManager ya se encarga de usar RpcTarget.All,
            // por lo que actualizará el gameManager local y el remoto al mismo tiempo.
            netManager.SendReady();
        }

        SetStatus("Listo. Esperando oponente...");
    }

    private void Update()
    {
        if (!_isWaitingForSlot || _pendingCard == CardID.None) return;
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, slotLayerMask))
            {
                SlotCollider slot = hit.collider.GetComponent<SlotCollider>();
                if (slot != null) TryPlace(slot);
            }
        }
    }

    private void TryPlace(SlotCollider slot)
    {
        int cost = GetCost(_pendingCard);
        int row = slot.slotIndex < 3 ? 0 : 1;
        Transform t = scanner.BoardRefs.GetSlot(1, row, slot.slotIndex);
        Vector2 pos = new Vector2(t.localPosition.x, t.localPosition.z);

        if (gameManager.PlayCard(1, _pendingCard, pos, row, slot.slotIndex, cost))
        {
            FindAnyObjectByType<NetworkGameManager>().SendPlacement((int)_pendingCard, slot.slotIndex);

            RefreshEnergy();
            _isWaitingForSlot = false;
            SetStatus("Unidad Void colocada.");
        }
    }

    private bool IsVoid(CardID id) { foreach (var c in _allowedVoidCards) if (c == id) return true; return false; }
    private int GetCost(CardID id) => (id == CardID.AbyssReaper) ? 3 : 1;
    private void SetStatus(string m) => statusText.text = m;
    private void RefreshEnergy() => playerEnergyText.text = $"Energía: {gameManager.players[1].energy}";
}