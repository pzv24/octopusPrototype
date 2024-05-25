using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using Unity.VisualScripting;

public class TentacleCodeAnimator : MonoBehaviour
{
    [SerializeField] private TentacleVisual _visual;
    //TODO make sure this mathces tentacle gameplkay launch speed 
    [SerializeField] private float _launchAnimationSpeed = 12f;
    [SerializeField] private float _retractAnimationDuration = 0.5f;

    [SerializeField] private float _minDistanceForWiggle = 0.5f;
    [SerializeField] private float _maxDistanceForWiggle = 12f;
    [SerializeField] private Vector2 _wiggleFrequencyMinAndMaxBasedOnDistance = Vector2.zero;
    [SerializeField] private Vector2 _wiggleAmplitudeMinAndMaxBasedOnDistance = Vector2.zero;

    [SerializeField] private float _bezierCurveHeight = 3;
    [SerializeField, Range(0,1)] private float _bezierAnchorModifier = 0.5f;
    [SerializeField] private float _curveDirection = 1;

    private void Start()
    {
        _visual = GetComponent<TentacleVisual>();
    }
    [Button]
    public void AnimateLaunch(Vector3 anchorWorldPosition)
    {
        StartCoroutine(LaunchTentacle(anchorWorldPosition));
    }
    [Button]
    public void AnimateRetract()
    {
        StartCoroutine(RetractTentacle());
    }
    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse left click");
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0;
            AnimateLaunch(mousePosition);
            Debug.Log("Distance of launch is : " +Vector3.Distance(transform.position, mousePosition));
        }
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Mouse right click");
            AnimateRetract();
        }
    }

    private IEnumerator LaunchTentacle(Vector3 anchorWorldPosition)
    {
        float lerp = 0;
        _visual.IsLaunching = true;
        Vector3 start = Vector3.zero;
        Vector3 end = anchorWorldPosition;
        Vector3 bezierAnchor = start + ((end - start)*_bezierAnchorModifier) + Vector3.Cross((end-start).normalized, Vector3.forward * _curveDirection) * _bezierCurveHeight;
        Debug.DrawLine(start, bezierAnchor, Color.blue, 1);
        Debug.DrawLine(end, bezierAnchor, Color.blue, 1);
        while (lerp < 1)
        {
            lerp += _launchAnimationSpeed * Time.deltaTime;
            //Vector3 finalPosition = Vector3.Slerp(Vector3.zero, anchorWorldPosition, lerp);
            Vector3 startToAnchor = Vector3.Lerp(start, bezierAnchor, lerp);
            Vector3 anchorToEnd = Vector3.Lerp(bezierAnchor, end, lerp);
            Vector3 finalPosition = Vector3.Lerp(startToAnchor, anchorToEnd, lerp);
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator RetractTentacle()
    {
        _visual.SetIsConnecteed(false);
        _visual.SetIsWiggling(true);
        float lerp = 0;
        Vector3 startPos = _visual.FollowTransform.position;
        while (lerp < 1)
        {
            lerp += (1 / _retractAnimationDuration) * Time.deltaTime;
            Vector3 finalPosition = Vector3.Lerp(startPos, Vector3.zero, lerp);
            SetDistanceBasedWiggle();
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
        _visual.SetIsWiggling(false);
    }

    private void SetDistanceBasedWiggle()
    {
        float distance = Vector3.Distance(_visual.transform.position, _visual.FollowTransform.position);
        float lerp = Mathf.InverseLerp(_maxDistanceForWiggle, _minDistanceForWiggle, distance);
        lerp = distance > _maxDistanceForWiggle ? 1 : lerp;
        float distanceBasedWaveMagnitude = Mathf.Lerp(_wiggleAmplitudeMinAndMaxBasedOnDistance.y, _wiggleAmplitudeMinAndMaxBasedOnDistance.x, lerp);
        float distanceBasedWaveFrequency = Mathf.Lerp(_wiggleFrequencyMinAndMaxBasedOnDistance.y, _wiggleFrequencyMinAndMaxBasedOnDistance.x, lerp);
        _visual.SetWiggleVariables(distanceBasedWaveFrequency, distanceBasedWaveMagnitude);
    }
}
