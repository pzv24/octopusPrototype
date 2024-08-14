using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TentacleCodeAnimator : MonoBehaviour
{
    [Header("Plug in Fields")]
    [SerializeField] private TentacleVisual _visual;

    [Header("Launch Animation Settings")]
    [SerializeField] private float _launchAnimationSpeed = 12f;
    [SerializeField] private float _bezerCurveMaxHeight = 3;
    [SerializeField] private float _bezerCurveMinHeight = 0.2f;
    [SerializeField, Range(0,1)] private float _bezierAnchorModifier = 0.5f;
    [Header("Retract Settings")]
    [SerializeField] private float _retractAnimationDuration = 0.5f;

    [Header("Tentacle Jump Animation Settings")]
    [SerializeField] private float _animationEndAnglePosition = 90;
    [SerializeField] private AnimationCurve _jumpAnimationCurve;
    [SerializeField, MinMaxSlider(0,5,ShowFields = true)] private Vector2 _jumpAnimationSpeedMinMax = new Vector2(1,3);
    [SerializeField, MinMaxSlider(0,1,ShowFields = true)] private Vector2 _angleMagnitudeModifier = new Vector2(0.8f,1);
    [SerializeField] private float _delayForSwitch = 0.3f;

    [Header("Wiggle Animation Settings")]
    [SerializeField] private float _minDistanceForWiggle = 0.5f;
    [SerializeField] private float _maxDistanceForWiggle = 12f;
    [SerializeField] private Vector2 _wiggleFrequencyMinAndMaxBasedOnDistance = Vector2.zero;
    [SerializeField] private Vector2 _wiggleAmplitudeMinAndMaxBasedOnDistance = Vector2.zero;

    [Button]
    public void AnimateLaunch(Vector3 anchorWorldPosition, Vector2 hitNormal)
    {
        _visual.ChangeVisualState(TentacleVisualState.Launching);
        _visual.SetFollowEndPosition(_visual.GetTentacleEndPosition());
        StartCoroutine(LaunchTentacle(anchorWorldPosition, hitNormal));
    }
    [Button]
    public void AnimateRetract()
    {
        StartCoroutine(RetractTentacle());
    }
    [Button]
    public void AnimateConnectProbe(Vector3 start, Vector3 end)
    {
        StartCoroutine(ConnectProbe(start, end));
    }
    public void AnimateJump(Transform freelookRootTransform, Vector2 startLookDirection, float direction)
    {
        StartCoroutine(JumpTentacleAnimation(freelookRootTransform, startLookDirection, direction));
    }

    private IEnumerator LaunchTentacle(Vector3 anchorWorldPosition, Vector2 hitNormal)
    {
        _visual.SetAutoConnectEnabled(false);
        float lerp = 0;
        float starTime = Time.time;
        Vector3 start = _visual.FollowTransform.position;
        Vector3 end = anchorWorldPosition;
        float bezerLerpValue = Mathf.InverseLerp(0, 8, Vector3.Distance(anchorWorldPosition, transform.position));
        float bezierHeight = Mathf.Lerp(_bezerCurveMinHeight, _bezerCurveMaxHeight, bezerLerpValue);
        Vector3 bezierAnchor = start + ((end - start)*_bezierAnchorModifier) + new  Vector3(hitNormal.x,hitNormal.y,0) * bezierHeight;
        Debug.DrawRay(start, hitNormal * 15, Color.red);
        Debug.DrawLine(start, bezierAnchor, Color.blue, 1);
        Debug.DrawLine(end, bezierAnchor, Color.blue, 1);
        while (lerp < 1)
        {
            lerp += _launchAnimationSpeed * Time.deltaTime;
            Vector3 startToAnchor = Vector3.Lerp(start, bezierAnchor, lerp);
            Vector3 anchorToEnd = Vector3.Lerp(bezierAnchor, end, lerp);
            Vector3 finalPosition = Vector3.Lerp(startToAnchor, anchorToEnd, lerp);
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
        _visual.SetAutoConnectEnabled(true);

        //Debug.Log("Launch elapsed " + (Time.time - starTime) + " by tentacle " + gameObject.transform.parent.name);
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
    private IEnumerator ConnectProbe(Vector3 start, Vector3 endPosition)
    {
        _visual.SetAutoConnectEnabled(false);
        float lerp = 0;
        _visual.ChangeVisualState(TentacleVisualState.Launching);
        while (lerp < 1)
        {
            lerp += _launchAnimationSpeed * Time.deltaTime;
            Vector3 finalPosition = Vector3.Lerp(start, endPosition, lerp);
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
        _visual.SetAutoConnectEnabled(true);

    }
    private void SetDistanceBasedWiggle()
    {
        float distance = Vector3.Distance(_visual.transform.position, _visual.FollowTransform.position);
        float lerp = Mathf.InverseLerp(_maxDistanceForWiggle, _minDistanceForWiggle, distance);
        //Debug.Log(lerp);
        lerp = distance > _maxDistanceForWiggle ? 1 : lerp;
        float distanceBasedWaveMagnitude = Mathf.Lerp(_wiggleAmplitudeMinAndMaxBasedOnDistance.y, _wiggleAmplitudeMinAndMaxBasedOnDistance.x, lerp);
        float distanceBasedWaveFrequency = Mathf.Lerp(_wiggleFrequencyMinAndMaxBasedOnDistance.y, _wiggleFrequencyMinAndMaxBasedOnDistance.x, lerp);
        _visual.SetWiggleVariables(distanceBasedWaveFrequency, distanceBasedWaveMagnitude);
    }

    private IEnumerator JumpTentacleAnimation(Transform freelookRootTransform, Vector2 startLookDirection, float direction = 1)
    {
        // set the visual state to retracting to get the correct parameters, then set the target mode to untargeted (disable targeted end position)
        _visual.ChangeVisualState(TentacleVisualState.Retracting);
        _visual.SetTargetedEndPositionEnabled(false);

        //set the initial object rotation to the start position given the x rotation parameter
        freelookRootTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, _animationEndAnglePosition));
        float lerp = 0;
        float delayElapsed = 0;
        float targetAngle = direction == 1 ? 270 : -90;
        targetAngle = targetAngle * Random.Range(_angleMagnitudeModifier.x, _angleMagnitudeModifier.y);
        float animSpeed = Random.Range(_jumpAnimationSpeedMinMax.x, _jumpAnimationSpeedMinMax.y);
        // init variables
        while (lerp < 1 || delayElapsed < _delayForSwitch)
        {
            if(lerp < 1)
            {
                // get the normalized value of elapsed (between 0 and 1)
                float evaluated = _jumpAnimationCurve.Evaluate(lerp);
                float newZrotationValue = Mathf.Lerp(_animationEndAnglePosition, targetAngle, evaluated);
                Vector3 targetRotation = new Vector3(0, 0, newZrotationValue);
                freelookRootTransform.localRotation = Quaternion.Euler(targetRotation);

                lerp += Mathf.Clamp(Time.deltaTime * animSpeed, 0,1);
            }
            else
            {
                delayElapsed += Time.deltaTime;
            }
            yield return new WaitForEndOfFrame();
        }
        _visual.ChangeVisualState(TentacleVisualState.Idle);
    }

}
