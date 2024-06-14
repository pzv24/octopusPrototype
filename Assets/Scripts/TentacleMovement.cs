using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Runtime.InteropServices.WindowsRuntime;

public class TentacleMovement : MonoBehaviour
{
    [Header("Tentacle Plug in Fields")]
    [SerializeField] private List<Tentacle> _tentacleBank = new List<Tentacle>();
    [SerializeField] private List<Tentacle> _activeTentacles = new List<Tentacle>();
    [SerializeField] private Tentacle _tentaclePrefab;
    [SerializeField, ReadOnly] private Tentacle _probingTentacle;

    [Header("Tentacle Settings")]
    [SerializeField] private int _totalTentacleCount;
    [SerializeField] private float _tentacleFireCooldown =1f;
    [SerializeField] private float _tentacleMaxDistance;
    [SerializeField] private float _tentacleLaunchSpeed = 3;
    [SerializeField] private int _maxActiveTentacles = 3;
    [SerializeField] private LayerMask _tentacleCollisionLayers;
    [SerializeField] private float _jumpCooldown = 0.5f;

    [Header("Tentacle Raycast Settings")]
    [SerializeField] private int _raycastConeCount = 10;
    [SerializeField] private int _raycastDepth = 3;
    [SerializeField] private float _angleWidth = 40f;

    [Header("Debug")]
    [SerializeField] private bool _debugEnabled = false;
    [SerializeField] private bool _hasActiveInput = true;
    [SerializeField, ReadOnly] private Vector3 _targetLocation = Vector3.zero;
    [SerializeField, ReadOnly] private float _tentacleChangeElapsed = 0;

    public int ActiveTentacleCount { get { return _activeTentacles.Count; } }
    public Vector2 TargetDirectionNormalized { get { return (_targetLocation - transform.position).normalized; } }
    public Vector2 TargetDirectionRaw { get { return (_targetLocation - transform.position); } }

    public List<Tentacle> ActiveTentacles { get { return _activeTentacles; } }

    private TentaclePhysics _tentaclePhysics;
    private float _lastJumpTime = 0;
    private void Start()
    {
        _tentacleChangeElapsed = 0;
        _tentaclePhysics = GetComponent<TentaclePhysics>();
        ReleaseAllTentacles();
    }
    private bool TryChangeTentacleAnchor()
    {
        if (_tentacleChangeElapsed > _tentacleFireCooldown && _hasActiveInput)
        {
            _tentacleChangeElapsed = 0;
            //raycast to see if we find a new anchor location
            RaycastHit2D hit = RaycastConeAndChoose();
            Vector2 newAnchorPosition = hit.point;
            if(_debugEnabled) Debug.DrawLine(transform.position, newAnchorPosition, Color.green);

            // if location valid, then launch a new tentacle in that direction
            if (newAnchorPosition != Vector2.zero)
            {
                MoveTentacleAnchor(newAnchorPosition, hit.normal);
                return true;
            }
        }
        return false;
    }
    private void FixedUpdate()
    {
        _tentacleChangeElapsed += Time.deltaTime;
        // if does not have active input, return 
        if (!_hasActiveInput) return;
        ////if has probe: move probe's anchor to target location (clamped by max distance)
        //if(_probingTentacle != null)
        //{
        //    float clampedMagnitude = Mathf.Clamp((transform.position - _targetLocation).magnitude, 0, _tentacleMaxDistance);
        //    Vector3 clampedDirection = TargetDirectionNormalized * clampedMagnitude;
        //    _probingTentacle.SetAnchorPosition(transform.position + clampedDirection);
        //}
        //else if !canGetWithCurrent: Fire tentacle 
        //  if hit, launch, else begin probing

        if (!_tentaclePhysics.CanGetToTargetWithCurrentTentacles)
        {
            bool tentacleRaycastSucessful = TryChangeTentacleAnchor();
            if(!tentacleRaycastSucessful)
            {
                if(_probingTentacle == null)
                {
                    Tentacle tentacle = GetNextTentacle();
                    tentacle.SetTentacleProbing(true);
                    _probingTentacle = tentacle;
                }
                float clampedMagnitude = Mathf.Clamp((transform.position - _targetLocation).magnitude, 0, _tentacleMaxDistance);
                Vector3 clampedDirection = TargetDirectionNormalized * clampedMagnitude;
                _probingTentacle.SetAnchorPosition(transform.position + clampedDirection);
            }
        }
        else if(_tentaclePhysics.CanGetToTargetWithCurrentTentacles && _probingTentacle != null)
        {
            ReleaseProbingTentacle();
        }
    }

    public void SetTargetLocation(Vector3 targetLocation)
    {
        _targetLocation = targetLocation;
        if (targetLocation != Vector3.zero && _hasActiveInput)
        {
            Vector2 targetDirection = (_targetLocation - transform.position).normalized;
            _tentaclePhysics.TryGiveFreeImpulse(targetDirection, ActiveTentacleCount);
        }
        if (_debugEnabled) Debug.DrawLine(transform.position, _targetLocation, Color.yellow);
    }

    public void ReleaseAllTentacles()
    {
        if (_lastJumpTime + _jumpCooldown <= Time.time)
        {
            for (int i = 0; i < _activeTentacles.Count; i++)
            {
                _activeTentacles[i].DeactivateTentacle();
                _tentacleBank.Add(_activeTentacles[i]);
            }
            _tentaclePhysics.GiveDetachAllBost();
            _activeTentacles.Clear();
            _lastJumpTime = Time.time;
        }
    }
    public void ReleaseProbingTentacle()
    {
        if(_probingTentacle != null)
        {
            _probingTentacle.DeactivateTentacle();
            _probingTentacle.SetTentacleProbing(false);
            _tentacleBank.Add(_probingTentacle);
            _activeTentacles.Remove(_probingTentacle);
            _probingTentacle = null;
        }
    }
    public void SetHasActiveInput(bool hasActiveInput)
    {
        _hasActiveInput = hasActiveInput;
    }
    private RaycastHit2D RaycastTentacle(Vector2 directionalVector)
    {
        RaycastHit2D[] hitInfo = new RaycastHit2D[_raycastDepth];
        int hits = Physics2D.RaycastNonAlloc(transform.position, directionalVector, hitInfo, _tentacleMaxDistance, _tentacleCollisionLayers);
        if(_debugEnabled) Debug.DrawRay(transform.position, directionalVector * _tentacleMaxDistance, Color.cyan);
        if (hits == 0)
        {
            return hitInfo[0];
        }
        for (int i  = 0; i < hits; i++)
        {
            // if you hit a solid wall, that's the farthest you can go already
            if (hitInfo[i].collider.gameObject.layer == LayerMask.NameToLayer("SolidTerrain"))
            {
                return hitInfo[i];
            }
            continue;
        }
        // if no solid walls were hit, return the farthest point
        return hitInfo[hits - 1];
    }

    //raycast in a cone and return the farthest reaching vector
    private RaycastHit2D RaycastConeAndChoose()
    {
        // get the target position and make it a Vector2 directional vector
        Vector2 targetDirection = (_targetLocation - transform.position).normalized;
        Vector2 vector2Target = new Vector2(targetDirection.x, targetDirection.y);

        // calculate the spet length for the angle lerp
        float stepLength = (_angleWidth / _raycastConeCount);

        // calculate the start (left-most angle of the cone)
        Vector2 coneStart = ReturnRotatedVectorByXDegrees(vector2Target, -_angleWidth/2);

        // prep the arary for the best hits for each
        RaycastHit2D[] allHits = new RaycastHit2D[_raycastConeCount];

        // do the raycast
        for (int i = 0; i < _raycastConeCount; i++)
        {
            Vector2 raycastVector = ReturnRotatedVectorByXDegrees(coneStart, stepLength * i);
            RaycastHit2D outputHit = RaycastTentacle(raycastVector);
            allHits[i] = outputHit;
            if(_debugEnabled) Debug.DrawRay(transform.position, raycastVector * _tentacleMaxDistance, Color.yellow);
        }
        // stash the current best hit and its distance to target positon
        RaycastHit2D bestHit = allHits[0];
        float bestHidDistanceTotarget = Vector2.Distance(bestHit.point, _targetLocation);
        //cycle through all hits, choosing a new best if distance is smaller than current best
        for (int i = 0; i < allHits.Length;i++)
        {
            float currentDistanceToTarget = (Vector2.Distance(allHits[i].point, _targetLocation));
            if (currentDistanceToTarget < bestHidDistanceTotarget)
            {
                bestHit = allHits[i];
                bestHidDistanceTotarget = currentDistanceToTarget;
            }
        }
        return bestHit;
    }

    private Vector2 ReturnRotatedVectorByXDegrees(Vector2 vector, float degrees)
    {
        return new Vector3(
            vector.x * Mathf.Cos(Mathf.Deg2Rad * degrees) - vector.y * Mathf.Sin(Mathf.Deg2Rad * degrees),
            vector.y * Mathf.Cos(Mathf.Deg2Rad * degrees) + vector.x * Mathf.Sin(Mathf.Deg2Rad * degrees));
    }
    private void MoveTentacleAnchor(Vector2 newPosition, Vector2 hitNormal)
    {
        Tentacle tentacleToMove;
        if (_probingTentacle != null)
        {
            tentacleToMove = _probingTentacle;
            tentacleToMove.ConnectProbe(newPosition);
            _probingTentacle = null;
        }
        else
        {
            tentacleToMove = GetNextTentacle();
            tentacleToMove.LaunchTentacle(newPosition, hitNormal, _tentacleLaunchSpeed);
        }
    }

    private Tentacle GetNextTentacle()
    {
        if (_activeTentacles.Count >= _maxActiveTentacles)
        {
            // deactivate the oldest tentacle in the list
            _activeTentacles[0].DeactivateTentacle();
            _tentacleBank.Add(_activeTentacles[0]);
            _activeTentacles.RemoveAt(0);
        }
        Tentacle tentacleToMove = _tentacleBank[0];
        _tentacleBank.RemoveAt(0);
        _activeTentacles.Add(tentacleToMove);
        tentacleToMove.gameObject.SetActive(true);
        return tentacleToMove;
    }
    // to be called by individual tentacles when certain critera is met to self-deactivate
    public void TentacleSelfDeactivate(Tentacle tentacle)
    {
        _tentacleBank.Add(tentacle);
        _activeTentacles.Remove(tentacle);
        tentacle.DeactivateTentacle();
    }
}
