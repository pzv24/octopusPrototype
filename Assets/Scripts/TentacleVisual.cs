using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;

public class TentacleVisual : MonoBehaviour
{
    [SerializeField] private int _tentacleSegmentCount = 50;
    [SerializeField] private Transform _targetDirection;
    [SerializeField] private Transform _finalPosition;
    [SerializeField] private float _segmentSeparation;
    [SerializeField] private float _smoothSpeed = 10f;
    [SerializeField] private float _connectedSmoothFactor = 20f;
    [SerializeField, ReadOnly] private Vector3[] _segmentPositions;
    [SerializeField, ReadOnly] private Vector3[] _segmentVelocities;
    [SerializeField] private bool _connected = false;
    private LineRenderer _lineRenderer;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _tentacleSegmentCount;
        _segmentPositions = new Vector3[_tentacleSegmentCount];
        _segmentVelocities = new Vector3[_tentacleSegmentCount];
    }

    private void Update()
    {
        if (_connected)
        {
            Vector3 tentacleDirection = (_finalPosition.position - transform.position).normalized;
            float distancePerSegment = Vector3.Distance(_finalPosition.position, transform.position) / (_tentacleSegmentCount - 1);
            // set the root 
            _segmentPositions[0] = transform.position;
            _segmentPositions[_segmentPositions.Length-1] = _finalPosition.position;
            // set the target position of the last point
            //_segmentPositions[_segmentPositions.Length - 1] = _finalPosition.position;
            // iterate backwards from last point
            for (int i = 1; i < _segmentPositions.Length; i++)
            {
                _segmentPositions[i] = Vector3.SmoothDamp(_segmentPositions[i], _segmentPositions[i - 1] + tentacleDirection * distancePerSegment, ref _segmentVelocities[i], _smoothSpeed/(_connectedSmoothFactor*i));
            }
            _lineRenderer.SetPositions(_segmentPositions);
        }
        else
        {
            Vector3 tentacleDirection = (_finalPosition.position - transform.position).normalized;
            float distancePerSegment = Vector3.Distance(_finalPosition.position, transform.position) / (_tentacleSegmentCount-1);
            // set the root 
            _segmentPositions[0] = transform.position;
            // set the target position of the last point
            //_segmentPositions[_segmentPositions.Length - 1] = _finalPosition.position;
            // iterate backwards from last point
            for (int i = 1;  i < _segmentPositions.Length; i++)
            {
                //float lerper = Mathf.InverseLerp(0, _tentacleSegmentCount, i);
                //float smoothSpeed = Mathf.Lerp(_smoothSpeed, _smoothSpeed / _tentacleSegmentCount, lerper);
                _segmentPositions[i] = Vector3.SmoothDamp(_segmentPositions[i], _segmentPositions[i - 1] + tentacleDirection * distancePerSegment, ref _segmentVelocities[i], _smoothSpeed);
            }
            _lineRenderer.SetPositions(_segmentPositions);
        }
    }
}
