using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class ConsoleDisplayer : MonoBehaviour
{
    [SerializeField] private GameObject scrollView;
    [SerializeField] private VerticalLayoutGroup LogHistory;
    [SerializeField] private int LogEntryFontSize = 32;

    [SerializeField] private TextMeshProUGUI LogEntryPrefab;
    [SerializeField]
    [Range(1, 100)]
    private int MaxLogCount = 100;

    private readonly string red = "red";
    private readonly string yellow = "yellow";
    private readonly string blue = "white";

    private readonly List<TextMeshProUGUI> logEntries = new List<TextMeshProUGUI>();

    [SerializeField] private Scrollbar scrollbar;
    [SerializeField] private bool isConsoleEnable = true;

    private void Awake()
    {
        Application.logMessageReceived += DisplayLogCallback;

        LogHistory.spacing = LogEntryFontSize / 2f;

        scrollView.SetActive(isConsoleEnable);
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= DisplayLogCallback;

    }

    private void DisplayLogCallback(string logString, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                DisplayMsg(logString, red);
                break;
            case LogType.Assert:
                DisplayMsg(logString, red);
                break;
            case LogType.Warning:
                DisplayMsg(logString, yellow);
                break;
            case LogType.Log:
                DisplayMsg(logString, blue);
                break;
            case LogType.Exception:
                DisplayMsg(logString, red);
                break;
            default:
                break;
        }
    }

    private void DisplayMsg(string logString, string color)
    {
        var newLogEntry = Instantiate(LogEntryPrefab, LogHistory.transform);
        newLogEntry.text = $"<color=\"{color}\">{DateTime.Now.ToString("HH:mm:ss.fff")} {logString}</color>\n";
        newLogEntry.fontSize = LogEntryFontSize;

        logEntries.Add(newLogEntry);

        if (logEntries.Count > MaxLogCount)
        {
            var textObj = logEntries.First();
            logEntries.RemoveAt(0);
            Destroy(textObj.gameObject);
        }

        scrollbar.value = 0;
    }

    public void ToggleConsole()
    {
        isConsoleEnable = !isConsoleEnable;
        scrollView.SetActive(isConsoleEnable);
    }
}
