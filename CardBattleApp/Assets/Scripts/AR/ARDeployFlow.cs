using UnityEngine;
using TMPro;

public class ARDeployFlow : MonoBehaviour
{
    public GameManager gameManager;
    public ARCardScanner scanner;

    [Header("UI Elements")]
    public GameObject confirmPanel;
    public TextMeshProUGUI cardNameText;

    private CardID _pendingCard = CardID.None;
    private bool _isWaitingForSlotSelection = false;

    private void OnEnable() => scanner.OnCardScanned += HandleCardScanned;
    private void OnDisable() => scanner.OnCardScanned -= HandleCardScanned;

    private void HandleCardScanned(CardID cardId)
    {
        // Si ya estamos a la mitad de una decisión, ignoramos nuevos escaneos
        if (_pendingCard != CardID.None || _isWaitingForSlotSelection) return;

        // Mostrar UI
        _pendingCard = cardId;
        cardNameText.text = $"¿Desplegar {cardId}?\nCosto: {(cardId == CardID.SollarForce ? 2 : 1)} Energía";
        confirmPanel.SetActive(true);
    }

    // Botón: CONFIRMAR (El jugador dice "Sí, quiero usarla")
    public void OnConfirmDeploy()
    {
        confirmPanel.SetActive(false);
        _isWaitingForSlotSelection = true;
        gameManager.LogMessage(0, "<color=cyan>Toca una casilla aliada en el tablero.</color>");
    }

    // Botón: CANCELAR (El jugador se arrepiente)
    public void OnCancelDeploy()
    {
        _pendingCard = CardID.None;
        _isWaitingForSlotSelection = false;
        confirmPanel.SetActive(false);
    }

    private void Update()
    {
        // Fase 3: Detectar el toque en la casilla del tablero 3D
        if (_isWaitingForSlotSelection && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Tus casillas deben tener un collider y un script o tag identificador
                    SlotCollider slot = hit.collider.GetComponent<SlotCollider>();
                    if (slot != null)
                    {
                        TrySpawnCard(slot);
                    }
                }
            }
        }
    }

    private void TrySpawnCard(SlotCollider slot)
    {
        int cost = (_pendingCard == CardID.SollarForce) ? 2 : 1;
        Vector2 logicalPos = new Vector2(slot.transform.position.x, slot.transform.position.z);

        // Intentar jugar la carta a través del GameManager
        bool success = gameManager.PlayCard(0, _pendingCard, logicalPos, slot.row, slot.slotIndex, cost);

        if (success)
        {
            // Resetear el flujo para permitir escanear la siguiente carta
            _pendingCard = CardID.None;
            _isWaitingForSlotSelection = false;
        }
        else
        {
            gameManager.LogMessage(0, "<color=red>Movimiento inválido o energía insuficiente.</color>");
            OnCancelDeploy(); // Si falla, cancelamos el flujo
        }
    }
}