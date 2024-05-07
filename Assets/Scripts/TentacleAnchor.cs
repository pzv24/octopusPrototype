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

    private void Update()
    {
        if( _isConnected)
        {
            _fromPlayerVector = PlayerVector();
            _tentacleVisual.SetPosition(1, Vector3.zero);
        }
        _tentacleVisual.SetPosition(0, -_fromPlayerVector);
    }

    private Vector2 PlayerVector()
    {
        Vector2 playerV2 = new Vector2(_playerRB.gameObject.transform.position.x, _playerRB.gameObject.transform.position.y);
        Vector2 currentV2 = new Vector2(transform.position.x, transform.position.y);
        return currentV2 - playerV2;
    }

    public void DeactivateTentacle()
    {
        _isConnected = false;
        gameObject.SetActive(false);
        _tentacleVisual.gameObject.SetActive(false);
    }

    public void ActivateTentacle(Vector2 anchorPosition)
    {
        _isConnected = true;
        gameObject.SetActive(true);
        _tentacleVisual.gameObject.SetActive(true);
        transform.position = new Vector3(anchorPosition.x, anchorPosition.y, transform.position.z);
    }

    public void LaunchTentacle(Vector3 anchorPosition, float travelSpeed = 10f)
    {
        StartCoroutine(TentacleVisualLerp(anchorPosition, travelSpeed));
    }

    private IEnumerator TentacleVisualLerp(Vector3 anchorPosition, float speed)
    {
        float iterator = 0;
        while(iterator < 1)
        {
            iterator += speed * Time.deltaTime;
            // the "start" of the tentacle, the part attached to the player
            Vector3 lerpingVector = Vector3.Slerp(PlayerVector(), Vector3.zero, iterator);
            _tentacleVisual.SetPosition(1, -lerpingVector);
            yield return null;
        }
        ActivateTentacle(anchorPosition);
    }
}
