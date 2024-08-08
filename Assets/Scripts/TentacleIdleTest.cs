using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TentacleIdleTest : MonoBehaviour
{
    [SerializeField] private int _tentacleSegmentCount = 30;
    [SerializeField] private Transform _root;
    [SerializeField] private Transform _animRoot;
    [SerializeField] private Transform _targetPostion;
    [SerializeField] private float _targetDistance = 0.2f;
    [SerializeField] private float _maxDistance = 0.4f;
    [SerializeField] private float _smoothSpeed = 0.01f;
    private LineRenderer _lineRenderer;
    private Vector3[] _segmentPositions;
    private Vector3[] _segmentVelocities;
    [SerializeField] private float _snapSpeedMult =10;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _tentacleSegmentCount;
        _segmentPositions = new Vector3[_tentacleSegmentCount];
        _segmentVelocities = new Vector3[_tentacleSegmentCount];
    }
    private void Update()
    {
        _segmentPositions[0] = _root.position;
        //Vector3 finalTargetDirection = (_targetPostion.position - transform.position).normalized;
        for (int i = 1; i < _segmentPositions.Length; i++)
        {
            //current (almsot) targeted
            //Vector3 targetPosition = _segmentPositions[i - 1] + finalTargetDirection * _targetDistance;
            //_segmentPositions[i] = Vector3.SmoothDamp(_segmentPositions[i], targetPosition, ref _segmentVelocities[i], _smoothSpeed);

            //calculate the position with smooth damp function
            //normal
            Vector3 targetPosition = _segmentPositions[i - 1] + _animRoot.right * _targetDistance;
            Vector3 newPosition = Vector3.SmoothDamp(_segmentPositions[i], targetPosition, ref _segmentVelocities[i], _smoothSpeed);
            if (Vector3.Distance(newPosition, _segmentPositions[i - 1]) > _maxDistance)
            {
                targetPosition = _segmentPositions[i - 1] + (_segmentPositions[i] - _segmentPositions[i - 1]).normalized * _targetDistance;
                newPosition = targetPosition;
                //newPosition = Vector3.SmoothDamp(_segmentPositions[i], targetPosition, ref _segmentVelocities[i], _smoothSpeed / _snapSpeedMult);
            }
            _segmentPositions[i] = newPosition;
        }

        // apply the position to the line renderer
        _lineRenderer.SetPositions(_segmentPositions);
    }


}
