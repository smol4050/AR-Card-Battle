using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Conecta el evento OnLogMessage del GameManager con un TextMeshProUGUI.
/// Opcionalmente hace auto-scroll si hay un ScrollRect asignado.
/// 
/// Setup en Unity:
///   1. Añade este componente a cualquier GameObject de UI.
///   2. Asigna gameManager, logText y (opcional) scrollRect en el Inspector.
///   3. Ajusta maxLines si quieres limitar el historial visible.
/// </summary>
public class GameLogUI : MonoBehaviour
{
    [Header("Referencias")]
    public GameManager gameManager;
    public TextMeshProUGUI logText;

    [Header("Scroll (opcional)")]
    [Tooltip("Si el logText está dentro de un ScrollRect, asígnalo aquí para auto-scroll.")]
    public ScrollRect scrollRect;

    [Header("Configuración")]
    [Tooltip("Número máximo de líneas mostradas antes de eliminar las más antiguas.")]
    public int maxLines = 40;

    [Tooltip("Mostrar timestamp (segundos desde inicio de la sesión) junto a cada línea.")]
    public bool showTimestamp = false;

    private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder();
    private int _lineCount;

    // ─── CICLO DE VIDA ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (gameManager == null)
            Debug.LogError("[GameLogUI] GameManager no asignado en el Inspector.");
        if (logText == null)
            Debug.LogError("[GameLogUI] logText (TextMeshProUGUI) no asignado en el Inspector.");
    }

    private void OnEnable()
    {
        if (gameManager != null)
            gameManager.OnLogMessage += HandleLogMessage;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnLogMessage -= HandleLogMessage;
    }

    // ─── HANDLER ──────────────────────────────────────────────────────────────
    private void HandleLogMessage(int playerId, string message)
    {
        if (logText == null) return;

        string prefix = playerId switch
        {
            0 => "<color=#FFD700>[Sollar]</color> ",
            1 => "<color=#AA44FF>[Vacío]</color> ",
            -1 => "<color=#AAAAAA>[Sistema]</color> ",
            _ => ""
        };

        string timestamp = showTimestamp ? $"<color=#555555>[{Time.time:F1}s]</color> " : "";

        _sb.AppendLine($"{timestamp}{prefix}{message}");
        _lineCount++;

        // Podar si superamos maxLines
        if (_lineCount > maxLines)
        {
            // Eliminar la primera línea del StringBuilder
            string full = _sb.ToString();
            int firstNewline = full.IndexOf('\n');
            if (firstNewline >= 0)
            {
                _sb.Remove(0, firstNewline + 1);
                _lineCount--;
            }
        }

        logText.text = _sb.ToString();

        // Auto-scroll al fondo
        if (scrollRect != null)
            Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>Limpia el log visualmente y resetea el buffer.</summary>
    public void ClearLog()
    {
        _sb.Clear();
        _lineCount = 0;
        if (logText != null) logText.text = string.Empty;
    }
}