using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("Configuración de UI")]
    public CanvasGroup loadingCanvasGroup; // El panel que cubre todo
    public Image imagenTitilante;          // La imagen que va a parpadear
    public float velocidadTitileo = 2f;    // Qué tan rápido parpadea
    public float fadeDuration = 0.5f;      // Duración del fade entrada/salida

    private void Awake()
    {
        // Singleton persistente
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Empezamos invisible
        loadingCanvasGroup.alpha = 0;
        loadingCanvasGroup.blocksRaycasts = false;
    }

    // El método que llamarás desde otros scripts
    public void CargarEscena(string nombreEscena)
    {
        StartCoroutine(ProcesoCarga(nombreEscena));
    }

    private IEnumerator ProcesoCarga(string nombreEscena)
    {
        // 1. Fade In (Aparece el panel de carga)
        yield return StartCoroutine(Fade(1));
        loadingCanvasGroup.blocksRaycasts = true;

        // 2. Iniciar la carga de la escena en segundo plano
        AsyncOperation operacion = SceneManager.LoadSceneAsync(nombreEscena);

        // Mientras la escena carga, hacemos que la imagen titile
        while (!operacion.isDone)
        {
            // Efecto de titileo usando una onda Senoidal (Sin)
            float alpha = (Mathf.Sin(Time.time * velocidadTitileo) + 1f) / 2f;
            if (imagenTitilante != null)
            {
                Color c = imagenTitilante.color;
                c.a = alpha;
                imagenTitilante.color = c;
            }
            yield return null;
        }

        // 3. Fade Out (Desaparece el panel cuando ya cargó)
        yield return StartCoroutine(Fade(0));
        loadingCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = loadingCanvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        loadingCanvasGroup.alpha = targetAlpha;
    }
}
