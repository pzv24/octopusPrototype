using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.VisualScripting;

public class TentaclePhysics : MonoBehaviour
{

    [SerializeField, ReadOnly] private List<Tentacle> _tentacles = new List<Tentacle>();
    [SerializeField, ReadOnly] private Vector2 _finalVector = Vector2.zero;
    private Rigidbody2D _rigidBody;
    [SerializeField] private float _implulseMagnitude = 1;
    [SerializeField] private bool _useImpulseModifier = false;

    [SerializeField] private float _linearDragConnected = 5;
    [SerializeField] private float _angularDragConnected = 5;

    [SerializeField] private float _linearDragFree = 0.2f;
    [SerializeField] private float _angularDragFree = 0.2f;

    [Header("Little Impulse Settings")]
    [SerializeField] private bool _useFreeImpulse = true;
    [SerializeField] private float _lilImpulseMaxSpeed = 5;
    [SerializeField] private float _lilAccelerationBoost = 1;
    [SerializeField] private float _notGroundedForceMultiplier = 0.1f;

    [Header("On Surface Settings")]
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private float _groundSphereCastRadius = 1;

    [Header("Rope-like settings")]
    [SerializeField] private float _maxRopeLength = 10f;
    [SerializeField] private float _elasticity = 0.1f;

    [SerializeField, ReadOnly] private float _speed;

    public bool IsOnSurface = false;
    public Vector3 CurrentSurfaceNormal = Vector3.up;
    private Coroutine _impulseCooldownCoroutine = null;
    private PlayerController _controller;
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
    }
    public void InitPhysics(List<Tentacle> tentacles)
    {
        Debug.Log("innit physics");
        _tentacles.Clear();
        for (int i = 0; i < tentacles.Count; i++)
        {
            _tentacles.Add(tentacles[i]);
        }
    }
    private void FindActiveForces()
    {
        _finalVector = Vector2.zero;
        for (int i = 0; i < _tentacles.Count; i++)
        {
            if(_tentacles[i].IsConnected && _tentacles[i].gameObject.activeInHierarchy)
            {
                float modifier = _useImpulseModifier ? _tentacles[i].CurrentInfluenceModifier : 1;
                _finalVector += _tentacles[i].PlayerToAnchorVectoRaw * modifier;
            }
        }
    }
    private void FindRopeLikeForces()
    {
        _finalVector = Vector2.zero;
        for (int i = 0; i < _tentacles.Count; i++)
        {
            if (_tentacles[i].IsConnected && _tentacles[i].gameObject.activeInHierarchy)
            {
                if (_tentacles[i].LockedMagnitude != -1 && _tentacles[i].PlayerToAnchorVectoRaw.magnitude > _tentacles[i].LockedMagnitude)
                {
                    _finalVector += _tentacles[i].PlayerToAnchorVectoRaw.normalized;
                    _finalVector = new Vector2(_finalVector.x, _finalVector.y*_rigidBody.gravityScale);
                }
            }
        }
    }

    private void ImpulseByTentacles()
    {
        _rigidBody.AddForce(_finalVector * _implulseMagnitude);
    }
    private void Update()
    {
        IsOnSurface = CheckSurface();
        if (true)
        {
            FindActiveForces();
        }
        else
        {
            FindRopeLikeForces();
        }
        ImpulseByTentacles();
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
        if (_useFreeImpulse && _controller.HasActiveInput)
        {
            if(IsOnSurface)
            {
                _rigidBody.AddForce(_rigidBody.mass * ImpulseAcceleration(targetDirectionNormalized));
            }
            else if (!IsOnSurface && activeTentacleCount > 0)
            {
                _rigidBody.AddForce(_rigidBody.mass * ImpulseAcceleration(targetDirectionNormalized) * _notGroundedForceMultiplier);
            }
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
            Gizmos.DrawLine(transform.position, tentacle.PlayerToAnchorVectoRaw + Get2DPosition());
        }
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, _finalVector + Get2DPosition());
    }

    private Vector2 Get2DPosition()
    {
        return new Vector2(transform.position.x, transform.position.y);
    }
}
