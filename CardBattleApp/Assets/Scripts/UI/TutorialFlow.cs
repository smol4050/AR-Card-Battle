using UnityEngine;
using System.Collections;

public enum TutorialStep { ScanBoard, ScanCards, DeployUnit, FreePlay, ScanUnit }

public class TutorialFlowController : MonoBehaviour
{
    [Header("Referencias de Scripts")]
    public ARCardScanner cardScanner;
    public GameManager gameManager;

    [Header("Paneles de UI")]
    public GameObject panelPaso1; // Escanear Tablero
    public GameObject panelPaso2; // Escanear Cartas
    public GameObject panelPaso3; // Posicionar Personaje

    private TutorialStep currentStep = TutorialStep.ScanBoard;

    private void Start()
    {
        ActualizarUI(TutorialStep.ScanBoard);

        if (cardScanner != null)
        {
            cardScanner.OnBattlefieldSpawned += HandleBoardDetected;
            cardScanner.OnCardScanned += HandleCardDetected;

            if (cardScanner.IsBattlefieldReady)
            {
                HandleBoardDetected(cardScanner.BoardRefs);
            }
        }

        if (gameManager != null)
        {
            gameManager.OnUnitSpawned += HandleUnitDeployed;
        }
    }

    private void Update()
    {
        // --- DETECCIÓN DE TOQUE PARA AVANCE MANUAL ---
        // Detecta toque en celular o click izquierdo en PC
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            AvanceManualPorToque();
        }
    }

    private void AvanceManualPorToque()
    {
        switch (currentStep)
        {
            case TutorialStep.ScanBoard:
                Debug.Log("Tutorial: Avance manual al Paso 2 (Cartas)");
                PasarAlSiguientePaso(TutorialStep.ScanCards);
                break;
            case TutorialStep.ScanCards:
                Debug.Log("Tutorial: Avance manual al Paso 3 (Despliegue)");
                PasarAlSiguientePaso(TutorialStep.DeployUnit);
                break;
            case TutorialStep.DeployUnit:
                Debug.Log("Tutorial: Avance manual al Final (Modo Libre)");
                PasarAlSiguientePaso(TutorialStep.FreePlay);
                break;
                // Si ya está en FreePlay, no hacemos nada para no romper el juego
        }
    }

    private void OnDestroy()
    {
        if (cardScanner != null)
        {
            cardScanner.OnBattlefieldSpawned -= HandleBoardDetected;
            cardScanner.OnCardScanned -= HandleCardDetected;
        }
        if (gameManager != null)
        {
            gameManager.OnUnitSpawned -= HandleUnitDeployed;
        }
    }

    // --- MANEJO DE EVENTOS AUTOMÁTICOS ---

    private void HandleBoardDetected(BattlefieldReferences refs)
    {
        if (currentStep == TutorialStep.ScanBoard)
        {
            PasarAlSiguientePaso(TutorialStep.ScanCards);
        }
    }

    private void HandleCardDetected(CardID card)
    {
        if (currentStep == TutorialStep.ScanCards)
        {
            PasarAlSiguientePaso(TutorialStep.DeployUnit);
        }
    }

    private void HandleUnitDeployed(int playerId, Unit unit)
    {
        if (currentStep == TutorialStep.DeployUnit && playerId == 0)
        {
            PasarAlSiguientePaso(TutorialStep.FreePlay);
        }
    }

    private void PasarAlSiguientePaso(TutorialStep next)
    {
        currentStep = next;
        ActualizarUI(currentStep);
    }

    private void ActualizarUI(TutorialStep step)
    {
        if (panelPaso1) panelPaso1.SetActive(step == TutorialStep.ScanBoard);
        if (panelPaso2) panelPaso2.SetActive(step == TutorialStep.ScanCards);
        if (panelPaso3) panelPaso3.SetActive(step == TutorialStep.ScanUnit || step == TutorialStep.DeployUnit);

        if (step == TutorialStep.FreePlay)
        {
            StartCoroutine(FinalizarTutorial());
        }
    }

    private IEnumerator FinalizarTutorial()
    {
        yield return new WaitForSeconds(2f);
        if (panelPaso3) panelPaso3.SetActive(false);
        Debug.Log("<color=yellow>Tutorial Finalizado - Control total al jugador</color>");
    }
}