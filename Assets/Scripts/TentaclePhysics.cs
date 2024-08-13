using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TentaclePhysics : MonoBehaviour
{
    [Header("Tentacle Settings")]
    [SerializeField] private List<Tentacle> _tentacles = new List<Tentacle>();
    [SerializeField] private float _implulseMagnitude = 1;

    [Header("Varied RB Settings")]
    [SerializeField] private float _linearDragConnected = 5;
    [SerializeField] private float _angularDragConnected = 5;
    [SerializeField] private float _linearDragFree = 0.2f;
    [SerializeField] private float _angularDragFree = 0.2f;

    [Header("Little Impulse Settings")]
    [SerializeField] private bool _useLittleImpulse = true;
    [SerializeField] private float _lilImpulseMaxSpeed = 5;
    [SerializeField] private float _lilAccelerationBoost = 1;
    [SerializeField] private float _notGroundedForceMultiplier = 0.1f;
    [SerializeField] private float _maxAngleFromNormal = 20f;

    [Header("On Surface Settings")]
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private float _groundSphereCastRadius = 1;

    [Header("Gravity Settings")]
    [SerializeField] private float _customGravityAcceleration = 20;
    [SerializeField] private float _xDirCorrectionAcceleration = 20;
    [SerializeField] private float _gravityDecreasePerConnectedTentacle = 0.2f;
    [SerializeField, ReadOnly] private float _currentGravityScale = 0;
    [SerializeField, ReadOnly] private float _currentXCorrectionScale = 0;
    [SerializeField, ReadOnly] private int _currentConnectedTentacles = 0;

    [Header("Jump Boost")]
    [SerializeField] private float _boostAccelerationMagnitude = 50f;
    [SerializeField, Tooltip("If false, will boost in target direciton instead")] private bool _boostInLookDirection = true;

    [Header("Misc.")]
    [SerializeField] private bool _debugEnabled = false;
    [SerializeField, ReadOnly] private bool _canGetToTargetWithCurrentTentacles = false;
    [SerializeField, ReadOnly] private Vector2 _finalVector = Vector2.zero;
    [SerializeField, ReadOnly] private Vector2 _totalContributionVector = Vector2.zero;
    [SerializeField, ReadOnly] private Vector2 _totalForcesWithoutContribution = Vector2.zero;

    public bool CanGetToTargetWithCurrentTentacles { get { return _canGetToTargetWithCurrentTentacles; } }

    public bool IsOnSurface = false;
    public Vector3 CurrentSurfaceNormal = Vector3.up;
    private PlayerController _controller;
    private TentacleMovement _movement;
    private Rigidbody2D _rigidBody;

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
        _controller = GetComponent<PlayerController>();
        _movement = GetComponent<TentacleMovement>();
    }

    //update methods
    private void FindFinalVector()
    {
        // zero out innitials
        _finalVector = Vector2.zero;
        _totalForcesWithoutContribution = Vector2.zero;
        _currentConnectedTentacles = 0;
        //if player has no movement input, return
        if (_controller.ReleasePressed) return;

        Vector2 totalContribution = Vector2.zero;
        Vector2 totalForces = Vector2.zero;

        // for every ACTIVE and ANCHORED tentacle, get their contribution vector and add the forces to the total available
        for (int i = 0; i < _tentacles.Count; i++)
        {
            if(_tentacles[i].IsAnchored && _tentacles[i].gameObject.activeInHierarchy)
            {
                totalContribution += _tentacles[i].ContributionVector;
                _totalForcesWithoutContribution += _tentacles[i].PlayerToAnchorVector;
                _currentConnectedTentacles++;
            }
        }
        _totalContributionVector = totalContribution;

        // split the forces from the total and clamp them relative to the current target direction
        // impirtant note, the foreces can be negative, so do allow for negative values by considering the signs of the total controbution axis
        float clampedXcontribution = Mathf.Clamp(Mathf.Abs(totalContribution.x), 0, Mathf.Abs(_movement.TargetDirectionRaw.x)) * Mathf.Sign(totalContribution.x);
        float clampedYcontribution = Mathf.Clamp(Mathf.Abs(totalContribution.y), 0, Mathf.Abs(_movement.TargetDirectionRaw.y)) * Mathf.Sign(totalContribution.y);

        // assigned the clamped values as the possible contribution (max force available) in each axis
        _finalVector = new Vector2(clampedXcontribution, clampedYcontribution);
        if(_debugEnabled) Debug.DrawRay(transform.position, _finalVector, Color.magenta);
    }

    private void ImpulseByTentacles()
    {
        // assign the gravity (y correction force) and the X correction force (to prevent unrealisticly "pushing" tentacles) relative to # of connected tentacles
        _currentGravityScale = _customGravityAcceleration - (_gravityDecreasePerConnectedTentacle * _currentConnectedTentacles * _customGravityAcceleration);
        _currentXCorrectionScale = _xDirCorrectionAcceleration - (_gravityDecreasePerConnectedTentacle * _currentConnectedTentacles * _xDirCorrectionAcceleration);

        // calculate the final force after corrections
        Vector2 finalFinalForce = (_finalVector * _implulseMagnitude) + new Vector2(_totalForcesWithoutContribution.x * _currentXCorrectionScale, (Vector2.down * _currentGravityScale).y);

        //Debug.DrawRay(transform.position, finalFinalForce, Color.black);

        // aplly force to rb
        _rigidBody.AddForce(finalFinalForce);
    }
    private bool CheckSurface()
    {
        // get the normal of the surface currently in touch (if any)
        RaycastHit2D[] hitInfo = new RaycastHit2D[1];
        int hit = Physics2D.CircleCastNonAlloc(transform.position, _groundSphereCastRadius, Vector2.zero, hitInfo, 0, _groundLayers);
        if (hit > 0)
        {
            CurrentSurfaceNormal = hitInfo[0].normal;
            return true;
        }
        return false;
    }
    // adjust the drag and angular drag of the octupus on free flight (allows for nicer jumping archs, without the springy behavior while connected to tentacles)
    private void AdjustDrag()
    {
        if (_finalVector == Vector2.zero)
        {
            _rigidBody.angularDrag = _angularDragFree;
            _rigidBody.drag = _linearDragFree;
        }
        else
        {
            _rigidBody.angularDrag = _angularDragConnected;
            _rigidBody.drag = _linearDragConnected;
        }
    }
    private void FixedUpdate()
    {
        //check for attached surface (if any)
        IsOnSurface = CheckSurface();

        //check if the player can get to current target location with tentacles already in place
        _canGetToTargetWithCurrentTentacles = Vector2.Distance(_finalVector, _movement.TargetDirectionRaw) < 0.5f;

        //call the update methods
        FindFinalVector();
        ImpulseByTentacles();
        AdjustDrag();
    }

    // input methods 
    public void TryGiveFreeImpulse(Vector3 targetDirectionNormalized, int activeTentacleCount)
    {
        if (_controller.HasActiveInput && _useLittleImpulse)
        {
            //if not connected and on ground
            if(activeTentacleCount == 0 && IsOnSurface)
            {
                Vector3 finalImpulseDirection = Vector3.RotateTowards(CurrentSurfaceNormal, targetDirectionNormalized, _maxAngleFromNormal * Mathf.Deg2Rad, 0);
                _rigidBody.AddForce(_rigidBody.mass * ImpulseAcceleration(finalImpulseDirection));
            }
            else if (activeTentacleCount > 0)
            {
                _rigidBody.AddForce(_rigidBody.mass * ImpulseAcceleration(targetDirectionNormalized) * _notGroundedForceMultiplier);
            }
        }
    }
    // give the jump boost when detaching all tentacles 
    public void GiveDetachAllBost()
    {
        Vector3 forceVector = _boostInLookDirection ? _controller.LookDirection.normalized : _movement.TargetDirectionNormalized;
        _rigidBody.AddForce(forceVector * _boostAccelerationMagnitude);
    }
    public Vector2 ImpulseAcceleration(Vector2 targetDirectionNormalized)
    {
        Vector2 targetVelocity = _lilImpulseMaxSpeed * targetDirectionNormalized.normalized;
        Vector2 velocityDiff = targetVelocity - _rigidBody.velocity;
        Vector2 acceleration = velocityDiff * _lilAccelerationBoost;
        return acceleration;
    }
    public void SetBoostInLookDirection(bool boostInDirection)
    {
        _boostInLookDirection = boostInDirection;
    }

    // debug 
    private void OnDrawGizmos()
    {
        if(_debugEnabled)
        {
            foreach (Tentacle tentacle in _tentacles)
            {
                if(tentacle.IsAnchored)
                {
                    Gizmos.DrawLine(transform.position, tentacle.PlayerToAnchorVector + Get2DPosition());
                }
            }
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _finalVector + Get2DPosition());
        }
    }

    private Vector2 Get2DPosition()
    {
        return new Vector2(transform.position.x, transform.position.y);
    }
}
