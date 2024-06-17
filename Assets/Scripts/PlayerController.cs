using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private TentacleMovement _movement;
    private PlayerInput _playerInput;
    private TentaclePhysics _physics;
    private Vector2 _moveInput;
    private bool _mousePressed = false;
    private bool _releasePressed = false;
    private SpriteRenderer _targetLocationSprite;
    private Vector3 _lookDirection = Vector3.zero;

    //to be used by controller support implementation, currently useless
    [SerializeField] private float _tentacleRange = 5;

    [SerializeField] private bool _debugLog = false;

    [SerializeField, ReadOnly] private Vector3 _targetLocation = Vector3.zero;
    [SerializeField] private GameObject _targetPostionVisual;
    [SerializeField] private bool _showTargetPosition = true;
    [SerializeField] private Color _canGetThereColor;
    [SerializeField] private Color _outsideCurrentTentaclesColor;
    [SerializeField] private Color _noDirectRouteColor;
    [SerializeField] private LayerMask _solidTerrainLayer;
    [SerializeField, ReadOnly] private bool _directLineTotarget = false;

    public bool HasActiveInput { get; private set; }
    public Vector3 LookDirection { get { return _lookDirection; } }
    public bool ReleasePressed {  get { return _releasePressed; } }

    private void Start()
    {
        _movement = GetComponent<TentacleMovement>();
        _mousePressed = false;
        HasActiveInput = false;
        _physics = GetComponent<TentaclePhysics>();
        _movement.SetHasActiveInput(false);
        _targetLocationSprite = _targetPostionVisual.GetComponent<SpriteRenderer>();
    }
    public void OnMove(InputValue input)
    {
        _moveInput = input.Get<Vector2>();
    }

    public void OnSetTargetLocationMouse(InputValue input)
    {
        if(input.Get<float>() == 1)
        {
            _mousePressed = true;
        }
        else
        {
            _mousePressed = false;
            _movement.ReleaseProbingTentacle();
            //_targetLocation = transform.position;
        }
    }
    public void OnRelease(InputValue input)
    {
        if(input.Get<float>() == 1)
        {
            _releasePressed = true;
            if(_debugLog) Debug.Log("Releasing tentacles");
            _movement.ReleaseAllTentacles();
            _targetLocation = transform.position;
        }
        else
        {
            _releasePressed = false;
        }
    }

    public void OnSetTargetLocationController(InputValue input)
    {
        Vector2 relativeLocation = input.Get<Vector2>() * _tentacleRange;
        Vector2 currentPostion = new Vector2(transform.position.x, transform.position.y);
        SetTargetLocationFromInput(relativeLocation + currentPostion);
    }

    private void SetTargetLocationFromInput(Vector3 targetLocation)
    {
        _targetLocation = targetLocation;
        DirectLineToTargetLocation(targetLocation);
        if (_showTargetPosition && _targetPostionVisual.activeInHierarchy)
        {
            _targetPostionVisual.transform.position = targetLocation;
            ChangeVisualColor();
        }
    }
    private void ChangeVisualColor()
    {
        if(!_directLineTotarget)
        {
            _targetLocationSprite.color = _noDirectRouteColor;
            return;
        }
        else if (_physics.CanGetToTargetWithCurrentTentacles)
        {
            _targetLocationSprite.color = _canGetThereColor;
            return;
        }
        else
        {
            _targetLocationSprite.color = _outsideCurrentTentaclesColor;
        }
    }

    private void Update()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _lookDirection = mousePosition - transform.position;
        mousePosition.z = 0;
        if(_mousePressed && !_releasePressed)
        {
            SetTargetLocationFromInput(mousePosition);
            HasActiveInput = true;
        }
        else
        {
            HasActiveInput = false;
        }
        _movement.SetHasActiveInput(HasActiveInput);
        _movement.SetTargetLocation(_targetLocation);

    }

    private void DirectLineToTargetLocation(Vector3 mousePos)
    {
        RaycastHit2D[] hitInfo = new RaycastHit2D[1];
        Vector3 direction = mousePos - transform.position;
        int hits = Physics2D.RaycastNonAlloc(transform.position, direction, hitInfo, direction.magnitude, _solidTerrainLayer);
        Debug.DrawRay(transform.position, direction, Color.gray);
        _directLineTotarget = hits == 0;
    }
    private void OnDrawGizmosSelected()
    {
        if (_debugLog)
        {
             Gizmos.DrawSphere(_targetLocation, 1);
        }
    }

}
