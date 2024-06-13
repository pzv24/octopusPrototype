using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using Unity.VisualScripting;

public class TentacleCodeAnimator : MonoBehaviour
{
    [Header("Plug in Fields")]
    [SerializeField] private TentacleVisual _visual;
    //TODO make sure this mathces tentacle gameplay launch speed 

    [Header("Launch Animation Settings")]
    [SerializeField] private float _launchAnimationSpeed = 12f;
    [SerializeField] private float _bezerCurveMaxHeight = 3;
    [SerializeField] private float _bezerCurveMinHeight = 0.2f;
    [SerializeField, Range(0,1)] private float _bezierAnchorModifier = 0.5f;
    [SerializeField] private float _curveDirection = 1;
    [Header("Retract Settings")]
    [SerializeField] private float _retractAnimationDuration = 0.5f;

    [Header("Wiggle Animation Settings")]
    [SerializeField] private float _minDistanceForWiggle = 0.5f;
    [SerializeField] private float _maxDistanceForWiggle = 12f;
    [SerializeField] private Vector2 _wiggleFrequencyMinAndMaxBasedOnDistance = Vector2.zero;
    [SerializeField] private Vector2 _wiggleAmplitudeMinAndMaxBasedOnDistance = Vector2.zero;

    private void Start()
    {
        _visual = GetComponent<TentacleVisual>();
    }
    [Button]
    public void AnimateLaunch(Vector3 anchorWorldPosition, Vector2 hitNormal)
    {
        StartCoroutine(LaunchTentacle(anchorWorldPosition, hitNormal));
    }
    [Button]
    public void AnimateRetract()
    {
        StartCoroutine(RetractTentacle());
    }
    public void AnimateJump()
    {
        StartCoroutine(JumpRetractMotion());
    }

    private IEnumerator LaunchTentacle(Vector3 anchorWorldPosition, Vector2 hitNormal)
    {
        float lerp = 0;
        _visual.ChangeVisualState(TentacleVisualState.Launching);
        Vector3 start = transform.position;
        Vector3 end = anchorWorldPosition;
        float bezerLerpValue = Mathf.InverseLerp(0, 8, Vector3.Distance(anchorWorldPosition, transform.position));
        float bezierHeight = Mathf.Lerp(_bezerCurveMinHeight, _bezerCurveMaxHeight, bezerLerpValue);
        Vector3 bezierAnchor = start + ((end - start)*_bezierAnchorModifier) + new  Vector3(hitNormal.x,hitNormal.y,0) * bezierHeight;
        //Debug.DrawRay(start, hitNormal*15, Color.red);
        //Debug.DrawLine(start, bezierAnchor, Color.blue, 1);
        //Debug.DrawLine(end, bezierAnchor, Color.blue, 1);
        while (lerp < 1)
        {
            lerp += _launchAnimationSpeed * Time.deltaTime;
            Vector3 startToAnchor = Vector3.Lerp(start, bezierAnchor, lerp);
            Vector3 anchorToEnd = Vector3.Lerp(bezierAnchor, end, lerp);
            Vector3 finalPosition = Vector3.Lerp(startToAnchor, anchorToEnd, lerp);
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator RetractTentacle()
    {
        _visual.ChangeVisualState(TentacleVisualState.Retracting);
        _visual.SetIsWiggling(true);
        float lerp = 0;
        Vector3 startPos = _visual.FollowTransform.position;
        while (lerp < 1)
        {
            lerp += (1 / _retractAnimationDuration) * Time.deltaTime;
            Vector3 finalPosition = Vector3.Lerp(startPos, transform.position, lerp);
            SetDistanceBasedWiggle();
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
        _visual.SetIsWiggling(false);
        _visual.ChangeVisualState(TentacleVisualState.Idle);
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

    private IEnumerator JumpRetractMotion()
    {
        yield return null;
    }

}
