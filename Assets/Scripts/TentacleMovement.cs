using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TreeEditor;
using System.Runtime.CompilerServices;

public class TentacleMovement : MonoBehaviour
{
    [SerializeField] private List<TentacleAnchor> _tentacles = new List<TentacleAnchor>();
    [SerializeField] private float _tentacleFireCooldown =1f;
    [SerializeField] private float _tentacleMaxDistance;
    [SerializeField] private LayerMask _tentacleCollisionLayers;
    RaycastHit2D[] _tentacleRaycastHit = new RaycastHit2D[1];

    private Vector3 _targetLocation = Vector3.zero;
    private float _tentacleChangeElapsed = 0;
    private int _currentAnchorChangeIndex = 0;

    public void SetTargetLocation(Vector3 targetLocation)
    {
        _targetLocation = targetLocation;
        //Debug.DrawLine(transform.position, _targetLocation);
        Debug.DrawRay(transform.position, _targetLocation.normalized);
    }

    private void Update()
    {
        _tentacleChangeElapsed += Time.deltaTime;
        RaycastTentacle();
        //TryChangeTentacleAnchor();
    }

    private void TryChangeTentacleAnchor()
    {
        if(Vector3.Distance(_targetLocation, transform.position) > 0.5f)
        {
            if(_tentacleChangeElapsed > _tentacleFireCooldown)
            {
                _tentacleChangeElapsed = 0;
                _tentacles[_currentAnchorChangeIndex].transform.position = _targetLocation;
                _currentAnchorChangeIndex += 1;
                if(_currentAnchorChangeIndex >= _tentacles.Count)
                {
                    _currentAnchorChangeIndex = 0;
                }
            }
        }
    }

    private void RaycastTentacle()
    {
        Vector3 targetDirection = (_targetLocation - transform.position).normalized;
        int hits = Physics2D.RaycastNonAlloc(transform.position, targetDirection, _tentacleRaycastHit, _tentacleMaxDistance, _tentacleCollisionLayers);
        //Debug.DrawRay(transform.position, targetDirection * _tentacleMaxDistance, Color.cyan);
        if (hits == 0) return;
        //Debug.Log(_tentacleRaycastHit[0].collider.gameObject.name);
        Debug.DrawLine(transform.position, _tentacleRaycastHit[0].point, Color.magenta);
    }
}
