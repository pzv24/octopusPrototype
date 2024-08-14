using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

// visual manager for tentacles. Only mangages tentacle position behavior in the different states 
// the actual animations (movement of the look at transfom and free look transform) managed by the tentacle code animator
public class TentacleVisual : MonoBehaviour
{
    [Header("Plug in Fields")]
    [SerializeField] private Transform _followEndTransform;
    [SerializeField] private Transform _freeMoveTentacleTransform;
    [SerializeField] private Transform _freeMoveTentacleAnimatedTransform;
    [SerializeField] private LookAtMouse _headController;

    [Header(" Movement Settings")]
    [SerializeField] private int _tentacleSegmentCount = 50;
    [SerializeField] private float _baseSmoothSpeed = 10f;
    [SerializeField] private float _tentacleLengthModifier = 1;

    [Header("Wiggle Settings")]
    [SerializeField] private bool _isWiggling = false;
    [SerializeField] private bool _randomizeStartDirection = true;
    [SerializeField] private float _waveFrequency = 1;
    [SerializeField] private float _waveMagnitude = 1;
    private Transform _wiggledEndTransfrom;
    private float _wiggleTime = 0;

    [Header("Connection Settings")]
    [SerializeField] private bool _setAutoConnect = true;
    [SerializeField] private float _autoConnectDistance = 0.3f;

    [Header("Idle Settings")]
    [SerializeField] private bool _targetedEndPosition = true;
    [SerializeField] private float _idleTentacleLength = 6;
    [SerializeField] private float _idleSmoothOverride = 0.1f;

    [Header("States Info")]
    [SerializeField] private TentacleVisualState _visualState = TentacleVisualState.Idle;
    [SerializeField] private TentacleVisualState _defaultState = TentacleVisualState.Idle;
    [SerializeField] private float _connectedSmoothFactor = 20f;
    [SerializeField] private float _launchingSmoothFactor = 5000f;
    [SerializeField] private float _looseSmoothFactor = 2500f;
    [SerializeField, ReadOnly] private float _currentSmoothFactor = 0;

    [Header("Tentacle debug colors")]
    [SerializeField] private bool _changeColorOnState = false;
    [SerializeField] private Gradient _original;
    [SerializeField] private Gradient _isLaunchignColor;
    [SerializeField] private Gradient _isConnectedColor;

    private LineRenderer _lineRenderer;
    private Tentacle _tentacleCore;
    private Vector3[] _segmentPositions;
    private Vector3[] _segmentVelocities;
    private float _connectedModifier = 1;
    private float _idleTentacleSeparationPerSegment;
    private TentacleIdleAnimation _idleAnimator;

    public Transform FollowTransform { get { return _followEndTransform; } }

    private void Start()
    {
        // get components
        _tentacleCore = GetComponentInParent<Tentacle>();
        _idleAnimator = _freeMoveTentacleAnimatedTransform.GetComponent<TentacleIdleAnimation>();

        //allows to initialize the visual side of gameplay element missing (for testing purposes)
        if(_tentacleCore == null)
        {
            InitVisual(_followEndTransform);
        }

        // set visual state to idle and calculate the idle tentacle per segment constant based on parameters
        ChangeVisualState(TentacleVisualState.Idle);
        _idleTentacleSeparationPerSegment = _idleTentacleLength / _tentacleSegmentCount;
    }

    private void OnValidate()
    {
        ChangeVisualState(_visualState);
    }

    public void InitVisual(Transform anchor)
    {
        // innitilaize values on the line renderer
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _tentacleSegmentCount;
        _segmentPositions = new Vector3[_tentacleSegmentCount];
        _segmentVelocities = new Vector3[_tentacleSegmentCount];

        // create the "wiggle posisition" obect as a child of the follow end transform
        GameObject wigglePos = new GameObject("WigglePoint");
        wigglePos.transform.parent = _followEndTransform;
        _wiggledEndTransfrom = wigglePos.transform;

        // reset tentacle position and variables for a clean start
        ForceResetPosition();
    }
    private void Update()
    {
        // fixes bugs with weird tentacle innitialization values for the first few frames of the game
        if(Time.time < 0.5f)
        {
            ForceResetPosition();
            return;
        }
        // get the real final target position after tentacle "wiggle" is accounted for 
        GetFinalTargetPosition();

        // fix the tentacle textures based on position
        SetTextureBasedOnPlayerPosition();

        // if there is a head connected, move the freemove tentacle transform to face 90 degrees down from the player's look angle (so, directly down)
        if(_headController != null)
        {
            float lookAngle = Mathf.Atan2(_headController.PlayerApparentUp.x, _headController.PlayerApparentUp.y) * Mathf.Rad2Deg;
            _freeMoveTentacleTransform.rotation = Quaternion.AngleAxis(lookAngle + 90, -Vector3.forward);
        }
        // set the first line renderer segment position to the root
        _segmentPositions[0] = transform.position;

        FindTentacleLineRendererPositions();

        // apply the new positions to the line renderer
        _lineRenderer.SetPositions(_segmentPositions);

        // if tentacle set to autoconnect, then tentacle will automatically stick to a surface when its close enough
        if(_setAutoConnect 
            && _visualState == TentacleVisualState.Launching 
            && Vector3.Distance(_segmentPositions[_segmentPositions.Length - 1], _followEndTransform.position) < _autoConnectDistance
            && !_targetedEndPosition)
        {
            ChangeVisualState(TentacleVisualState.Connected);
            //Debug.Log("tentacle "+ this.gameObject.name +" auto connected");
        }
    }
    private void GetFinalTargetPosition()
    {
        // if retracted, make the targed end position match the root (to hide the tentacle fully)
        if (_visualState == TentacleVisualState.Retracted)
        {
            _followEndTransform.position = transform.position;
        }
        // if wiggle is enabled
        if (_isWiggling)
        {
            //increase the sine wave iterator
            _wiggleTime += Time.deltaTime;

            // set the final target direction for the entire tentacle based on current final position target
            Vector3 finalTargetDirection = (_followEndTransform.position - transform.position).normalized;

            // rotate the player to anchor vector 90 degrees to get a perpendicular motion
            float angle = Mathf.Atan2(finalTargetDirection.y, finalTargetDirection.x) * Mathf.Rad2Deg - 90;
            _followEndTransform.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // move the wiggle end transform target location on its x axis based ona sine wave to create the wiggle motion
            _wiggledEndTransfrom.localPosition = new Vector3(Mathf.Sin(_waveFrequency * _wiggleTime) * _waveMagnitude, 0, 0);
        }
        else
        {
            // else, just make the wiggle position match the target
            _wiggledEndTransfrom.position = _followEndTransform.position;
        }
    }
    private void SetTextureBasedOnPlayerPosition()
    {
        // change the scale of the line renderer y coordinate texture based on player to anchor vector of the tentacle (so suckers face downwards relative to player)
        if (_visualState == TentacleVisualState.Idle) return;
        _lineRenderer.textureScale = new Vector2(_lineRenderer.textureScale.x, Mathf.Sign(_tentacleCore.PlayerToAnchorVector.x) * -1);
    }
    private void FindTentacleLineRendererPositions()
    {
        // calculate the distance per segment based on target distance
        float distancePerSegment = Vector3.Distance(_wiggledEndTransfrom.position, transform.position) * _tentacleLengthModifier / (_tentacleSegmentCount - 1);

        // set the final target direction for the entire tentacle based on current final position target
        Vector3 finalTargetDirection = (_wiggledEndTransfrom.position - transform.position).normalized;
        Vector3 targetPosition = Vector3.zero;
        Vector3 newSegmentPosition = Vector3.zero;
        // iterate on the points on the line renderer to update positions
        for (int i = 1; i < _segmentPositions.Length; i++)
        {
            // calculate the smooth factor:
            // if detached, slow down the the smooth modifier towards the tip
            // if connected, the opporite, massively increase the speed of the entire tentagle (making it rigid), specially towards the end point
            float smoothSpeed = ((_baseSmoothSpeed + i) / _currentSmoothFactor) / _connectedModifier;
            smoothSpeed = _visualState == TentacleVisualState.Idle? _idleSmoothOverride + (_idleSmoothOverride * (i/_tentacleSegmentCount)) : smoothSpeed;

            if (_targetedEndPosition)
            {
                // for each point, set the target position based on the previous point, plus the direction * disntace 
                targetPosition = _segmentPositions[i - 1] + finalTargetDirection * distancePerSegment;
            }
            // if the tentacle is in freemove, then base the position on the freemove tentacle root transfrom instead
            else
            {
                targetPosition = CalculateIdleTargetPosition(i);
            }

            //calculate the position with smooth damp function
            newSegmentPosition = Vector3.SmoothDamp(_segmentPositions[i], targetPosition, ref _segmentVelocities[i], smoothSpeed);
            newSegmentPosition = IdleRopeLikeCheck(newSegmentPosition, i);

            // store the new position for that segment
            _segmentPositions[i] = newSegmentPosition;
        }
    }

    private Vector3 CalculateIdleTargetPosition(int segmentIndex)
    {
        return _segmentPositions[segmentIndex - 1] + _freeMoveTentacleAnimatedTransform.right * _idleTentacleSeparationPerSegment;
    }
    private Vector3 IdleRopeLikeCheck(Vector3 newSegmentPosition, int i)
    {
        // if the tentacle is in idle, and the tentacle would need to "stretch", switch target position to be only based on a fixed distance from the 
        // previous tentacle. Allow for that "rope-like" movement when carrying the idle tentacles around
        if (_visualState == TentacleVisualState.Idle && Vector3.Distance(newSegmentPosition, _segmentPositions[i - 1]) > _idleTentacleSeparationPerSegment)
        {
            return _segmentPositions[i - 1] + (_segmentPositions[i] - _segmentPositions[i - 1]).normalized * _idleTentacleSeparationPerSegment;
        }
        else return newSegmentPosition;
    }
    private  void ForceResetPosition()
    {
        // sets all positions to zero
        for (int i = 1; i < _segmentPositions.Length; i++)
        {
            _segmentPositions[i] = Vector3.zero;
        }
    }

    // public call methods
    public void SetFollowEndPosition(Vector3 worldPosition)
    {
        // set the follow end transform (this affects the visual "target" of the tentacle, used for animation)
        _followEndTransform.position = worldPosition;
    }
    public void SetIsWiggling(bool isWiggling)
    {
        // if is wiggling, restart the timer
        if (isWiggling)
        {
            _wiggleTime = 0;
            //randomize start direction of the sine wave 
            if (_randomizeStartDirection)
            {
                int random = Random.Range(0, 2);
                bool startDown = random == 0;
                _wiggleTime = startDown ? Mathf.PI*2 : Mathf.PI * -2;
            }
        }
        _isWiggling = isWiggling;
    }

    // used for distance based wiggle variables adjustment 
    public void SetWiggleVariables(float wiggleFrequency, float wiggleMagnitude)
    {
        _waveFrequency = wiggleFrequency;
        _waveMagnitude = wiggleMagnitude;
    }
    // set enabled for cisual auto connect
    public void SetAutoConnectEnabled(bool autoConnectEnabled)
    {
        _setAutoConnect = autoConnectEnabled;
    }
    // set the y coordinate of the line renderer material (should only take 1 or -1 as values to avoid distortion)
    public void SetTentacleTextureScale(float scale)
    {
        if ((scale == 1f || scale == -1f) && _lineRenderer != null)
        {
            _lineRenderer.textureScale = new Vector2(_lineRenderer.textureScale.x, scale);
        }
    }
    // return the position of the tip of the tentacle
    public Vector3 GetTentacleEndPosition()
    {
        return _lineRenderer.GetPosition(_tentacleSegmentCount - 1);
    }
    // set targeted or root based tentacle movement
    public void SetTargetedEndPositionEnabled(bool enabled)
    {
        _targetedEndPosition = enabled;
    }
    // change the visual state of the tentacle
    // currently a bit rudimentary. To-do: this to an actual FSM
    public void ChangeVisualState(TentacleVisualState state)
    {
        //Debug.Log("Tentacle " + gameObject.transform.parent.name + " previous state was " + _visualState + " and just changed to " + state, gameObject);
        _visualState = state;

        // since this behaviors only apply to idle, handle them before the switch
        _targetedEndPosition =! (_visualState == TentacleVisualState.Idle);
        _idleAnimator?.SetIdleAnimationEnabled(_visualState == TentacleVisualState.Idle);

        // set the smooth factors and connected modifiers based on visual state
        // for debug purposes, change the color of the tentacle based on state (if enabled)
        switch (_visualState)
        {
            case TentacleVisualState.Connected:
                _currentSmoothFactor = _launchingSmoothFactor;
                _connectedModifier = _connectedSmoothFactor;
                if (_changeColorOnState && _lineRenderer != null) _lineRenderer.colorGradient = _isConnectedColor;
                break;
            case TentacleVisualState.Retracted:
                _currentSmoothFactor = _launchingSmoothFactor;
                _connectedModifier = _connectedSmoothFactor;
                if (_changeColorOnState && _lineRenderer != null) _lineRenderer.colorGradient = _original;

                break;
            case TentacleVisualState.Launching:
                _currentSmoothFactor = _launchingSmoothFactor;
                _connectedModifier = 1;
                if (_changeColorOnState && _lineRenderer != null) _lineRenderer.colorGradient = _isLaunchignColor;

                break;
            case TentacleVisualState.Idle:
                _currentSmoothFactor = _launchingSmoothFactor;
                _connectedModifier = 1;
                if (_changeColorOnState && _lineRenderer != null) _lineRenderer.colorGradient = _original;

                break;
            default:
                _currentSmoothFactor = _looseSmoothFactor;
                _connectedModifier = 1;
                if (_changeColorOnState && _lineRenderer != null) _lineRenderer.colorGradient = _original;

                break;
        }
    }
    //debug
    private void OnDrawGizmos()
    {
        if(_wiggledEndTransfrom != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_wiggledEndTransfrom.position, 0.1f);
        }
    }
}
