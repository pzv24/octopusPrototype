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

    [Header("Modular Force Settings")]
    [SerializeField, Range(0, 2)] private float _currentForceMultiplier = 1;
    [SerializeField] private float _angleThresholdForMaxInfluence = 5;
    [SerializeField] private float _angleThresholdForMinInfluence = 108;
    [SerializeField] private float _minForceMult = 0.3f;
    [SerializeField] private float _maxForceMult = 2;
    [SerializeField, Range(0, 360)] private float _selfBreakAngleThreshold = 90;

    public float CurrentForceMultiplier { get { return _currentForceMultiplier; } }

    private TentacleMovement _movement;

    [SerializeField, ReadOnly] private Vector2 _playerToAnchorVector = Vector2.zero;
    private bool _isRetracting;

    public Vector2 PlayerToAnchorVector 
    { 
        get 
        { 
            return new Vector2(_anchorPosition.x, _anchorPosition.y) - new Vector2(transform.position.x, transform.position.y); 
        } 
    }
    public float ForceMultiplier { get { return _currentForceMultiplier; } }
    public bool IsConnected { get { return _isConnected; } }

    private void Start()
    {
        _movement = GetComponentInParent<TentacleMovement>();
    }
    private void Update()
    {
        if( _isConnected)
        {
            _tentacleVisual.SetPosition(1, PlayerToAnchorVector);
            if (!_isRetracting)
            {
                CalculateInfluenceModifier();
            }
            if(_playerToAnchorVector.magnitude > _breakDistance)
            {
                DeactivateTentacle(12);
            }
        }
    }

    public void DeactivateTentacle(float speed)
    {
        if (!_isRetracting)
        {
            StartCoroutine(TentacleVisualLerpBack(speed));
        }
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

    public void LaunchTentacle(Vector3 anchorPosition, float travelSpeed = 10f)
    {
        _anchorPosition = anchorPosition;
        _tentacleVisual.SetPosition(1, Vector3.zero);
        _tentacleVisual.SetPosition(0, Vector3.zero);
        ActivateTentacleVisual();
        StartCoroutine(TentacleVisualLerp(anchorPosition, travelSpeed));
        Debug.Log(anchorPosition);
    }
    private void CalculateInfluenceModifier()
    {
        float angle = Vector2.Angle(_movement.TargetDirectionNormalized, PlayerToAnchorVector.normalized);
        if(angle >= _selfBreakAngleThreshold)
        {
            _movement.TentacleSelfDeactivate(this);
            return;
        }
        float lerp = Mathf.InverseLerp(_angleThresholdForMinInfluence, _angleThresholdForMaxInfluence, angle);
        _currentForceMultiplier = Mathf.Lerp(_minForceMult, _maxForceMult, lerp);
        //Debug.DrawRay(transform.position, _movement.TargetDirectionNormalized * 15, Color.red);
        //Debug.DrawRay(transform.position, PlayerToAnchorVectoRaw.normalized * 15, Color.red);
        //Debug.Log(angle);
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
            Vector3 lerpingVector = Vector3.Slerp(Vector2.zero, PlayerToAnchorVector, iterator);
            _tentacleVisual.SetPosition(1, lerpingVector);
            yield return new WaitForEndOfFrame();
        }
        OnTentacleConnected(anchorPosition);
    }
    private IEnumerator TentacleVisualLerpBack(float speed)
    {
        _isRetracting = true;
        float iterator = 1;
        _isConnected = false;
        while (iterator > 0)
        {
            iterator -= speed * Time.deltaTime;
            // the "start" of the tentacle, the part attached to the player
            Vector3 lerpingVector = Vector3.Slerp(Vector2.zero, PlayerToAnchorVector, iterator);
            _tentacleVisual.SetPosition(1, lerpingVector);
            yield return new WaitForEndOfFrame();
        }
        _tentacleVisual.gameObject.SetActive(false);
        _anchorPosition = Vector3.zero;
        _isRetracting = false;
        gameObject.SetActive(false);
    }
}
