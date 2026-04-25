using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("Configuración de Videos")]
    public VideoPlayer videoPlayer;
    public VideoClip videoEvento; // Primer video
    public VideoClip videoJuego;  // Segundo video

    [Header("Configuración de UI")]
    public GameObject panelInscripcion;
    public TMP_InputField inputNombre;
    public TMP_InputField inputApodo;
    public TMP_InputField inputPersonaje;
    public TMP_InputField inputEdad;
    public Button btnInscribirse;

    [Header("Configuración de Escena")]
    public string nombreEscenaMenu = "MenuScene"; // Cambia esto por el nombre exacto de tu escena

    private int faseVideo = 1; // 1 = Video Evento, 2 = Video Juego

    private void Start()
    {
        // Configuraciones iniciales
        panelInscripcion.SetActive(false);
        btnInscribirse.onClick.AddListener(AlDarClicInscribirse);

        // Nos suscribimos al evento que avisa cuando un video termina
        videoPlayer.loopPointReached += AlTerminarVideo;

        // Reproducimos el primer video
        ReproducirVideo(videoEvento);
    }

    private void ReproducirVideo(VideoClip clip)
    {
        videoPlayer.clip = clip;
        videoPlayer.Play();
    }

    private void AlTerminarVideo(VideoPlayer vp)
    {
        if (faseVideo == 1)
        {
            // Termina el Video del Evento -> Mostramos el Panel
            panelInscripcion.SetActive(true);
        }
        else if (faseVideo == 2)
        {
            // Termina el Video del Juego -> Pasamos al Menú
            SceneManager.LoadScene(nombreEscenaMenu);
        }
    }

    private void AlDarClicInscribirse()
    {
        // 1. Validar que los campos no estén vacíos (básico)
        if (string.IsNullOrEmpty(inputNombre.text) || string.IsNullOrEmpty(inputApodo.text))
        {
            Debug.LogWarning("Faltan campos por llenar");
            return; // Puedes mostrar un texto de error en la UI aquí si quieres
        }

        // 2. Convertir la edad a número de forma segura
        int edadMela = 0;
        int.TryParse(inputEdad.text, out edadMela);

        // 3. Crear el paquete de datos y asignar el ID de perfil random
        PlayerData nuevosDatos = new PlayerData();
        nuevosDatos.nombre = inputNombre.text;
        nuevosDatos.apodo = inputApodo.text;
        nuevosDatos.personajeFavorito = inputPersonaje.text;
        nuevosDatos.edad = edadMela;
        nuevosDatos.idFotoPerfil = Random.Range(1, 4); // Genera 1, 2 o 3 al azar

        // 4. Guardar usando el Singleton
        SaveManager.Instance.GuardarDatos(nuevosDatos);

        // 5. Ocultar panel y lanzar el segundo video
        panelInscripcion.SetActive(false);
        faseVideo = 2;
        ReproducirVideo(videoJuego);
    }

    private void OnDestroy()
    {
        // Buena práctica: desuscribirse de eventos al destruir el objeto para evitar errores de memoria
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= AlTerminarVideo;
        }
    }
}