using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// player controller, manages inputs and send calls to respective scripts
public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject _targetPostionVisual;
    [SerializeField] private LayerMask _solidTerrainLayer;

    [Header("Debug")]
    [SerializeField] private bool _debugLog = false;
    [SerializeField] private bool _showTargetPosition = true;
    [SerializeField] private Color _canGetThereColor;
    [SerializeField] private Color _outsideCurrentTentaclesColor;
    [SerializeField] private Color _noDirectRouteColor;
    [SerializeField, ReadOnly] private Vector3 _targetLocation = Vector3.zero;
    [SerializeField, ReadOnly] private bool _directLineTotarget = false;

    public bool HasActiveInput { get; private set; }
    public Vector3 LookDirection { get { return _lookDirection; } }
    public Vector3 UpDirection { get { return _upDirection; } }
    public bool ReleasePressed {  get { return _releasePressed; } }

    private TentacleMovement _movement;
    private TentaclePhysics _physics;
    private bool _mousePressed = false;
    private bool _releasePressed = false;
    private SpriteRenderer _targetLocationSprite;
    private Vector3 _lookDirection = Vector3.zero;
    private Vector3 _upDirection = Vector3.zero;
    private void Start()
    {
        // get component and "zero" out innitial variables
        _movement = GetComponent<TentacleMovement>();
        _physics = GetComponent<TentaclePhysics>();
        _targetLocationSprite = _targetPostionVisual.GetComponent<SpriteRenderer>();

        HasActiveInput = false;
        _mousePressed = false;
        _movement.SetHasActiveInput(false);
    }
    public void OnSetTargetLocationMouse(InputValue input)
    {
        // get the state of mousePressed based on input value form input system
        if(input.Get<float>() == 1)
        {
            _mousePressed = true;
        }
        else
        {
            _mousePressed = false;
            _movement.ReleaseProbingTentacle();
        }
    }
    public void OnRelease(InputValue input)
    {
        //on release is the press of spacebar to release all tentacles and jump
        if(input.Get<float>() == 1)
        {
            _releasePressed = true;
            _movement.ReleaseAllTentacles();
            _targetLocation = transform.position;
            if(_debugLog) Debug.Log("Releasing tentacles");
        }
        else
        {
            _releasePressed = false;
        }
    }
    // similar method but for touch controlls, had to work a little different due to input system 
    public void OnReleaseTouch()
    {
        if (_debugLog) Debug.Log("Releasing tentacles");
        _movement.ReleaseAllTentacles();
        _targetLocation = transform.position;
    }
    private void SetTargetLocationFromInput(Vector3 targetLocation)
    {
        _targetLocation = targetLocation;
        DirectLineToTargetLocation(targetLocation);

        // if available, move the target position object (for debug purposes)
        if (_showTargetPosition && _targetPostionVisual.activeInHierarchy)
        {
            _targetPostionVisual.transform.position = targetLocation;
            ChangeVisualColor();
        }
    }
    private void DirectLineToTargetLocation(Vector3 mousePos)
    {
        // raycast to see if there is a direct route to target position (not blocked by terrain)
        // currently only used by debug functions
        RaycastHit2D[] hitInfo = new RaycastHit2D[1];
        Vector3 direction = mousePos - transform.position;
        int hits = Physics2D.RaycastNonAlloc(transform.position, direction, hitInfo, direction.magnitude, _solidTerrainLayer);
        Debug.DrawRay(transform.position, direction, Color.gray);
        _directLineTotarget = hits == 0;
    }

    private void Update()
    {
        //get mouse position, transform it relative to player and set it
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _lookDirection = mousePosition - transform.position;
        mousePosition.z = 0;

        // set the relative up direction based on look direction
        _upDirection = Vector3.Cross(_lookDirection, Vector3.forward);

        // if has active input, send the target location as the mouse position location, otherwise will send current position
        // this so the octuopus tries to stay in place if no move input is registered
        if(_mousePressed && !_releasePressed)
        {
            SetTargetLocationFromInput(mousePosition);
            HasActiveInput = true;
        }
        else
        {
            HasActiveInput = false;
        }
        // send the relevant variables for the movement script
        _movement.SetHasActiveInput(HasActiveInput);
        _movement.SetTargetLocation(_targetLocation);
        _movement.SetUpDirection(UpDirection);

    }
    // debug function, changes the color of the target sprite (if enabled) based on raycast conditions
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
    private void OnDrawGizmosSelected()
    {
        if (_debugLog)
        {
             Gizmos.DrawSphere(_targetLocation, 1);
        }
    }

}
