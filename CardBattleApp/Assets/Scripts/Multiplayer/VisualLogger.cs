using UnityEngine;
using TMPro;

public class VisualLogger : MonoBehaviour
{
    public TextMeshProUGUI logDisplay;

    // Suscribirse a los logs de Unity
    void OnEnable() => Application.logMessageReceived += HandleLog;
    void OnDisable() => Application.logMessageReceived -= HandleLog;

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logDisplay == null) return;

        string color = type == LogType.Error || type == LogType.Exception ? "red" : "white";
        if (type == LogType.Warning) color = "yellow";

        logDisplay.text += $"\n<color={color}>[{System.DateTime.Now:HH:mm:ss}] {logString}</color>";

        // Limitar a 10 líneas para no tapar la pantalla
        string[] lines = logDisplay.text.Split('\n');
        if (lines.Length > 10) logDisplay.text = string.Join("\n", lines, lines.Length - 10, 10);
    }
}