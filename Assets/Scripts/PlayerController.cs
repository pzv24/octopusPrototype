using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private TentacleMovement _movement;
    private PlayerInput _playerInput;
    private Vector2 _moveInput;
    private bool _mousePressed = false;
    private bool _canFireTentacles = true;
    private Vector3 _lookDirection = Vector3.zero;

    //to be used by controller support implementation, currently useless
    [SerializeField] private float _tentacleRange = 5;

    [SerializeField] private bool _debugLog = false;

    [SerializeField, ReadOnly] private Vector3 _targetLocation = Vector3.zero;

    public bool HasActiveInput { get; private set; }
    public Vector3 LookDirection { get { return _lookDirection; } }

    private void Start()
    {
        _movement = GetComponent<TentacleMovement>();
        _mousePressed = false;
        HasActiveInput = false;
        _movement.CanFireTentacles(false);
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
            _movement.CanFireTentacles(true);
        }
        else
        {
            _mousePressed = false;
            _movement.CanFireTentacles(false);
            //_targetLocation = transform.position;
        }
    }
    public void OnRelease(InputValue input)
    {
        if(input.Get<float>() == 1)
        {
            if(_debugLog) Debug.Log("Releasing tentacles");
            _movement.ReleaseAllTentacles();
            _targetLocation = transform.position;
            _movement.CanFireTentacles(false);
        }
        else if(_mousePressed)
        {
            _movement.CanFireTentacles(true);
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
    }


    private void Update()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _lookDirection = mousePosition - transform.position;
        mousePosition.z = 0;
        if(_mousePressed)
        {
            SetTargetLocationFromInput(mousePosition);
            HasActiveInput = true;
        }
        else
        {
            //SetTargetLocation(transform.position);
            HasActiveInput = false;
        }
        _movement.SetTargetLocation(_targetLocation);
    }
    private void OnDrawGizmosSelected()
    {
        if (_debugLog)
        {
             Gizmos.DrawSphere(_targetLocation, 1);
        }
    }

}
