using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private TentacleMovement _movement;
    private PlayerInput _playerInput;
    private Vector2 _moveInput;
    private bool _mousePressed = false;
    private bool _canFireTentacles = true;

    [SerializeField] private float _tentacleRange = 5;

    [SerializeField, ReadOnly] private Vector3 _targetLocation = Vector3.zero;

    public bool HasActiveInput { get; private set; }

    private void Start()
    {
        _movement = GetComponent<TentacleMovement>();
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
            _movement.LockTentacleMagnitudes(false);
        }
        else
        {
            _mousePressed = false;
            _targetLocation = transform.position;
            _movement.LockTentacleMagnitudes(true);
        }
    }
    public void OnRelease(InputValue input)
    {
        if(input.Get<float>() == 1)
        {
            Debug.Log("Releasing tentacles");
            _movement.ReleaseAllTentacles();
            _movement.CanFireTentacles(false);
        }
        else
        {
            _movement.CanFireTentacles(true);
        }
    }

    public void OnSetTargetLocationController(InputValue input)
    {
        Vector2 relativeLocation = input.Get<Vector2>() * _tentacleRange;
        Vector2 currentPostion = new Vector2(transform.position.x, transform.position.y);
        SetTargetLocation(relativeLocation + currentPostion);
    }

    private void SetTargetLocation(Vector3 targetLocation)
    {
        _targetLocation = targetLocation;
    }


    private void Update()
    {
        if(_mousePressed)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0;
            SetTargetLocation(mousePosition);
            HasActiveInput = true;
        }
        else
        {
            SetTargetLocation(transform.position);
            HasActiveInput = false;
        }
        _movement.SetTargetLocation(_targetLocation);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(_targetLocation, 1);
    }

}
