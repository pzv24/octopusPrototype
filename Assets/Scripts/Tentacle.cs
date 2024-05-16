using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TreeEditor;

public class Tentacle : MonoBehaviour
{
    [SerializeField] private LineRenderer _tentacleVisual;
    [SerializeField] private Rigidbody2D _playerRB;
    [SerializeField] private bool _isConnected = true;
    [SerializeField] private float _breakDistance = 10;
    [SerializeField, ReadOnly] private Vector2 _anchorPosition;
    [SerializeField, Range(0,1)] private float _currentForceMultiplier = 1;
    [SerializeField] private float _angleThresholdForMaxInfluence = 5;
    [SerializeField] private float _angleThresholdForMinInfluence = 108;
    [SerializeField] private float _minForceMult = 0.5f;
    [SerializeField] private float _maxForceMult = 1;

    [SerializeField, ReadOnly] private float  _playerToAnchorMagnitude = 0f;
    private float _lockedMagnitude;
    private FixedJoint2D _rope;
    private TentacleMovement _movement;

    public Vector2 PlayerToAnchorVectoRaw 
    { 
        get 
        { 
            return new Vector2(_anchorPosition.x, _anchorPosition.y) - new Vector2(transform.position.x, transform.position.y); 
        } 
    }
    public float CurrentInfluenceModifier { get { return  _currentForceMultiplier; } }
    public bool IsConnected { get { return _isConnected; } } 
    public float LockedMagnitude { get { return _lockedMagnitude; } }

    private void Start()
    {
        _rope = GetComponentInChildren<FixedJoint2D>();
        _movement = GetComponentInParent<TentacleMovement>();
    }
    private void Update()
    {
        if( _isConnected)
        {
            _tentacleVisual.SetPosition(1, PlayerToAnchorVectoRaw);
            if(PlayerToAnchorVectoRaw.magnitude > _breakDistance)
            {
                DeactivateTentacle(12);
            }
            CalculateInfluenceModifier();
        }
        if (_rope != null)
        {
            _rope.anchor = PlayerToAnchorVectoRaw;
        }
    }
    private void CalculateInfluenceModifier()
    {
        float angle = Vector2.Angle(_movement.TargetDirectionNormalized, PlayerToAnchorVectoRaw.normalized);
        float lerp = Mathf.InverseLerp(_angleThresholdForMinInfluence, _angleThresholdForMaxInfluence, angle);
        _currentForceMultiplier = Mathf.Lerp(_minForceMult, _maxForceMult, lerp);
        _currentForceMultiplier = 1;
        //Debug.DrawRay(transform.position, _movement.TargetDirectionNormalized * 15, Color.red);
        //Debug.DrawRay(transform.position, PlayerToAnchorVectoRaw.normalized * 15, Color.red);
        //Debug.Log(angle);
    }

    public void DeactivateTentacle(float speed)
    {
        StartCoroutine(TentacleVisualLerpBack(speed));
    }

    public void ActivateTentacleVisual()
    {
        _tentacleVisual.gameObject.SetActive(true);
    }
    public void OnTentacleConnected(Vector2 anchorPosition)
    {
        //transform.position = new Vector3(anchorPosition.x, anchorPosition.y, transform.position.z);
        _isConnected = true;
    }
    public void SetLockMagnitude(bool lockmagnitude)
    {
        if (lockmagnitude)
        {
            _lockedMagnitude = PlayerToAnchorVectoRaw.magnitude;
        }
        else
        {
            _lockedMagnitude = -1;
        }
    }

    public void LaunchTentacle(Vector3 anchorPosition, float travelSpeed = 10f)
    {
        _anchorPosition = anchorPosition;
        _tentacleVisual.SetPosition(1, Vector3.zero);
        _tentacleVisual.SetPosition(0, Vector3.zero);
        ActivateTentacleVisual();
        StartCoroutine(TentacleVisualLerp(anchorPosition, travelSpeed));
        Debug.Log(anchorPosition);
    }

    // line position 0 -> local anchor coordinate (always 0,0)
    // line position 1 -> relative player position 
    private IEnumerator TentacleVisualLerp(Vector3 anchorPosition, float speed)
    {
        float iterator = 0;
        while (iterator < 1)
        {
            iterator += speed * Time.deltaTime;
            // the "start" of the tentacle, the part attached to the player
            Vector3 lerpingVector = Vector3.Slerp(Vector2.zero, PlayerToAnchorVectoRaw, iterator);
            _tentacleVisual.SetPosition(1, lerpingVector);
            yield return new WaitForEndOfFrame();
        }
        OnTentacleConnected(anchorPosition);
    }
    private IEnumerator TentacleVisualLerpBack(float speed)
    {
        float iterator = 1;
        _isConnected = false;
        while (iterator > 0)
        {
            iterator -= speed * Time.deltaTime;
            // the "start" of the tentacle, the part attached to the player
            Vector3 lerpingVector = Vector3.Slerp(Vector2.zero, PlayerToAnchorVectoRaw, iterator);
            _tentacleVisual.SetPosition(1, lerpingVector);
            yield return new WaitForEndOfFrame();
        }
        _tentacleVisual.gameObject.SetActive(false);
        _anchorPosition = Vector3.zero;
        gameObject.SetActive(false);
        SetLockMagnitude(false);
    }
}
