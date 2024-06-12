using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TreeEditor;
using Unity.VisualScripting;

public class Tentacle : MonoBehaviour
{
    [SerializeField] private TentacleVisual _tentacleVisual;
    [SerializeField] private TentacleCodeAnimator _tentacleAnimation;
    [SerializeField] private GameObject _tentacleVisualObject;
    [SerializeField] private Rigidbody2D _playerRB;
    [SerializeField] private bool _isConnected = true;
    [SerializeField] private float _breakDistance = 10;
    [SerializeField] private Transform _anchor;

    [Header("Modular Force Settings")]
    [SerializeField, Range(0, 2)] private float _currentForceMultiplier = 1;
    [SerializeField] private float _angleThresholdForMaxInfluence = 5;
    [SerializeField] private float _angleThresholdForMinInfluence = 108;
    [SerializeField] private float _minForceMult = 0.3f;
    [SerializeField] private float _maxForceMult = 2;
    [SerializeField, Range(0, 360)] private float _selfBreakAngleThreshold = 90;

    [Header("Raycast Break Settings")]
    [SerializeField] private LayerMask _solidGroundLayer;

    public float CurrentForceMultiplier { get { return _currentForceMultiplier; } }
    public Vector2 AnchorNormal { get; private set; }

    private TentacleMovement _movement;
    [SerializeField, ReadOnly] private Vector2 _contributionVector = Vector2.zero;
    [SerializeField, ReadOnly] private Vector2 _playerToAnchorVector = Vector2.zero;
    private bool _isRetracting;

    public Vector2 PlayerToAnchorVector
    {
        get
        {
            return new Vector2(_anchor.position.x, _anchor.position.y) - new Vector2(transform.position.x, transform.position.y);
        }
    }
    public float ForceMultiplier { get { return _currentForceMultiplier; } }
    public bool IsConnected { get { return _isConnected; } }

    public Vector2 ContributionVector { get { return _contributionVector; } }

    private void Start()
    {
        _movement = GetComponentInParent<TentacleMovement>();
        _tentacleVisual.InitVisual(_anchor);
    }
    private void Update()
    {
        if (_isConnected)
        {
            Debug.DrawRay(_anchor.transform.position, AnchorNormal, Color.red);
            if (!_isRetracting)
            {
                CalculateInfluenceModifier();
            }
            if (_playerToAnchorVector.magnitude > _breakDistance || !HasDirectVisionOfAnchor())
            {
                DeactivateTentacle(12);
            }
        }
        _playerToAnchorVector = PlayerToAnchorVector;
        CalculateContributionVector();
    }
    public void LaunchTentacle(Vector3 anchorPosition, Vector2 hitNormal, float travelSpeed = 10f)
    {
        _anchor.position = anchorPosition;
        _tentacleVisualObject.SetActive(true);
        _tentacleAnimation.AnimateLaunch(anchorPosition, hitNormal);
        AnchorNormal = hitNormal;
        StartCoroutine(GameplayConnectedTimer(travelSpeed));
        //Debug.Log(anchorPosition);
    }
    private IEnumerator GameplayConnectedTimer(float connectSpeed)
    {
        float lerp = 0;
        while (lerp <= 1)
        {
            lerp += Time.deltaTime * connectSpeed;
            yield return new WaitForFixedUpdate();
        }
        _isConnected = true;
    }
    public void DeactivateTentacle(float speed)
    {
        _isConnected = false;
        _tentacleAnimation.AnimateRetract();
    }
    public void DeactivateJumpTentacle()
    {
        _isConnected = false;
        _tentacleAnimation.AnimateJump();
    }
    private void CalculateInfluenceModifier()
    {
        float angle = Vector2.Angle(_movement.TargetDirectionNormalized, PlayerToAnchorVector.normalized);
        if (angle >= _selfBreakAngleThreshold)
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
    private void CalculateContributionVector()
    {
        float xContribution = 0; 
        float yContribution = 0;
        if (_movement.TargetDirectionRaw.magnitude > 0.01f)
        {

            // if the current x component is in the same direction (sign) as the target direction
            if (Mathf.Sign(_movement.TargetDirectionRaw.x) == Mathf.Sign(PlayerToAnchorVector.x))
            {
                xContribution = PlayerToAnchorVector.x;
            }
            if (Mathf.Sign(_movement.TargetDirectionRaw.y) == Mathf.Sign(PlayerToAnchorVector.y))
            {
                yContribution = PlayerToAnchorVector.y;
            }
        }
        _contributionVector = new Vector2(xContribution, yContribution);
    }
    private bool HasDirectVisionOfAnchor()
    {
        RaycastHit2D[] hitInfo = new RaycastHit2D[1];
        int hits = Physics2D.RaycastNonAlloc(transform.position, PlayerToAnchorVector.normalized, hitInfo, PlayerToAnchorVector.magnitude, _solidGroundLayer);
        if (hits == 0)
        {
            return true;
        }
        if (Mathf.Abs(hitInfo[0].distance - PlayerToAnchorVector.magnitude) > 0.5f)
        {
            return false;
        }
        return true;
    }
}
