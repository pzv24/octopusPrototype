using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

// tentacle base class, manages the gameplay aspects of the tentacle (tenteacle connect anchor, connected states, raycasts, tentacle probing)
public class Tentacle : MonoBehaviour
{
    [Header("Plug in Fields")]
    [SerializeField] private TentacleVisual _tentacleVisual;
    [SerializeField] private TentacleCodeAnimator _tentacleAnimation;
    [SerializeField] private Rigidbody2D _playerRB;
    [SerializeField] private Transform _anchor;
    [SerializeField] private Transform _freeLookRoot;

    [Header("Tentacle Settings")]
    [SerializeField] private bool _tentacleAnchored = true;
    [SerializeField] private float _breakDistance = 10;

    [Header("Tentacle Probing")]
    [SerializeField] private bool _tentacleProbing = false;
    [SerializeField] private LayerMask _bothTerrainLayers;
    [SerializeField] private float _circleCastRadius = 0.3f;

    [Header("Break Settings")]
    [SerializeField] private LayerMask _solidGroundLayer;
    [SerializeField] private bool _breakOnLooseDirectSight = true;
    [SerializeField, Range(0, 360)] private float _selfBreakAngleThreshold = 90;

    [SerializeField, ReadOnly] private Vector2 _contributionVector = Vector2.zero;
    [SerializeField, ReadOnly] private Vector2 _playerToAnchorVector = Vector2.zero;

    public Vector2 AnchorNormal { get; private set; }
    public bool IsAnchored { get { return _tentacleAnchored; } }
    public Vector2 ContributionVector { get { return _contributionVector; } }

    public Vector2 PlayerToAnchorVector
    {
        get
        {
            return new Vector2(_anchor.position.x, _anchor.position.y) - new Vector2(transform.position.x, transform.position.y);
        }
    }

    private TentacleMovement _movement;
    private void Start()
    {
        _movement = GetComponentInParent<TentacleMovement>();
        _tentacleVisual.InitVisual(_anchor);
    }
    private void FixedUpdate()
    {
        //if the tentacle is actually connected, calculate contribution and check for break conditions
        if (_tentacleAnchored && !_tentacleProbing)
        {
            Debug.DrawRay(_anchor.transform.position, AnchorNormal, Color.red);
            // test for auto break conditions
            if (AutoBreakConnection())
            {
                _movement.TentacleSelfDeactivate(this);
            }
            CalculateContributionVector();
        }
        // update the player to anchor vector
        _playerToAnchorVector = PlayerToAnchorVector;
    }
    // private update methods
    private bool AutoBreakConnection()
    {
        // if tentacle extends beyond max range, return true
        if (_playerToAnchorVector.magnitude > _breakDistance) return true;

        // if enabled, when tentacle breaks direct "sight" to anchor, break connection
        if (_breakOnLooseDirectSight && !HasDirectVisionOfAnchor()) return true;

        return false;
    }
    private void CalculateContributionVector()
    {
        // since tentacles can only pull (give a force in the player -> anchor direction), build the contrubution vector from the 
        // axis the align in direction to the target direction given by the player 
        // aka, you can only move towards the anchor
        float xContribution = 0;
        float yContribution = 0;
        if (_movement.TargetDirectionRaw.magnitude > 0.01f)
        {

            // if the current x component is in the same direction (sign) as the target direction
            if (Mathf.Sign(_movement.TargetDirectionRaw.x) == Mathf.Sign(PlayerToAnchorVector.x))
            {
                xContribution = PlayerToAnchorVector.x;
            }
            if (Mathf.Sign(_movement.TargetDirectionRaw.y) == Mathf.Sign(PlayerToAnchorVector.y))
            {
                yContribution = PlayerToAnchorVector.y;
            }
        }
        _contributionVector = new Vector2(xContribution, yContribution);
    }
    private bool HasDirectVisionOfAnchor()
    {
        //raycast towards anchor, see if it hit a wall on the way
        RaycastHit2D[] hitInfo = new RaycastHit2D[1];
        int hits = Physics2D.RaycastNonAlloc(transform.position, PlayerToAnchorVector.normalized, hitInfo, PlayerToAnchorVector.magnitude, _solidGroundLayer);
        if (hits == 0)
        {
            return true;
        }
        if (Mathf.Abs(hitInfo[0].distance - PlayerToAnchorVector.magnitude) > 0.5f)
        {
            return false;
        }
        return true;
    }
    // timer to force activate the gameplay connected functionality regardless of current animation of tentacle 
    // prevents animation bugs to propagating into gameplay
    private IEnumerator GameplayConnectedTimer(float connectSpeed)
    {
        float lerp = 0;
        while (lerp <= 1)
        {
            lerp += Time.deltaTime * connectSpeed;
            yield return new WaitForFixedUpdate();
        }
        _tentacleAnchored = true;

    }
    // public call methods
    public void LaunchTentacle(Vector3 anchorEndPosition, Vector2 hitNormal, float travelSpeed = 1f)
    {
        // disable tentacle probing (if valid)
        _tentacleProbing = false;

        //change the tentacle visual state to launching
        _tentacleVisual.ChangeVisualState(TentacleVisualState.Launching);

        // send the call to animate the launch and assign the current anchor normal direction 
        _tentacleAnimation.AnimateLaunch(anchorEndPosition, hitNormal);
        AnchorNormal = hitNormal;

        // start the fail-safe gameplay connected timer
        StartCoroutine(GameplayConnectedTimer(travelSpeed));
    }
    public void DeactivateTentacle()
    {
        // deactivate variables and send the call to animate the retract for the tentacle
        _tentacleAnchored = false;
        _tentacleProbing = false;
        _tentacleAnimation.AnimateRetract();
    }
    public void DeactivateJumpTentacle(float direction)
    {
        // deactivate the tentacle, but call for the jump animation instead
        _tentacleAnchored = false;
        _tentacleAnimation.AnimateJump(_freeLookRoot, PlayerToAnchorVector, direction);
    }
    public void SetTentacleProbing(bool probing)
    {
        // set tentacle to probing behavior (actual tentacle probing target position managed by movement script)
        _tentacleProbing = probing;
        if(probing)
        {
            //Debug.Log($"Tentacle {gameObject.name} is now probing...");
            _tentacleAnchored = false;
            _tentacleVisual.ChangeVisualState(TentacleVisualState.Retracting);
        }
    }
    // connect tentacle to anchor, called when this tentacle is being used as probe
    public void ConnectProbe(Vector3 anchorTargetPosition)
    {
        _tentacleAnchored = true;
        _tentacleProbing = false;
        _tentacleAnimation.AnimateConnectProbe(_anchor.position, anchorTargetPosition);

        StartCoroutine(GameplayConnectedTimer(10));
    }
    // set the gameplay tentacle anchor position
    public void SetAnchorPosition(Vector3 position)
    {
        _anchor.position = position;
    }
    
    private void OnDrawGizmos()
    {
        if (_tentacleProbing)
        {
            Gizmos.DrawWireSphere(_anchor.position, _circleCastRadius);
        }
    }
}
