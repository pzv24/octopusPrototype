using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TentacleAnchor : MonoBehaviour
{
    [SerializeField] private LineRenderer _tentacleVisual;
    [SerializeField] private Rigidbody2D _playerRB;
    [SerializeField] private bool _isConnected = true;

    [SerializeField, ReadOnly] private Vector2 _fromPlayerVector = Vector2.zero;
    public Vector2 FromPlayerVector { get { return _fromPlayerVector; } }
    public bool IsConnected { get { return _isConnected; } } 

    private void Update()
    {
        if( _isConnected)
        {
            _fromPlayerVector = PlayerToAnchorVector();
            _tentacleVisual.SetPosition(1, Vector3.zero);
            _tentacleVisual.SetPosition(0, -_fromPlayerVector);
        }
    }

    private Vector2 PlayerToAnchorVector()
    {
        Vector2 playerV2 = new Vector2(_playerRB.gameObject.transform.position.x, _playerRB.gameObject.transform.position.y);
        Vector2 currentV2 = new Vector2(transform.position.x, transform.position.y);
        return currentV2 - playerV2;
    }

    public void DeactivateTentacle()
    {
        _isConnected = false;
        _tentacleVisual.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void ActivateTentacle()
    {
        _tentacleVisual.gameObject.SetActive(true);
        //gameObject.SetActive(true);
    }
    public void OnTentacleConnected(Vector2 anchorPosition)
    {
        transform.position = new Vector3(anchorPosition.x, anchorPosition.y, transform.position.z);
        _isConnected = true;
    }

    public void LaunchTentacle(Vector3 anchorPosition, float travelSpeed = 10f)
    {
        transform.position = new Vector3(anchorPosition.x, anchorPosition.y, transform.position.z);
        _fromPlayerVector = PlayerToAnchorVector();
        _tentacleVisual.SetPosition(1, -_fromPlayerVector);
        _tentacleVisual.SetPosition(0, -_fromPlayerVector);
        ActivateTentacle();
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
            Vector3 lerpingVector = Vector3.Slerp(PlayerToAnchorVector(), Vector2.zero, iterator);
            _tentacleVisual.SetPosition(1, -lerpingVector);
            _tentacleVisual.SetPosition(0, -PlayerToAnchorVector());
            yield return new WaitForEndOfFrame();
        }
        OnTentacleConnected(anchorPosition);
    }
    private IEnumerator TentacleVisualLerpBack(Vector3 anchorPosition, float speed)
    {
        float iterator = 0;
        while (iterator < 1)
        {
            iterator += speed * Time.deltaTime;
            // the "start" of the tentacle, the part attached to the player
            Vector3 lerpingVector = Vector3.Slerp(PlayerToAnchorVector(), Vector2.zero, iterator);
            _tentacleVisual.SetPosition(1, -lerpingVector);
            yield return null;
        }
        //ActivateTentacle(anchorPosition);
    }
}
