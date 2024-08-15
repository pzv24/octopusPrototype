using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugPanel : MonoBehaviour
{
    [SerializeField] private bool _panelEnabled = false;
    [SerializeField] List<GameObject> _panels = new List<GameObject>();
    [SerializeField] private TextMeshProUGUI _FPSCounter;
    private Coroutine _fpsMeterCoroutine;
    private void Start()
    {
        SetDebugTextElementsEnabled(_panelEnabled);
    }
    private void OnEnable()
    {
        if(_fpsMeterCoroutine != null)
        {
            StopCoroutine(_fpsMeterCoroutine);
        }
        _fpsMeterCoroutine = StartCoroutine(FPSCounter());
    }
    private void OnDisable()
    {
        StopCoroutine(_fpsMeterCoroutine);
        _fpsMeterCoroutine = null;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _panelEnabled = !_panelEnabled;
            SetDebugTextElementsEnabled(!_panelEnabled);
        }
    }
    private IEnumerator FPSCounter()
    {
        while (true)
        {
            if(_FPSCounter.isActiveAndEnabled)
            {
                _FPSCounter.text = $"FPS: {((int)(1f / Time.unscaledDeltaTime)).ToString()}";
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
    private void SetDebugTextElementsEnabled(bool enabled)
    {
        foreach(GameObject panel in _panels)
        {
            panel.SetActive(enabled);
        }
    }
}
