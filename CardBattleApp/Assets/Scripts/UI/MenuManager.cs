using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Mundos 360 Aleatorios")]
    public GameObject[] mundos360; // Arrastra aquí tus 2 Empties (Mundo 1 y Mundo 2)

    [Header("UI del Jugador (Fijo en Cámara)")]
    public TextMeshProUGUI txtApodo;
    public TextMeshProUGUI txtNombre;
    public Image imgFotoPerfil;
    public Sprite[] fotosDePerfil; // Arrastra aquí tus 3 fotos (Tamaño del array = 3)

    [Header("Paneles de UI")]
    public GameObject panelAjustes;
    public GameObject panelCreditos;

    private void Awake()
    {
        // Singleton local para esta escena
        Instance = this;
    }

    private void Start()
    {
        ConfigurarMundoAleatorio();
        CargarDatosJugador();
        CerrarTodosLosPaneles();
    }

    private void ConfigurarMundoAleatorio()
    {
        // Apagamos todos primero por seguridad
        foreach (var mundo in mundos360)
        {
            mundo.SetActive(false);
        }

        // Encendemos uno al azar
        if (mundos360.Length > 0)
        {
            int indexMundo = Random.Range(0, mundos360.Length);
            mundos360[indexMundo].SetActive(true);
        }
    }

    private void CargarDatosJugador()
    {
        // Traemos los datos del JSON usando el Singleton de la escena anterior
        if (SaveManager.Instance != null && SaveManager.Instance.DatosActuales != null)
        {
            PlayerData datos = SaveManager.Instance.DatosActuales;
            txtApodo.text = datos.apodo;
            txtNombre.text = datos.nombre;

            // Asignamos la foto basada en el ID random que generamos en la inscripción (1, 2 o 3)
            // Le restamos 1 porque los arrays en código empiezan en 0
            int indexFoto = Mathf.Clamp(datos.idFotoPerfil - 1, 0, fotosDePerfil.Length - 1);
            if (fotosDePerfil.Length > 0)
            {
                imgFotoPerfil.sprite = fotosDePerfil[indexFoto];
            }
        }
        else
        {
            Debug.LogWarning("No se encontraron datos del jugador. ¿Iniciaste desde la escena Intro?");
        }
    }

    // --- MÉTODOS PARA LOS BOTONES VR ---

    public void AbrirAjustes()
    {
        CerrarTodosLosPaneles();
        panelAjustes.SetActive(true);
    }

    public void AbrirCreditos()
    {
        CerrarTodosLosPaneles();
        panelCreditos.SetActive(true);
    }

    public void CerrarTodosLosPaneles()
    {
        if (panelAjustes != null) panelAjustes.SetActive(false);
        if (panelCreditos != null) panelCreditos.SetActive(false);
    }

    public void IniciarTutorial()
    {
        // Usamos el LoadingManager super pro que hicimos antes
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.CargarEscena("TutorialScene"); // Cambia el nombre si es distinto
        }
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    // Para saber si hay paneles estorbando la vista
    public bool HayPanelesAbiertos()
    {
        return (panelAjustes != null && panelAjustes.activeSelf) ||
               (panelCreditos != null && panelCreditos.activeSelf);
    }
}
