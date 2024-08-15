using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

// animates the follow transform object, which paired with the tentacle visual script that controls behavior, creates the procedural animations on the tentacles
public class TentacleCodeAnimator : MonoBehaviour
{
    [Header("Plug in Fields")]
    [SerializeField] private TentacleVisual _visual;

    [Header("Launch Animation Settings")]
    [SerializeField] private float _launchAnimationSpeed = 12f;
    [SerializeField] private float _bezerCurveMaxHeight = 3;
    [SerializeField] private float _bezerCurveMinHeight = 0.2f;
    [SerializeField] private float _maxLaunchDistanceForMaxHeightBezier = 8;
    [SerializeField, Range(0,1), Tooltip("The point betweent start and end positions where the peak of the curve will be placed")] 
    private float _bezierAnchorModifier = 0.5f;

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

    // public animation call methods
    [Button]
    public void AnimateLaunch(Vector3 anchorWorldPosition, Vector2 hitNormal)
    {
        _visual.ChangeVisualState(TentacleVisualState.Launching);
        // start the animation fron the current last segment position on the tentacle
        _visual.SetFollowEndPosition(_visual.GetTentacleEndPosition());

        // start animation to target position, given the normal of the target location to hit (used for the bezier curve)
        StartCoroutine(LaunchTentacle(anchorWorldPosition, hitNormal));
    }
    [Button]
    public void AnimateConnectProbe(Vector3 start, Vector3 end)
    {
        // call the animate probe (similar to launch, but without the curve)
        StartCoroutine(ConnectProbe(start, end));
    }
    [Button]
    public void AnimateRetract()
    {
        // call the retract animation
        StartCoroutine(RetractTentacle());
    }
    public void AnimateJump(Transform freelookRootTransform, Vector2 startLookDirection, float direction)
    {
        //similar to retract, but with the extra jump swing animation
        StartCoroutine(JumpTentacleAnimation(freelookRootTransform, startLookDirection, direction));
    }

    // private methods (the actual animations)
    private IEnumerator LaunchTentacle(Vector3 anchorWorldPosition, Vector2 hitNormal)
    {
        // disable auto connect to prevent tentacle from grabbing the terrain on the very start of the animation
        _visual.SetAutoConnectEnabled(false);
        
        //start up varaibles
        float lerp = 0;
        Vector3 start = _visual.FollowTransform.position;
        Vector3 end = anchorWorldPosition;

        // construct the bezier curve for evaluating the trajectory of the launch animation
        //first, modulate the max height of the curve beased on the distance of the launch (bigger trajectory, higher curve)
        float bezerLerpValue = Mathf.InverseLerp(0, _maxLaunchDistanceForMaxHeightBezier, Vector3.Distance(anchorWorldPosition, transform.position));

        //find the actual height to use based on the previously obtained value
        float bezierHeight = Mathf.Lerp(_bezerCurveMinHeight, _bezerCurveMaxHeight, bezerLerpValue);

        // generate the mid-point "anchor" for the bezier curve interpolation 
        // from the start, interpolate to the target position by the anchor modifier, multiply by the hit normal to get the correct direction, and finally multiply by height
        Vector3 bezierAnchor = start + ((end - start)*_bezierAnchorModifier) + new  Vector3(hitNormal.x,hitNormal.y,0) * bezierHeight;

        //// uncomment these to see the curves as debug draw lines
        //Debug.DrawRay(start, hitNormal * 15, Color.red);
        //Debug.DrawLine(start, bezierAnchor, Color.blue, 1);
        //Debug.DrawLine(end, bezierAnchor, Color.blue, 1);

        while (lerp < 1)
        {
            lerp += _launchAnimationSpeed * Time.deltaTime;
            // to evaluate the curve, lerp both from start to anchor, and from anchor to end
            Vector3 startToAnchor = Vector3.Lerp(start, bezierAnchor, lerp);
            Vector3 anchorToEnd = Vector3.Lerp(bezierAnchor, end, lerp);

            // then lerp between these two values for a nice curve
            // credits to Vivek Tank from GameDeveloper "How to work with Bezier Curve in Games with Unity" for this elegant little implementation
            Vector3 finalPosition = Vector3.Lerp(startToAnchor, anchorToEnd, lerp);

            // set the position as the follow target position
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
        // at the end, re-enable autoconnect
        _visual.SetAutoConnectEnabled(true);

        //Debug.Log("Launch elapsed " + (Time.time - starTime) + " by tentacle " + gameObject.transform.parent.name);
    }

    private IEnumerator RetractTentacle()
    {
        // enable wiggling motion and set retracting state
        _visual.ChangeVisualState(TentacleVisualState.Retracting);
        _visual.SetIsWiggling(true);

        //start up variables
        float lerp = 0;
        Vector3 startPos = _visual.FollowTransform.position;
        float speed = (1 / _retractAnimationDuration);

        while (lerp < 1)
        {
            // get next follow target position based on current anim speed
            lerp += speed * Time.deltaTime;
            Vector3 finalPosition = Vector3.Lerp(startPos, transform.position, lerp);

            // adjust the wiggle amount based on distance to player
            SetDistanceBasedWiggle();

            // set the position on the follow transform
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
        // disable wiggling and return to idle state
        _visual.SetIsWiggling(false);
        _visual.ChangeVisualState(TentacleVisualState.Idle);
    }
    private IEnumerator ConnectProbe(Vector3 start, Vector3 endPosition)
    {
        // disable autoconnect and set to launching state
        _visual.SetAutoConnectEnabled(false);
        _visual.ChangeVisualState(TentacleVisualState.Launching);

        //start the lerp float
        float lerp = 0;
        while (lerp < 1)
        {
            // simply lerp the positions based on the elapsed time
            lerp += _launchAnimationSpeed * Time.deltaTime;
            Vector3 finalPosition = Vector3.Lerp(start, endPosition, lerp);
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
        // re enable auto connect
        _visual.SetAutoConnectEnabled(true);

    }
    private void SetDistanceBasedWiggle()
    {
        // get distance to player from follow transform and inverse lerp to get the target wiggle amount
        float distance = Vector3.Distance(_visual.transform.position, _visual.FollowTransform.position);
        float lerp = Mathf.InverseLerp(_maxDistanceForWiggle, _minDistanceForWiggle, distance);

        // set lerp value to 1 if distance exceeds max
        lerp = distance > _maxDistanceForWiggle ? 1 : lerp;

        // lerp both magnitude and frequency based on the lerp value, and apply
        float distanceBasedWaveMagnitude = Mathf.Lerp(_wiggleAmplitudeMinAndMaxBasedOnDistance.y, _wiggleAmplitudeMinAndMaxBasedOnDistance.x, lerp);
        float distanceBasedWaveFrequency = Mathf.Lerp(_wiggleFrequencyMinAndMaxBasedOnDistance.y, _wiggleFrequencyMinAndMaxBasedOnDistance.x, lerp);
        _visual.SetWiggleVariables(distanceBasedWaveFrequency, distanceBasedWaveMagnitude);
    }

    private IEnumerator JumpTentacleAnimation(Transform freelookRootTransform, Vector2 startLookDirection, float direction = 1)
    {
        // set the visual state to retracting to get the correct parameters, then set the target mode to untargeted (disable targeted end position)
        _visual.ChangeVisualState(TentacleVisualState.Retracting);
        _visual.SetTargetedEndPositionEnabled(false);

        //set the initial object rotation to the start position given the z rotation parameter
        freelookRootTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, _animationEndAnglePosition));

        // start variables
        float lerp = 0;
        float delayElapsed = 0;
        // given the direction of the animation (1 or -1) set the target to equivalent angled facing to player backwards
        float targetAngle = direction == 1 ? 270 : -90;

        // randomize the target angle and anim speed for a bit of variability
        targetAngle = targetAngle * Random.Range(_angleMagnitudeModifier.x, _angleMagnitudeModifier.y);
        float animSpeed = Random.Range(_jumpAnimationSpeedMinMax.x, _jumpAnimationSpeedMinMax.y);

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
                // once the lerp value has gotten to 1, give the animation a little delay before going back to idle
                delayElapsed += Time.deltaTime;
            }
            yield return new WaitForEndOfFrame();
            // after both actions in the while are satisfied, then end the animation and go to idle
        }
        _visual.ChangeVisualState(TentacleVisualState.Idle);
    }

}
