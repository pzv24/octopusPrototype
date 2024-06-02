using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.VisualScripting;

public class TentaclePhysics : MonoBehaviour
{

    [SerializeField] private List<Tentacle> _tentacles = new List<Tentacle>();
    [SerializeField, ReadOnly] private Vector2 _finalVector = Vector2.zero;
    private Rigidbody2D _rigidBody;
    [SerializeField] private float _implulseMagnitude = 1;

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

    [SerializeField, ReadOnly] private float _speed;

    public bool IsOnSurface = false;
    public Vector3 CurrentSurfaceNormal = Vector3.up;
    private Coroutine _impulseCooldownCoroutine = null;
    private PlayerController _controller;
    private TentacleMovement _movement;

    //public void FindIndividualVectors()
    //{
    //    _tentacleVectors.Clear();
    //    for (int i = 0; i < _tentacles.Count; i++)
    //    {
    //        Vector2 anchorLocation = new Vector2(_tentacles[i].transform.position.x, _tentacles[i].transform.position.y);
    //        _tentacleVectors.Add(anchorLocation - new Vector2(transform.position.x, transform.position.y));
    //    }
    //}

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
        _controller = GetComponent<PlayerController>();
        _movement = GetComponent<TentacleMovement>();
    }
    public void InitPhysics(List<Tentacle> tentacles)
    {
        for (int i = 0; i < tentacles.Count; i++)
        {
            _tentacles.Add(tentacles[i]);
        }
    }
    private void FindFinal()
    {
        _finalVector = Vector2.zero;
        float xCont = 0;
        float yCont = 0;
        for (int i = 0; i < _tentacles.Count; i++)
        {
            if(_tentacles[i].IsConnected && _tentacles[i].gameObject.activeInHierarchy)
            {
                //if (_controller.HasActiveInput)
                //{
                //    _finalVector += _tentacles[i].PlayerToAnchorVector * _tentacles[i].ForceMultiplier;
                //}
                //else
                //{
                //    _finalVector += _tentacles[i].PlayerToAnchorVector.normalized * _tentacles[i].ForceMultiplier;
                //}
                _finalVector += _tentacles[i].ContributionVector;
            }
        }
                    //_finalVector = new Vector2(xCont, yCont);
        Debug.DrawRay(transform.position, _finalVector*_movement.TargetDirectionRaw.magnitude, Color.green);
    }
    private float GetsFinalXContribution(float axisForceContribution)
    {
        if (Mathf.Sign(axisForceContribution) == Mathf.Sign(_movement.TargetDirectionRaw.x))
        {
            float contribution = Mathf.Clamp(Mathf.Abs(axisForceContribution), 0, (Mathf.Abs(_movement.TargetDirectionRaw.x)));
            contribution *= Mathf.Sign(axisForceContribution);
            return contribution;
        }
        return 0;
    }
    private float GetsFinalYContribution(float axisForceContribution)
    {
        if (Mathf.Sign(axisForceContribution) == Mathf.Sign(_movement.TargetDirectionRaw.y))
        {
            float contribution = Mathf.Clamp(Mathf.Abs(axisForceContribution), 0, (Mathf.Abs(_movement.TargetDirectionRaw.y)));
            contribution *= Mathf.Sign(axisForceContribution);
            return contribution;
        }
        return 0;
    }

    private void ImpulseByTentacles()
    {
        _rigidBody.AddForce(_finalVector * _implulseMagnitude);
    }
    private void FixedUpdate()
    {
        IsOnSurface = CheckSurface();
        FindFinal();
        ImpulseByTentacles();
        AdjustDrag();
        _speed = _rigidBody.velocity.magnitude;
    }
    private bool CheckSurface()
    {
        RaycastHit2D[] hitInfo = new RaycastHit2D[1];
        //int hit = Physics2D.OverlapCircleNonAlloc(transform.position, _groundSphereCastRadius, hitInfo, _groundLayers);
        int hit = Physics2D.CircleCastNonAlloc(transform.position, _groundSphereCastRadius, Vector2.zero, hitInfo, 0, _groundLayers);
        if (hit > 0)
        {
            CurrentSurfaceNormal = hitInfo[0].normal;
            return true;
        }
        return false;
    }
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
    public Vector2 ImpulseAcceleration(Vector2 targetDirectionNormalized)
    {
        Vector2 targetVelocity = _lilImpulseMaxSpeed * targetDirectionNormalized.normalized;
        Vector2 velocityDiff = targetVelocity - _rigidBody.velocity;
        Vector2 acceleration = velocityDiff * _lilAccelerationBoost;
        return acceleration;
    }

    private void OnDrawGizmos()
    {
        foreach (Tentacle tentacle in _tentacles)
        {
            if(tentacle.IsConnected)
            {
                Gizmos.DrawLine(transform.position, tentacle.PlayerToAnchorVector + Get2DPosition());
            }
        }
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, _finalVector + Get2DPosition());
    }

    private Vector2 Get2DPosition()
    {
        return new Vector2(transform.position.x, transform.position.y);
    }
}
