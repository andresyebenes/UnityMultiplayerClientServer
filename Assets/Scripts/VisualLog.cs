using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisualLog : MonoBehaviour
{
    public ScrollRect logScrollRect;
    public RectTransform logContentTransform;
    public Text logLabel;
    public GameObject logEntryPrefab;
    public Mode mode = Mode.ONE_LABEL;
    public bool alsoDebugLog = false;
    
    public static VisualLog instance = null;

    private bool _logFromThread = false;
    private List<string> _messagesFromThread = new List<string>();

    public enum Mode
    {
        ONE_LABEL,
        MULTIPLE_LABELS
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
        }
    }


    private void Update()
    {
        if (_logFromThread)
        {
            _logFromThread = false;

            foreach (string message in _messagesFromThread) {
                log(message);
            }

            _messagesFromThread.Clear();
        }
    }


    public void log(string message)
    {
        if (alsoDebugLog)
            Debug.Log(message);

        switch (mode) {
            case Mode.ONE_LABEL:
                if (logLabel != null)
                {
                    //logLabel.text += string.IsNullOrEmpty(logLabel.text) ? message : "\n" + message;
                    logLabel.text += "\n" + message;
                    StartCoroutine(ScrollDown());
                }
                break;
            case Mode.MULTIPLE_LABELS:
                if (logEntryPrefab != null && logContentTransform != null)
                {
                    GameObject logEntry = Instantiate<GameObject>(logEntryPrefab, logContentTransform);
                    logEntry.SetActive(false);
                    Text logLabel = logEntry.GetComponent<Text>();

                    if (logLabel != null)
                    {
                        logLabel.text = message;
                    }

                    logEntry.SetActive(true);
                    logScrollRect.verticalNormalizedPosition = 0f;
                    StartCoroutine(ScrollDown());
                }
                break;
        }
    }


    public void logFromThread(string message)
    {
        _logFromThread = true;
        _messagesFromThread.Add(message);
    }


    private IEnumerator ScrollDown()
    {
        yield return new WaitForEndOfFrame();

        logScrollRect.verticalNormalizedPosition = 0f;
    }
}
