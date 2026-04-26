using UnityEngine;
using System.Collections;

public enum TutorialStep { ScanBoard, ScanCards, DeployUnit, FreePlay }

/// <summary>
/// Controla los paneles del tutorial usando un sistema híbrido a prueba de fallos.
/// </summary>
public class TutorialFlowController : MonoBehaviour
{
    [Header("Referencia OBLIGATORIA")]
    public ARDeployFlow deployFlow;

    [Header("Paneles de UI")]
    public GameObject panelPaso1;     // Escanea tablero
    public GameObject panelPaso2;     // Escanea carta
    public GameObject panelPaso3;     // Toca slot
    public GameObject panelFreePlay;  // Pulsa Listo
    public GameObject panelBattle;    // ¡Batalla!

    [Header("Duración panel batalla (segundos)")]
    public float battlePanelDuration = 2.5f;

    private TutorialStep _step = TutorialStep.ScanBoard;

    private void Start()
    {
        if (deployFlow == null)
        {
            Debug.LogError("[Tutorial] ARDeployFlow no asignado. El tutorial no funcionará.");
            return;
        }
        RefreshPanels();
    }

    // ─── EL PARCHE A PRUEBA DE FALLOS ─────────────────────────────────────────
    // En lugar de depender de un evento frágil que puede perderse, el Update
    // revisa activamente si el tablero ya existe físicamente.
    private void Update()
    {
        if (deployFlow == null || deployFlow.scanner == null) return;

        // PASO 1 al PASO 2: ¿Apareció el tablero?
        if (_step == TutorialStep.ScanBoard && deployFlow.scanner.IsBattlefieldReady)
        {
            Debug.Log("<color=cyan>[Tutorial] Tablero detectado fìsicamente. Avanzando al Paso 2.</color>");
            AdvanceTo(TutorialStep.ScanCards);
        }
    }

    // ─── EVENTOS DE UI (Estos no fallan porque dependen de clics humanos) ─────
    private void OnEnable()
    {
        if (deployFlow != null)
        {
            deployFlow.OnCardConfirmed += HandleCardConfirmed;
            deployFlow.OnUnitPlaced += HandleUnitPlaced;
            deployFlow.OnPlayerReady += HandlePlayerReady;
        }
    }

    private void OnDisable()
    {
        if (deployFlow != null)
        {
            deployFlow.OnCardConfirmed -= HandleCardConfirmed;
            deployFlow.OnUnitPlaced -= HandleUnitPlaced;
            deployFlow.OnPlayerReady -= HandlePlayerReady;
        }
    }

    private void HandleCardConfirmed()
    {
        if (_step == TutorialStep.ScanCards) AdvanceTo(TutorialStep.DeployUnit);
    }

    private void HandleUnitPlaced()
    {
        if (_step == TutorialStep.DeployUnit) AdvanceTo(TutorialStep.FreePlay);
    }

    private void HandlePlayerReady()
    {
        SetPanel(panelPaso1, false);
        SetPanel(panelPaso2, false);
        SetPanel(panelPaso3, false);
        SetPanel(panelFreePlay, false);
        if (panelBattle != null) StartCoroutine(ShowBattlePanel());
    }

    // ─── GESTIÓN DE PANELES ───────────────────────────────────────────────────
    private void AdvanceTo(TutorialStep next)
    {
        _step = next;
        RefreshPanels();
    }

    private void RefreshPanels()
    {
        SetPanel(panelPaso1, _step == TutorialStep.ScanBoard);
        SetPanel(panelPaso2, _step == TutorialStep.ScanCards);
        SetPanel(panelPaso3, _step == TutorialStep.DeployUnit);
        SetPanel(panelFreePlay, _step == TutorialStep.FreePlay);
        SetPanel(panelBattle, false);
    }

    private IEnumerator ShowBattlePanel()
    {
        SetPanel(panelBattle, true);
        yield return new WaitForSeconds(battlePanelDuration);
        SetPanel(panelBattle, false);
    }

    private static void SetPanel(GameObject p, bool active)
    {
        if (p != null) p.SetActive(active);
    }
}