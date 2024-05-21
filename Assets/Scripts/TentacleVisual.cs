using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;

public class TentacleVisual : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _tentacleSegmentCount = 50;
    [SerializeField] private float _smoothSpeed = 10f;
    [SerializeField] private float _connectedSmoothFactor = 20f;
    [SerializeField] private float _detachedSmoothFactor = 200f;
    [SerializeField] private float _tentacleLengthModifier = 1;

    [Header("Plug in Fields")]
    [SerializeField] private Transform _origin;
    [SerializeField] private Transform _followEndTransform;

    [Header("Debug")]
    [SerializeField] private bool _connected = false;

    private LineRenderer _lineRenderer;
    private Vector3[] _segmentPositions;
    private Vector3[] _segmentVelocities;

    public Transform FollowTransform { get { return _followEndTransform; } }

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _tentacleSegmentCount;
        _segmentPositions = new Vector3[_tentacleSegmentCount];
        _segmentVelocities = new Vector3[_tentacleSegmentCount];
    }

    private void Update()
    {
        // set the final target direction for the entire tentacle based on current final position target
        Vector3 finalTargetDirection = (_followEndTransform.position - transform.position).normalized;
        // calculate the distance per segment based on target distance
        float distancePerSegment = Vector3.Distance(_followEndTransform.position, transform.position) * _tentacleLengthModifier / (_tentacleSegmentCount - 1);
        // set the root 
        _segmentPositions[0] = _origin.position;
        for (int i = 1; i < _segmentPositions.Length; i++)
        {
            // for each point, set the target position based on the previous point, plus the direction * disntace 
            Vector3 targetPosition = _segmentPositions[i - 1] + finalTargetDirection * distancePerSegment;

            // calculate the smooth factor:
            // if detached, slow down the the smooth modifier towards the tip
            // if connected, the opporite, massively increase the speed of the entire tentagle (makign it rigid), specially towards the end point
            float smoothFactorModifier = _connected ? _smoothSpeed / (_connectedSmoothFactor * i) : (_smoothSpeed + i) / _detachedSmoothFactor;

            //calculate the position with smooth damp function
            _segmentPositions[i] = Vector3.SmoothDamp(_segmentPositions[i], targetPosition, ref _segmentVelocities[i], smoothFactorModifier);
        }
        // apply the position to the line renderer
        _lineRenderer.SetPositions(_segmentPositions);
    }

    public void SetFollowEndPosition(Vector3 worldPosition)
    {
        _followEndTransform.position = worldPosition;
    }
    public void SetIsConnecteed(bool isConnecteed)
    {
        _connected = isConnecteed;
    }
}
