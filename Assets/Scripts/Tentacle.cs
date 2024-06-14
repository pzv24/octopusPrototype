using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Tentacle : MonoBehaviour
{
    [Header("Plug in Fields")]
    [SerializeField] private TentacleVisual _tentacleVisual;
    [SerializeField] private TentacleCodeAnimator _tentacleAnimation;
    [SerializeField] private GameObject _tentacleVisualObject;
    [SerializeField] private Rigidbody2D _playerRB;
    [SerializeField] private Transform _anchor;

    [Header("Tentacle Settings")]
    [SerializeField] private bool _tentacleAnchored = true;
    [SerializeField] private float _breakDistance = 10;

    [Header("Tentacle Probing")]
    [SerializeField] private bool _tentacleProbing = false;
    [SerializeField] private LayerMask _bothTerrainLayers;
    [SerializeField] private float _circleCastRadius = 0.3f;

    [Header("Break Settings")]
    [SerializeField] private LayerMask _solidGroundLayer;
    [SerializeField] private bool _breakOnLooseDirectSight = true;
    [SerializeField, Range(0, 360)] private float _selfBreakAngleThreshold = 90;

    [SerializeField, ReadOnly] private Vector2 _contributionVector = Vector2.zero;
    [SerializeField, ReadOnly] private Vector2 _playerToAnchorVector = Vector2.zero;

    public Vector2 AnchorNormal { get; private set; }
    public bool IsAnchored { get { return _tentacleAnchored; } }
    public Vector2 ContributionVector { get { return _contributionVector; } }
    public Vector2 PlayerToAnchorVector
    {
        get
        {
            return new Vector2(_anchor.position.x, _anchor.position.y) - new Vector2(transform.position.x, transform.position.y);
        }
    }

    private TentacleMovement _movement;
    private void Start()
    {
        _movement = GetComponentInParent<TentacleMovement>();
        _tentacleVisual.InitVisual(_anchor);
    }
    private void FixedUpdate()
    {
        if (_tentacleAnchored)
        {
            Debug.DrawRay(_anchor.transform.position, AnchorNormal, Color.red);
            CalculateAngleToTargetPosition();
            if (AutoBreakConnection())
            {
                DeactivateTentacle();
            }
            CalculateContributionVector();
        }
        else if (_tentacleProbing)
        {
            Collider2D[] hits = new Collider2D[1];
            int hitCount = Physics2D.OverlapCircleNonAlloc(_anchor.transform.position, _circleCastRadius, hits, _bothTerrainLayers);
            if(hitCount > 0)
            {
                Debug.Log(hits[0].ClosestPoint(_anchor.transform.position));
                Debug.Log("Probe Connected");
                _movement.ReleaseProbingTentacle();
            }
        }
        _playerToAnchorVector = PlayerToAnchorVector;
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
        _tentacleAnchored = true;
    }
    private bool AutoBreakConnection()
    {
        if (_playerToAnchorVector.magnitude > _breakDistance) return true;
        if (_breakOnLooseDirectSight && !HasDirectVisionOfAnchor()) return true;
        return false;

    }
    public void DeactivateTentacle()
    {
        _tentacleAnchored = false;
        _tentacleAnimation.AnimateRetract();
    }
    public void DeactivateJumpTentacle()
    {
        _tentacleAnchored = false;
        _tentacleAnimation.AnimateJump();
    }
    public void SetTentacleProbing(bool probing)
    {
        _tentacleProbing = probing;
        if(probing)
        {
            _tentacleAnchored = false;
        }
    }
    public void SetAnchorPosition(Vector3 position)
    {
        _anchor.position = position;
    }
    private void CalculateAngleToTargetPosition()
    {
        float angle = Vector2.Angle(_movement.TargetDirectionNormalized, PlayerToAnchorVector.normalized);
        if (angle >= _selfBreakAngleThreshold)
        {
            _movement.TentacleSelfDeactivate(this);
            return;
        }
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
