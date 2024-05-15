using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TreeEditor;

public class Tentacle : MonoBehaviour
{
    [SerializeField] private LineRenderer _tentacleVisual;
    [SerializeField] private Rigidbody2D _playerRB;
    [SerializeField] private bool _isConnected = true;
    [SerializeField] private float _breakDistance = 10;
    [SerializeField, ReadOnly] private Vector2 _anchorPosition;

    [SerializeField, ReadOnly] private Vector2 _playerToAnchorVector = Vector2.zero;
    public Vector2 PlayerToAnchorVector 
    { 
        get 
        { 
            return new Vector2(_anchorPosition.x, _anchorPosition.y) - new Vector2(transform.position.x, transform.position.y); 
        } 
    }
    public bool IsConnected { get { return _isConnected; } } 

    private void Update()
    {
        if( _isConnected)
        {
            _tentacleVisual.SetPosition(1, PlayerToAnchorVector);
            if(_playerToAnchorVector.magnitude > _breakDistance)
            {
                DeactivateTentacle(12);
            }
        }
    }

    public void DeactivateTentacle(float speed)
    {
        StartCoroutine(TentacleVisualLerpBack(speed));
    }

    public void ActivateTentacleVisual()
    {
        _tentacleVisual.gameObject.SetActive(true);
    }
    public void OnTentacleConnected(Vector2 anchorPosition)
    {
        //transform.position = new Vector3(anchorPosition.x, anchorPosition.y, transform.position.z);
        _isConnected = true;
    }

    public void LaunchTentacle(Vector3 anchorPosition, float travelSpeed = 10f)
    {
        _anchorPosition = anchorPosition;
        _tentacleVisual.SetPosition(1, Vector3.zero);
        _tentacleVisual.SetPosition(0, Vector3.zero);
        ActivateTentacleVisual();
        StartCoroutine(TentacleVisualLerp(anchorPosition, travelSpeed));
        Debug.Log(anchorPosition);
    }

    // line position 0 -> local anchor coordinate (always 0,0)
    // line position 1 -> relative player position 
    private IEnumerator TentacleVisualLerp(Vector3 anchorPosition, float speed)
    {
        float iterator = 0;
        while (iterator < 1)
        {
            iterator += speed * Time.deltaTime;
            // the "start" of the tentacle, the part attached to the player
            Vector3 lerpingVector = Vector3.Slerp(Vector2.zero, PlayerToAnchorVector, iterator);
            _tentacleVisual.SetPosition(1, lerpingVector);
            yield return new WaitForEndOfFrame();
        }
        OnTentacleConnected(anchorPosition);
    }
    private IEnumerator TentacleVisualLerpBack(float speed)
    {
        float iterator = 1;
        _isConnected = false;
        while (iterator > 0)
        {
            iterator -= speed * Time.deltaTime;
            // the "start" of the tentacle, the part attached to the player
            Vector3 lerpingVector = Vector3.Slerp(Vector2.zero, PlayerToAnchorVector, iterator);
            _tentacleVisual.SetPosition(1, lerpingVector);
            yield return new WaitForEndOfFrame();
        }
        _tentacleVisual.gameObject.SetActive(false);
        _anchorPosition = Vector3.zero;
        gameObject.SetActive(false);
    }
}
