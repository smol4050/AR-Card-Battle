using UnityEngine;
using TMPro; // Usamos la librería oficial para mayor seguridad y rendimiento

public class UIManager : MonoBehaviour
{
    [Header("Main Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject tutorialPanel; // Corresponde a tu Panel_Principal

    [Header("Tutorial Panels (Panel_Info)")]
    [SerializeField] private GameObject[] tutorialSteps; // Aquí arrastras Panel1, Panel2, Panel3

    [Header("UI Controls")]
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject startButton; // Tu botón "Iniciar"

    [Header("Gameplay Core References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private VisualController visualController;
    [SerializeField] private TutorialManager tutorialManager;

    private int _currentStep = 0;

    // Textos para cada paso del tutorial
    private readonly string[] _tutorialMessages = new string[]
    {
        "Welcome! In this game, each player has 20 HP and a limited Energy pool.", // Step 0 (Panel 1)
        "There are 3 Lanes. You can place one unit per lane to attack or defend.", // Step 1 (Panel 2)
        "Cards cost Energy. Place a unit, use an ability, and destroy the enemy!"  // Step 2 (Panel 3)
    };

    void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        tutorialPanel.SetActive(false);

        // Asegurarnos de que el gameplay está apagado
        gameManager.gameObject.SetActive(false);
    }

    // Se llama desde el botón del menú principal para ir al tutorial
    public void StartTutorialUI()
    {
        mainMenuPanel.SetActive(false);
        tutorialPanel.SetActive(true);
        _currentStep = 0;
        UpdateTutorialUI();
    }

    public void NextStep()
    {
        if (_currentStep < tutorialSteps.Length - 1)
        {
            _currentStep++;
            UpdateTutorialUI();
        }
    }

    public void PreviousStep()
    {
        if (_currentStep > 0)
        {
            _currentStep--;
            UpdateTutorialUI();
        }
    }

    private void UpdateTutorialUI()
    {
        // 1. Activar solo el panel correspondiente
        for (int i = 0; i < tutorialSteps.Length; i++)
        {
            tutorialSteps[i].SetActive(i == _currentStep);
        }

        // 2. Actualizar el texto del tutorial
        if (tutorialText != null && _currentStep < _tutorialMessages.Length)
        {
            tutorialText.text = _tutorialMessages[_currentStep];
        }

        // 3. Controlar visibilidad de botones de navegación
        backButton.SetActive(_currentStep > 0);
        nextButton.SetActive(_currentStep < tutorialSteps.Length - 1);

        // 4. El botón "Iniciar" solo aparece en el último paso
        startButton.SetActive(_currentStep == tutorialSteps.Length - 1);
    }

    // Se llama desde tu botón "Iniciar"
    public void StartGame()
    {
        // 1. Ocultamos toda la UI del menú
        tutorialPanel.SetActive(false);
        mainMenuPanel.SetActive(false);

        // 2. Activamos los sistemas lógicos y visuales
        gameManager.gameObject.SetActive(true);
        visualController.enabled = true;

        // 3. Inicializamos las matemáticas y los turnos
        gameManager.InitializeGame();
        turnManager.Initialize(gameManager);

        // 4. Le damos el control in-game al TutorialManager
        tutorialManager.StartInGameTutorial();
    }
}