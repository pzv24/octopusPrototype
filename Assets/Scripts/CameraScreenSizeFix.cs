using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Sirenix.OdinInspector;
using Cinemachine;

public class CameraScreenSizeFix : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _virtualCamera;
    [SerializeField] private TextMeshProUGUI _displayText;
    [SerializeField] private float _targetOrthoSize = 10;
    [SerializeField] private float _maxOrthoSize = 18;
    [SerializeField] private float _minWorldDistance = 20;
    [SerializeField] private float _differenceTolerance = 0.5f;
    [SerializeField] private float _refreshCooldown = 0.5f;
    [SerializeField, ReadOnly] private float _cameraWorldDistance;

    private void Start()
    {
        StartCoroutine(AdjustCameraOrthoSizeRefresh());
    }

    private IEnumerator AdjustCameraOrthoSizeRefresh()
    {
        while (true)
        {
            float target = FindCameraOrthoSize();
            if (Mathf.Abs(_virtualCamera.m_Lens.OrthographicSize - target) > _differenceTolerance)
            {
                _virtualCamera.m_Lens.OrthographicSize = target;
            }
            if (_displayText.isActiveAndEnabled)
            {
                _displayText.text = $"{Screen.width} by {Screen.height} pixels. Resolution is {Screen.currentResolution}. Target Orthosize {target}";
            }
            yield return new WaitForSeconds(_refreshCooldown);
        }

    }
    private float FindCameraOrthoSize()
    {
        float targetOrthoSize = _virtualCamera.m_Lens.OrthographicSize;
        Vector3 left = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0));
        Vector3 right = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0));
        _cameraWorldDistance = Vector3.Distance(left, right);
        if (Mathf.Abs(_cameraWorldDistance - _minWorldDistance) > _differenceTolerance)
        {
            float aspectRatio = ((float)Screen.width / (float)Screen.height) * 2;
            targetOrthoSize = _minWorldDistance / aspectRatio;
            targetOrthoSize = Mathf.Round(targetOrthoSize * 100f) / 100f;
        }
        return Mathf.Clamp(targetOrthoSize, _targetOrthoSize, _maxOrthoSize);
    }
}
