using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem; // Importante: Nueva librería

public class IntroManager : MonoBehaviour
{
    [Header("Configuración de Videos")]
    public VideoPlayer videoPlayer;
    public VideoClip videoEvento;
    public VideoClip videoJuego;

    [Header("Configuración de UI")]
    public GameObject panelInscripcion;
    public CanvasGroup fadeCanvasGroup;
    public TMP_InputField inputNombre, inputApodo, inputPersonaje, inputEdad;
    public Button btnInscribirse;

    [Header("Audio")]
    public AudioSource bgmInscripcion;
    public float fadeDuration = 1.0f;

    [Header("Configuración de Escena")]
    public string nombreEscenaMenu = "01_MainMenu";

    private int faseVideo = 1;
    private bool estaHaciendoFade = false;

    private void Start()
    {
        panelInscripcion.SetActive(false);
        fadeCanvasGroup.alpha = 1;
        bgmInscripcion.volume = 0;
        bgmInscripcion.Stop();

        btnInscribirse.onClick.AddListener(AlDarClicInscribirse);
        videoPlayer.loopPointReached += AlTerminarVideo;

        StartCoroutine(FlujoInicial());
    }

    private void Update()
    {
        // SISTEMA DE SKIP (Nuevo Input System)
        // Pointer.current.press detecta clics y toques por igual
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame && !estaHaciendoFade)
        {
            if (videoPlayer.isPlaying)
            {
                SaltarVideo();
            }
        }
    }

    private IEnumerator FlujoInicial()
    {
        ReproducirVideo(videoEvento);
        yield return StartCoroutine(Fade(0));
    }

    private void ReproducirVideo(VideoClip clip)
    {
        videoPlayer.clip = clip;
        videoPlayer.Play();
        videoPlayer.SetDirectAudioVolume(0, 1);
    }

    private void SaltarVideo()
    {
        videoPlayer.Stop();
        AlTerminarVideo(videoPlayer);
    }

    private void AlTerminarVideo(VideoPlayer vp)
    {
        if (estaHaciendoFade) return;

        if (faseVideo == 1)
        {
            StartCoroutine(TransicionVideoAPanel());
        }
        else if (faseVideo == 2)
        {
            StartCoroutine(TransicionFinalAMenu());
        }
    }

    private IEnumerator TransicionVideoAPanel()
    {
        estaHaciendoFade = true;
        yield return StartCoroutine(Fade(1, true));

        panelInscripcion.SetActive(true);
        bgmInscripcion.Play();

        yield return StartCoroutine(Fade(0, false, true));
        estaHaciendoFade = false;
    }

    private void AlDarClicInscribirse()
    {
        if (string.IsNullOrEmpty(inputNombre.text) || string.IsNullOrEmpty(inputApodo.text)) return;

        int edadMela = 0;
        int.TryParse(inputEdad.text, out edadMela);

        PlayerData nuevosDatos = new PlayerData();
        nuevosDatos.nombre = inputNombre.text;
        nuevosDatos.apodo = inputApodo.text;
        nuevosDatos.personajeFavorito = inputPersonaje.text;
        nuevosDatos.edad = edadMela;
        nuevosDatos.idFotoPerfil = Random.Range(1, 4);

        SaveManager.Instance.GuardarDatos(nuevosDatos);
        StartCoroutine(TransicionPanelAVideo2());
    }

    private IEnumerator TransicionPanelAVideo2()
    {
        estaHaciendoFade = true;
        yield return StartCoroutine(Fade(1, false, true));

        panelInscripcion.SetActive(false);
        bgmInscripcion.Stop();

        faseVideo = 2;
        ReproducirVideo(videoJuego);
        yield return StartCoroutine(Fade(0));

        estaHaciendoFade = false;
    }

    private IEnumerator TransicionFinalAMenu()
    {
        estaHaciendoFade = true;
        yield return StartCoroutine(Fade(1, true)); // Tu fade interno de la escena

        // CAMBIO AQUÍ: Llamamos al LoadingManager
        LoadingManager.Instance.CargarEscena(nombreEscenaMenu);
    }

    private IEnumerator Fade(float targetAlpha, bool fadeVideoAudio = false, bool fadeBGM = false)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float startBGMVol = bgmInscripcion.volume;
        float startVideoVol = videoPlayer.GetDirectAudioVolume(0);
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float lerpVal = time / fadeDuration;

            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, lerpVal);

            if (fadeVideoAudio)
            {
                videoPlayer.SetDirectAudioVolume(0, Mathf.Lerp(startVideoVol, 1 - targetAlpha, lerpVal));
            }

            if (fadeBGM)
            {
                bgmInscripcion.volume = Mathf.Lerp(startBGMVol, 1 - targetAlpha, lerpVal);
            }

            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= AlTerminarVideo;
    }
}