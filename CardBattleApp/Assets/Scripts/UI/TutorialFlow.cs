using UnityEngine;
using System.Collections;

public enum TutorialStep { ScanBoard, ScanCards, DeployUnit, FreePlay }

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
        // Inicializamos el tutorial
        ActualizarUI(TutorialStep.ScanBoard);

        // Nos suscribimos a los eventos de tus scripts
        if (cardScanner != null)
        {
            cardScanner.OnBattlefieldSpawned += HandleBoardDetected;
            cardScanner.OnCardScanned += HandleCardDetected;
        }

        if (gameManager != null)
        {
            gameManager.OnUnitSpawned += HandleUnitDeployed;
        }
    }

    private void OnDestroy()
    {
        // Des-suscripción para evitar errores de memoria
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
        if (currentStep == TutorialStep.ScanBoard)
        {
            Debug.Log("Tutorial: Tablero detectado.");
            PasarAlSiguientePaso(TutorialStep.ScanCards);
        }
    }

    private void HandleCardDetected(CardID card)
    {
        if (currentStep == TutorialStep.ScanCards)
        {
            Debug.Log($"Tutorial: Carta {card} detectada.");
            PasarAlSiguientePaso(TutorialStep.DeployUnit);
        }
    }

    private void HandleUnitDeployed(int playerId, Unit unit)
    {
        // Solo nos importa si el jugador local (ID 0) desplegó algo durante el paso 3
        if (currentStep == TutorialStep.DeployUnit && playerId == 0)
        {
            Debug.Log("Tutorial: Unidad desplegada. ¡Tutorial completado!");
            PasarAlSiguientePaso(TutorialStep.FreePlay);
        }
    }

    // --- LÓGICA DE FLUJO ---

    private void PasarAlSiguientePaso(TutorialStep next)
    {
        currentStep = next;
        ActualizarUI(currentStep);
    }

    private void ActualizarUI(TutorialStep step)
    {
        // Apagamos todo primero
        panelPaso1.SetActive(false);
        panelPaso2.SetActive(false);
        panelPaso3.SetActive(false);

        // Encendemos solo el necesario
        switch (step)
        {
            case TutorialStep.ScanBoard:
                panelPaso1.SetActive(true);
                break;
            case TutorialStep.ScanCards:
                panelPaso2.SetActive(true);
                break;
            case TutorialStep.DeployUnit:
                panelPaso3.SetActive(true);
                break;
            case TutorialStep.FreePlay:
                // Aquí podrías poner un mensaje de "¡A jugar!" que desaparezca solo
                StartCoroutine(FinalizarTutorial());
                break;
        }
    }

    private IEnumerator FinalizarTutorial()
    {
        // El jugador ya es libre, ocultamos paneles de instrucción
        Debug.Log("Tutorial: El jugador ahora está en modo libre.");
        yield return new WaitForSeconds(3f);
        // Opcional: habilitar el TurnManager si estaba pausado
    }
}