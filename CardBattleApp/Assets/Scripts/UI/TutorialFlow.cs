using UnityEngine;
using System.Collections;

public enum TutorialStep { ScanBoard, ScanCards, DeployUnit, FreePlay, ScanUnit}

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
        // 1. Forzar el estado inicial de la UI
        ActualizarUI(TutorialStep.ScanBoard);

        if (cardScanner != null)
        {
            // 2. Suscribirse a los eventos para cambios futuros
            cardScanner.OnBattlefieldSpawned += HandleBoardDetected;
            cardScanner.OnCardScanned += HandleCardDetected;

            // 3. ¡EL TRUCO! Si el tablero ya está listo (porque se detectó rápido),
            // forzamos el paso al siguiente nivel de una vez.
            if (cardScanner.IsBattlefieldReady)
            {
                Debug.Log("Tutorial: El tablero ya estaba listo antes de empezar. Saltando al paso 2.");
                HandleBoardDetected(cardScanner.BoardRefs);
            }
        }

        if (gameManager != null)
        {
            gameManager.OnUnitSpawned += HandleUnitDeployed;
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

    // --- MANEJO DE EVENTOS ---

    private void HandleBoardDetected(BattlefieldReferences refs)
    {
        // Solo avanzamos si estamos en el paso 1
        if (currentStep == TutorialStep.ScanBoard)
        {
            Debug.Log("<color=cyan>Tutorial: Recibida señal de Tablero. Cambiando a Paso 2.</color>");
            PasarAlSiguientePaso(TutorialStep.ScanCards);
        }
    }

    private void HandleCardDetected(CardID card)
    {
        if (currentStep == TutorialStep.ScanCards)
        {
            Debug.Log($"<color=cyan>Tutorial: Carta {card} detectada. Cambiando a Paso 3.</color>");
            PasarAlSiguientePaso(TutorialStep.DeployUnit);
        }
    }

    private void HandleUnitDeployed(int playerId, Unit unit)
    {
        if (currentStep == TutorialStep.DeployUnit && playerId == 0)
        {
            Debug.Log("<color=green>Tutorial: ¡Unidad puesta! Fin del tutorial.</color>");
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
        // Aseguramos que los paneles existan antes de tocarlos
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
        yield return new WaitForSeconds(3f);
        // Aquí podrías apagar todos los paneles definitivamente
        if (panelPaso3) panelPaso3.SetActive(false);
    }
}