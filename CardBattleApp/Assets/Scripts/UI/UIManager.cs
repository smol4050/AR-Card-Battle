using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Paneles principales")]
    public GameObject mainMenuPanel;
    public GameObject tutorialPanel;

    [Header("Configuración del Botón")]
    // Usamos 'Component' para que Unity no se bloquee al compilar
    // Luego lo trataremos como texto en el código
    public Component nextButtonText;

    [Header("Tutorial - Pasos")]
    public GameObject[] tutorialSteps;

    private int currentStep = 0;

    void Start() { ShowMainMenu(); }

    public void ShowMainMenu()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (tutorialPanel) tutorialPanel.SetActive(false);
    }

    public void StartTutorial()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (tutorialPanel) tutorialPanel.SetActive(true);
        currentStep = 0;
        UpdateTutorialUI();
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void NextStep()
    {
        if (currentStep < tutorialSteps.Length - 1)
        {
            currentStep++;
            UpdateTutorialUI();
        }
        else { StartGame(); }
    }

    void UpdateTutorialUI()
    {
        for (int i = 0; i < tutorialSteps.Length; i++)
        {
            if (tutorialSteps[i]) tutorialSteps[i].SetActive(i == currentStep);
        }

        if (nextButtonText != null)
        {
            string texto = (currentStep == tutorialSteps.Length - 1) ? "Comenzar" : "Siguiente";

            // Este truco intenta cambiar el texto de TMPro sin necesitar el 'using'
            nextButtonText.SendMessage("set_text", texto, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void StartGame() { SceneManager.LoadScene("02_ARBattle"); }
}