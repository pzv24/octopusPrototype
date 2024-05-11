using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TreeEditor;
using System.Runtime.CompilerServices;

public class TentacleMovement : MonoBehaviour
{
    [SerializeField] private List<TentacleAnchor> _tentacleBank = new List<TentacleAnchor>();
    [SerializeField] private List<TentacleAnchor> _activeTentacles = new List<TentacleAnchor>();
    [SerializeField] private float _tentacleFireCooldown =1f;
    [SerializeField] private float _tentacleMaxDistance;
    [SerializeField] private float _tentacleLaunchSpeed = 3;
    [SerializeField] private int _maxActiveTentacles = 3;
    [SerializeField] private LayerMask _tentacleCollisionLayers;

    [SerializeField, ReadOnly] private Vector3 _targetLocation = Vector3.zero;
    [SerializeField, ReadOnly] private float _tentacleChangeElapsed = 0;
    [SerializeField, ReadOnly] private int _activeTentavles;

    private void Start()
    {
        _tentacleChangeElapsed = 0;
    }

    public void SetTargetLocation(Vector3 targetLocation)
    {
        _targetLocation = targetLocation;
        //Debug.DrawLine(transform.position, _targetLocation);
        //Debug.DrawRay(transform.position, _targetLocation.normalized);
    }

    private void FixedUpdate()
    {
        _tentacleChangeElapsed += Time.deltaTime;
        //RaycastTentacle();
        TryChangeTentacleAnchor();
    }

    private void TryChangeTentacleAnchor()
    {
        if(Vector3.Distance(_targetLocation, transform.position) > 0.5f)
        {
            if(_tentacleChangeElapsed > _tentacleFireCooldown)
            {
                //raycast to see if we find a new anchor location
                Vector2 newAnchorPosition = RaycastTentacle();

                // if location valid, then launch a new tentacle in that direction
                if (newAnchorPosition != Vector2.zero)
                {
                    _tentacleChangeElapsed = 0;
                    Debug.Log("firing tentacle");
                    MoveTentacleAnchor(newAnchorPosition);
                }
            }
        }
    }
    private Vector2 RaycastTentacle()
    {
        Vector3 targetDirection = (_targetLocation - transform.position).normalized;
        RaycastHit2D[] hitInfo = new RaycastHit2D[1];
        int hits = Physics2D.RaycastNonAlloc(transform.position, targetDirection, hitInfo, _tentacleMaxDistance, _tentacleCollisionLayers);
        //Debug.DrawRay(transform.position, targetDirection * _tentacleMaxDistance, Color.cyan);
        if (hits == 0)
        {
            return Vector2.zero;
        }
        Debug.DrawLine(transform.position, hitInfo[0].point, Color.magenta, 0.5f);
        return hitInfo[0].point;
    }
    private void MoveTentacleAnchor(Vector2 newPosition)
    {
        //ManageTentacleCycle();
        // deactivate the 
        if (_activeTentacles.Count >= _maxActiveTentacles)
        {
            // deactivate the oldest tentacle in the list
            _activeTentacles[0].DeactivateTentacle();
            _tentacleBank.Add(_activeTentacles[0]);
            _activeTentacles.RemoveAt(0);
        }
        TentacleAnchor anchorToMove = _tentacleBank[0];
        _tentacleBank.RemoveAt(0);
        _activeTentacles.Add(anchorToMove);
        anchorToMove.gameObject.SetActive(true);
        anchorToMove.LaunchTentacle(newPosition, _tentacleLaunchSpeed);

    }

    //private TentacleAnchor ManageTentacleCycle()
    //{
    //    // deactivate the 
    //    if(_activeTentacles.Count >= _maxActiveTentacles)
    //    {
    //        _activeTentacles[0].DeactivateTentacle();
    //    }
    //    // just increment int, do not deactivate current one
    //    IncrementTentacleIndex();
    //    return _tentacleBank[_tentacleIndexHead];
    //    //_tentacles[_currentAnchorChangeIndex].ActivateTentacle(_tentacleRaycastHit[0].point);
    //}


    private int ActiveTentacleCount()
    {
        int count = 0;
        for (int i = 0;  i < _tentacleBank.Count; i++)
        {
            if (_tentacleBank[i].isActiveAndEnabled && _tentacleBank[i].IsConnected)
            {
                count++;
            }
        }
        _activeTentavles = count;
        return count;
    }
    //private void IncrementTentacleIndex()
    //{
    //    _tentacleIndexHead += 1;
    //    if (_tentacleIndexHead >= _tentacleBank.Count)
    //    {
    //        _tentacleIndexHead = 0;
    //    }
    //    ActiveTentacleCount();
    //}

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.magenta;
    //    Gizmos.DrawCube(_tentacleRaycastHit[0].point, Vector3.one*0.9f);
    //}
}
