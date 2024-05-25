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
    [SerializeField] private bool _setAutoConnect = true;
    [SerializeField] private float _autoConnectDistance = 0.3f;

    [Header("Wiggle Settings")]
    [SerializeField] private bool _isWiggling = false;
    [SerializeField] private bool _randomizeStartDirection = true;
    [SerializeField] private float _waveFrequency = 1;
    [SerializeField] private float _waveMagnitude = 1;
    private Transform _wiggledEndTransfrom;
    private float _wiggleTime = 0;

    [Header("Plug in Fields")]
    [SerializeField] private Transform _followEndTransform;

    [Header("Debug")]
    [SerializeField] private bool _connected = false;

    private LineRenderer _lineRenderer;
    private Vector3[] _segmentPositions;
    private Vector3[] _segmentVelocities;

    public Transform FollowTransform { get { return _followEndTransform; } }
    public bool IsLaunching = false;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _tentacleSegmentCount;
        _segmentPositions = new Vector3[_tentacleSegmentCount];
        _segmentVelocities = new Vector3[_tentacleSegmentCount];
        GameObject wigglePos = new GameObject("WigglePoint");
        wigglePos.transform.parent = _followEndTransform;
        _wiggledEndTransfrom = wigglePos.transform;
    }

    private void GetWiggledTargetPosition()
    {
        if(_isWiggling)
        {
            _wiggleTime += Time.deltaTime;
            // set the final target direction for the entire tentacle based on current final position target
            Vector3 finalTargetDirection = (_followEndTransform.position - transform.position).normalized;
            float angle = Mathf.Atan2(finalTargetDirection.y, finalTargetDirection.x) * Mathf.Rad2Deg - 90;
            _followEndTransform.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            _wiggledEndTransfrom.localPosition = new Vector3(Mathf.Sin(_waveFrequency * _wiggleTime) * _waveMagnitude, 0, 0);
        }
        else
        {
            _wiggledEndTransfrom.position = _followEndTransform.position;
        }
    }

    private void Update()
    {
        GetWiggledTargetPosition();
        // set the final target direction for the entire tentacle based on current final position target
        Vector3 finalTargetDirection = (_wiggledEndTransfrom.position - transform.position).normalized;
        // calculate the distance per segment based on target distance
        float distancePerSegment = Vector3.Distance(_wiggledEndTransfrom.position, transform.position) * _tentacleLengthModifier / (_tentacleSegmentCount - 1);
        // set the root 
        _segmentPositions[0] = transform.position;
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
        if(_setAutoConnect && IsLaunching && Vector3.Distance(_segmentPositions[_segmentPositions.Length - 1], _wiggledEndTransfrom.position) < _autoConnectDistance)
        {
            SetIsConnecteed(true);
            IsLaunching = false;
        }
    }

    public void SetFollowEndPosition(Vector3 worldPosition)
    {
        _followEndTransform.position = worldPosition;
    }
    public void SetIsConnecteed(bool isConnecteed)
    {
        _connected = isConnecteed;
    }
    [Button]
    public void SetIsWiggling(bool isWiggling)
    {
        if (isWiggling)
        {
            _wiggleTime = 0;
            if (_randomizeStartDirection)
            {
                int random = Random.Range(0, 2);
                Debug.Log(random.ToString());
                bool startDown = random == 0;
                _wiggleTime = startDown ? Mathf.PI*2 : Mathf.PI * -2;
            }
        }
        _isWiggling = isWiggling;
    }
    public void SetWiggleVariables(float wiggleFrequency, float wiggleMagnitude)
    {
        _waveFrequency = wiggleFrequency;
        _waveMagnitude = wiggleMagnitude;
    }

    private void OnDrawGizmos()
    {
        if(_wiggledEndTransfrom != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_wiggledEndTransfrom.position, 0.1f);
        }
    }
}
